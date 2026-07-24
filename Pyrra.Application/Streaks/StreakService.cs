using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Application.Focos;
using Pyrra.Domain.Focos;
using Pyrra.Domain.Users;

namespace Pyrra.Application.Streaks {
    public class StreakService : IStreakService {
        private const int MaxFreezes = 5;

        // Teto de dias avaliados por chamada. Um usuário que some por dois anos não deve gerar uma
        // varredura de 700 dias — como o streak zera no primeiro dia não perdoado, o resultado é o
        // mesmo. Dias anteriores ao corte são dados por acertados sem avaliação.
        private const int MaxDaysPerSettlement = 400;

        private readonly IStreakRepository            _streakRepository;
        private readonly IFreezeBankRepository        _freezeBankRepository;
        private readonly IDailyScoreRepository        _scoreRepository;
        private readonly IUserRepository              _userRepository;
        private readonly IPendingMilestoneRepository  _pendingMilestoneRepository;
        private readonly IPendingFreezeUseRepository  _pendingFreezeUseRepository;
        private readonly IDailyScoreCalculator        _calculator;
        private readonly IClockService                _clock;

        public StreakService(
            IStreakRepository            streakRepository,
            IFreezeBankRepository        freezeBankRepository,
            IDailyScoreRepository        scoreRepository,
            IUserRepository              userRepository,
            IPendingMilestoneRepository  pendingMilestoneRepository,
            IPendingFreezeUseRepository  pendingFreezeUseRepository,
            IDailyScoreCalculator        calculator,
            IClockService                clock) {
            _streakRepository            = streakRepository;
            _freezeBankRepository        = freezeBankRepository;
            _scoreRepository             = scoreRepository;
            _userRepository              = userRepository;
            _pendingMilestoneRepository  = pendingMilestoneRepository;
            _pendingFreezeUseRepository  = pendingFreezeUseRepository;
            _calculator                  = calculator;
            _clock                       = clock;
        }

        public async Task<StreakSettlementResult> SettleStreakAsync(Guid userId, CancellationToken cancellationToken = default) {
            var user = await GetUserAsync(userId, cancellationToken);
            var today = _clock.TodayIn(user.Timezone);

            var streak = await _streakRepository.GetByUserIdAsync(userId, cancellationToken)
                         ?? NewStreak(userId, user, today);
            var bank = await _freezeBankRepository.GetByUserIdAsync(userId, cancellationToken)
                       ?? NewFreezeBank(userId, today);

            GrantWeeklyFreezes(bank, today);

            var (milestones, freezeUses) = await SettleDaysAsync(userId, streak, bank, today, cancellationToken);

            await _streakRepository.UpsertAsync(streak, cancellationToken);
            await _freezeBankRepository.UpsertAsync(bank, cancellationToken);

            // Persiste ANTES de devolver: o acerto pode ter vindo de um check-in, cujo chamador
            // descarta este resultado. Sem gravar, o marco (ou o freeze gasto) seria detectado e
            // perdido para sempre.
            await PersistPendingMilestonesAsync(userId, milestones, cancellationToken);
            await PersistPendingFreezeUsesAsync(userId, freezeUses, cancellationToken);

            return new StreakSettlementResult(streak.CurrentCount, streak.BestCount, bank.FreezesAvailable, milestones);
        }

        public async Task<StreakStatusResult> GetStatusAsync(Guid userId, CancellationToken cancellationToken = default) {
            var settlement = await SettleStreakAsync(userId, cancellationToken);

            var user  = await GetUserAsync(userId, cancellationToken);
            var today = _clock.TodayIn(user.Timezone);

            // O dia corrente conta só visualmente: ele ainda pode ser desfeito por um toggle antes
            // da virada, então só entra de verdade quando virar passado e for acertado.
            var todayScore   = await _calculator.CalculateLiveAsync(userId, today, cancellationToken);
            var todayGoalMet = todayScore.Score.GoalMet;

            return new StreakStatusResult(
                settlement.CurrentCount,
                settlement.BestCount,
                settlement.FreezesAvailable,
                todayGoalMet,
                settlement.CurrentCount + (todayGoalMet ? 1 : 0),
                settlement.MilestonesReached);
        }

