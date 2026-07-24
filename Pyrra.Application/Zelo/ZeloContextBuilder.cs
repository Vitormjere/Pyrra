using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Application.Streaks;
using Pyrra.Domain.Nutricao;
using Pyrra.Domain.Treinos;

namespace Pyrra.Application.Zelo {
    // Monta um texto curto e legível com um resumo de cada módulo, para caber num prompt pequeno.
    // Não cruza módulos nem calcula nada além de médias simples: é só o retrato de "onde o usuário
    // está hoje" que o modelo lê antes de responder.
    public class ZeloContextBuilder : IZeloContextBuilder {
        // Janela de olhar para trás em nutrição e para a média de foco: uma semana é curto o
        // suficiente para ser "recente" e longo o bastante para pegar o último dia com registro.
        private const int LookbackDays = 7;

        // Quantos treinos recentes entram no resumo. Poucos, para o texto ficar enxuto.
        private const int RecentWorkoutCount = 5;

        private static readonly IReadOnlyDictionary<MealType, string> MealLabels = new Dictionary<MealType, string> {
            [MealType.CafeDaManha] = "Café da manhã",
            [MealType.Almoco]      = "Almoço",
            [MealType.Lanche]      = "Lanche",
            [MealType.Jantar]      = "Jantar",
        };

        private readonly IStreakService              _streakService;
        private readonly IDailyFocusRepository        _focusRepository;
        private readonly IFocusLogRepository          _focusLogRepository;
        private readonly IDailyScoreRepository        _scoreRepository;
        private readonly IWorkoutLogRepository        _workoutRepository;
        private readonly INutritionEntryRepository    _nutritionRepository;
        private readonly IUserRepository              _userRepository;
        private readonly IClockService                _clock;

        public ZeloContextBuilder(
            IStreakService              streakService,
            IDailyFocusRepository        focusRepository,
            IFocusLogRepository          focusLogRepository,
            IDailyScoreRepository        scoreRepository,
            IWorkoutLogRepository        workoutRepository,
            INutritionEntryRepository    nutritionRepository,
            IUserRepository              userRepository,
            IClockService                clock) {
            _streakService       = streakService;
            _focusRepository     = focusRepository;
            _focusLogRepository  = focusLogRepository;
            _scoreRepository     = scoreRepository;
            _workoutRepository   = workoutRepository;
            _nutritionRepository = nutritionRepository;
            _userRepository      = userRepository;
            _clock               = clock;
        }

        public async Task<string> BuildAsync(Guid userId, CancellationToken cancellationToken = default) {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
                       ?? throw new NotFoundException("Usuário não encontrado.");

            var today = _clock.TodayIn(user.Timezone);

            var sb = new StringBuilder();
            sb.Append("Dados de ").Append(user.Name).Append(" (hoje: ")
              .Append(today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).AppendLine(").");
            sb.AppendLine();

            await AppendFocusSectionAsync(sb, userId, today, cancellationToken);
            sb.AppendLine();
            await AppendWorkoutSectionAsync(sb, userId, cancellationToken);
            sb.AppendLine();
            await AppendNutritionSectionAsync(sb, userId, today, cancellationToken);

            return sb.ToString().TrimEnd();
        }