        // Avalia dia a dia, em ordem cronológica: o freeze consumido num dia altera o saldo
        // disponível para os dias seguintes do mesmo lote, então a ordem importa. Devolve os
        // marcos cruzados e os dias perdoados por freeze — ambos viram avisos pendentes.
        //
        // Cada dia é acertado no máximo uma vez na vida (LastSettledDate só avança e é
        // persistido), então registrar o freeze gasto aqui não corre risco de duplicar.
        private async Task<(IReadOnlyList<MilestoneReached> Milestones, IReadOnlyList<DateOnly> FreezeUses)> SettleDaysAsync(
            Guid userId, Streak streak, FreezeBank bank, DateOnly today, CancellationToken cancellationToken) {
            var yesterday = today.AddDays(-1);

            var start = streak.LastSettledDate.AddDays(1);
            var floor = today.AddDays(-MaxDaysPerSettlement);
            if (start < floor) {
                start = floor;
            }

            if (start > yesterday) {
                return (Array.Empty<MilestoneReached>(), Array.Empty<DateOnly>());
            }

            var scores = await _scoreRepository.GetByUserAndDateRangeAsync(userId, start, yesterday, cancellationToken);
            var scoreByDate = scores.ToDictionary(s => s.Date);

            var milestones = new List<MilestoneReached>();
            var freezeUses = new List<DateOnly>();

            for (var date = start; date <= yesterday; date = date.AddDays(1)) {
                scoreByDate.TryGetValue(date, out var score);

                // Dia sem DailyScore nenhum = usuário não apareceu = meta não batida.
                if (score?.GoalMet == true) {
                    streak.CurrentCount++;
                    streak.StreakStartDate ??= date;

                    if (streak.CurrentCount > streak.BestCount) {
                        streak.BestCount = streak.CurrentCount;
                    }

                    if (StreakMilestones.IsMilestone(streak.CurrentCount)) {
                        var windowStart = streak.LastMilestoneDate?.AddDays(1) ?? streak.StreakStartDate.Value;
                        var average = await AveragePercentageAsync(userId, windowStart, date, cancellationToken);

                        milestones.Add(new MilestoneReached(streak.CurrentCount, average, date));
                        streak.LastMilestoneDate = date;
                    }
                } else if (streak.CurrentCount > 0 && bank.FreezesAvailable > 0) {
                    // Dia perdoado: consome freeze e preserva a sequência. Só faz sentido gastar
                    // freeze quando há streak a proteger — com o contador zerado não há o que perder.
                    bank.FreezesAvailable--;
                    freezeUses.Add(date);
                } else {
                    streak.CurrentCount      = 0;
                    streak.StreakStartDate   = null;
                    streak.LastMilestoneDate = null;
                }

                streak.LastSettledDate = date;
            }

            return (milestones, freezeUses);
        }

        public async Task<IReadOnlyList<PendingMilestoneItem>> GetPendingMilestonesAsync(Guid userId, CancellationToken cancellationToken = default) {
            // Acerta antes de ler: um marco cruzado agora deve aparecer já nesta consulta.
            await SettleStreakAsync(userId, cancellationToken);

            var pending = await _pendingMilestoneRepository.GetPendingByUserIdAsync(userId, cancellationToken);
            return pending
                .Select(m => new PendingMilestoneItem(m.Id, m.Milestone, m.AveragePercentage, m.ReachedDate))
                .ToList();
        }

        public Task<int> AcknowledgeMilestonesAsync(Guid userId, IReadOnlyCollection<Guid>? ids, CancellationToken cancellationToken = default) =>
            _pendingMilestoneRepository.AcknowledgeAsync(userId, ids, _clock.UtcNow, cancellationToken);

        public async Task<IReadOnlyList<PendingFreezeUseItem>> GetPendingFreezeUsesAsync(Guid userId, CancellationToken cancellationToken = default) {
            // Acerta antes de ler: um freeze gasto agora deve aparecer já nesta consulta.
            await SettleStreakAsync(userId, cancellationToken);

            var pending = await _pendingFreezeUseRepository.GetPendingByUserIdAsync(userId, cancellationToken);
            return pending
                .Select(f => new PendingFreezeUseItem(f.Id, f.Date))
                .ToList();
        }