        private async Task AppendFocusSectionAsync(StringBuilder sb, Guid userId, DateOnly today, CancellationToken cancellationToken) {
            sb.AppendLine("FOCO/HÁBITOS");

            // GetStatusAsync roda o acerto do streak (mesmo caminho do dashboard) e devolve os
            // números já consolidados de hoje: sequência atual, melhor e se a meta do dia foi batida.
            var streak = await _streakService.GetStatusAsync(userId, cancellationToken);
            sb.Append("- Sequência atual: ").Append(streak.DisplayCount)
              .Append(" dia(s) (melhor: ").Append(streak.BestCount).AppendLine(").");
            sb.Append("- Meta de hoje: ").AppendLine(streak.TodayGoalMet ? "batida." : "ainda não batida.");

            // Média de aproveitamento dos últimos 7 dias. Percentage é fração (0 a 1); dias sem
            // score nenhum ficam de fora, mesmo critério do cálculo de marcos.
            var scores = await _scoreRepository.GetByUserAndDateRangeAsync(userId, today.AddDays(-(LookbackDays - 1)), today, cancellationToken);
            if (scores.Count > 0) {
                var average = (int)Math.Round(scores.Average(s => s.Percentage) * 100m);
                sb.Append("- Média dos últimos ").Append(LookbackDays).Append(" dias: ").Append(average).AppendLine("%.");
            } else {
                sb.Append("- Média dos últimos ").Append(LookbackDays).AppendLine(" dias: sem registros.");
            }

            var focuses = (await _focusRepository.GetAllByUserIdAsync(userId, cancellationToken))
                .Where(f => f.Active)
                .ToList();

            if (focuses.Count == 0) {
                sb.AppendLine("- Focos de hoje: nenhum foco ativo.");
                return;
            }

            // Um log por foco por dia diz se ele foi concluído hoje. Traz todos de uma vez.
            var logs = await _focusLogRepository.GetByFocusIdsAndDateAsync(focuses.Select(f => f.Id).ToList(), today, cancellationToken);
            var completedIds = logs.Where(l => l.Completed).Select(l => l.DailyFocusId).ToHashSet();

            var descriptions = focuses.Select(f =>
                $"{f.Name} ({(completedIds.Contains(f.Id) ? "concluído" : "pendente")})");
            sb.Append("- Focos de hoje: ").Append(string.Join(", ", descriptions)).AppendLine(".");
        }

        private async Task AppendWorkoutSectionAsync(StringBuilder sb, Guid userId, CancellationToken cancellationToken) {
            sb.AppendLine("TREINO (registros recentes)");

            // Já vem ordenado do mais recente para o mais antigo; pegamos só os primeiros.
            var workouts = (await _workoutRepository.GetAllByUserIdAsync(userId, cancellationToken: cancellationToken))
                .Take(RecentWorkoutCount)
                .ToList();

            if (workouts.Count == 0) {
                sb.AppendLine("- Sem registros.");
                return;
            }

            foreach (var w in workouts) {
                var date = w.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                sb.Append("- ").Append(date).Append(", ").Append(w.Type).Append(": ")
                  .AppendLine(DescribeWorkout(w));
            }
        }

        // Cada modalidade mostra só o que a identifica: Academia pelo exercício principal (e carga,
        // quando houver), Corrida pela distância e pace.
        private static string DescribeWorkout(WorkoutLog w) {
            if (w.Type == WorkoutType.Corrida) {
                var parts = new List<string>();
                if (w.DistanceKm is { } km) {
                    parts.Add($"{km.ToString("0.##", CultureInfo.InvariantCulture)}km");
                }
                if (w.PaceMinPerKm is { } pace) {
                    parts.Add($"pace {pace.ToString("0.##", CultureInfo.InvariantCulture)} min/km");
                }
                return parts.Count > 0 ? string.Join(", ", parts) : "corrida registrada";
            }

            var name = string.IsNullOrWhiteSpace(w.ExerciseName) ? "exercício" : w.ExerciseName!.Trim();
            if (w.LoadKg is { } load) {
                return $"{name} {load.ToString("0.##", CultureInfo.InvariantCulture)}kg";
            }
            return name;
        }

        private async Task AppendNutritionSectionAsync(StringBuilder sb, Guid userId, DateOnly today, CancellationToken cancellationToken) {
            // Refeições de hoje ou, se hoje não tiver nada, do dia mais recente com registro dentro
            // da janela. Uma busca por intervalo evita um método novo de repositório só para isso.
            var recent = await _nutritionRepository.GetByUserAndDateRangeAsync(userId, today.AddDays(-(LookbackDays - 1)), today, cancellationToken);

            if (recent.Count == 0) {
                sb.AppendLine("NUTRIÇÃO");
                sb.AppendLine("- Sem registros recentes.");
                return;
            }

            var latestDate = recent.Max(e => e.Date);
            var entries = recent.Where(e => e.Date == latestDate);

            sb.Append("NUTRIÇÃO (").Append(latestDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).AppendLine(")");
            foreach (var e in entries) {
                var meal = MealLabels.TryGetValue(e.MealType, out var label) ? label : e.MealType.ToString();
                sb.Append("- ").Append(meal).Append(": ").Append(e.ItemName);
                if (!string.IsNullOrWhiteSpace(e.Quantity)) {
                    sb.Append(" (").Append(e.Quantity).Append(')');
                }
                sb.AppendLine(".");
            }
        }
    }
}