        public Task<int> AcknowledgeFreezeUsesAsync(Guid userId, IReadOnlyCollection<Guid>? ids, CancellationToken cancellationToken = default) =>
            _pendingFreezeUseRepository.AcknowledgeAsync(userId, ids, _clock.UtcNow, cancellationToken);

        private async Task PersistPendingMilestonesAsync(Guid userId, IReadOnlyList<MilestoneReached> milestones, CancellationToken cancellationToken) {
            if (milestones.Count == 0) {
                return;
            }

            var pending = milestones
                .Select(m => new PendingMilestone {
                    Id                = Guid.NewGuid(),
                    UserId            = userId,
                    Milestone         = m.Milestone,
                    AveragePercentage = m.AveragePercentage,
                    ReachedDate       = m.ReachedDate,
                    CreatedAt         = _clock.UtcNow
                })
                .ToList();

            await _pendingMilestoneRepository.AddRangeAsync(pending, cancellationToken);
        }

        private async Task PersistPendingFreezeUsesAsync(Guid userId, IReadOnlyList<DateOnly> freezeUses, CancellationToken cancellationToken) {
            if (freezeUses.Count == 0) {
                return;
            }

            var pending = freezeUses
                .Select(date => new PendingFreezeUse {
                    Id             = Guid.NewGuid(),
                    UserId         = userId,
                    Date           = date,
                    CreatedAt      = _clock.UtcNow
                })
                .ToList();

            await _pendingFreezeUseRepository.AddRangeAsync(pending, cancellationToken);
        }

        // Média do aproveitamento no trecho que levou ao marco. Considera os DailyScore existentes
        // na janela; dias sem score nenhum ficam de fora em vez de entrar como 0%.
        private async Task<decimal> AveragePercentageAsync(Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken) {
            var scores = await _scoreRepository.GetByUserAndDateRangeAsync(userId, startDate, endDate, cancellationToken);
            if (scores.Count == 0) {
                return 0m;
            }

            return decimal.Round(scores.Average(s => s.Percentage), 4);
        }

        // 1 freeze por semana completa passada desde a última concessão, respeitando o teto.
        private static void GrantWeeklyFreezes(FreezeBank bank, DateOnly today) {
            var currentWeekStart = StartOfWeek(today);
            var weeksElapsed = (currentWeekStart.DayNumber - bank.LastGrantedWeekStart.DayNumber) / 7;

            if (weeksElapsed <= 0) {
                return;
            }

            bank.FreezesAvailable     = Math.Min(MaxFreezes, bank.FreezesAvailable + weeksElapsed);
            bank.LastGrantedWeekStart = currentWeekStart;
        }

        // Semana começa na segunda-feira.
        private static DateOnly StartOfWeek(DateOnly date) =>
            date.AddDays(-(((int)date.DayOfWeek + 6) % 7));

        // Streak novo começa a valer no dia do cadastro: assim um usuário recém-registrado não
        // recebe um passivo de dias perdidos anteriores à própria existência da conta.
        private Streak NewStreak(Guid userId, User user, DateOnly today) {
            var registrationDate = _clock.ToLocalDate(user.CreatedAt, user.Timezone);
            var firstDayToSettle = registrationDate > today ? today : registrationDate;

            return new Streak {
                Id              = Guid.NewGuid(),
                UserId          = userId,
                CurrentCount    = 0,
                BestCount       = 0,
                LastSettledDate = firstDayToSettle.AddDays(-1)
            };
        }

        // Nasce com 1 freeze: é a concessão da semana corrente, que de outra forma só viria na
        // virada da próxima semana.
        private static FreezeBank NewFreezeBank(Guid userId, DateOnly today) =>
            new() {
                Id                   = Guid.NewGuid(),
                UserId               = userId,
                FreezesAvailable     = 1,
                LastGrantedWeekStart = StartOfWeek(today)
            };

        private async Task<User> GetUserAsync(Guid userId, CancellationToken cancellationToken) {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null) {
                throw new NotFoundException("Usuário não encontrado.");
            }
            return user;
        }
    }
}
