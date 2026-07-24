using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pyrra.Domain.Financas;
using Pyrra.Domain.Focos;
using Pyrra.Domain.Nutricao;
using Pyrra.Domain.Planejamento;
using Pyrra.Domain.Tarefas;
using Pyrra.Domain.Treinos;
using Pyrra.Domain.Users;
using Pyrra.Domain.Zelo;

namespace Pyrra.Infrastructure.Data {
    public class PyrraDbContext : DbContext {
        public PyrraDbContext(DbContextOptions<PyrraDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<DailyFocus> DailyFocuses => Set<DailyFocus>();
        public DbSet<FocusLog> FocusLogs => Set<FocusLog>();
        public DbSet<DailyScore> DailyScores => Set<DailyScore>();
        public DbSet<Streak> Streaks => Set<Streak>();
        public DbSet<FreezeBank> FreezeBanks => Set<FreezeBank>();
        public DbSet<PendingMilestone> PendingMilestones => Set<PendingMilestone>();
        public DbSet<PendingFreezeUse> PendingFreezeUses => Set<PendingFreezeUse>();
        public DbSet<WorkoutLog> WorkoutLogs => Set<WorkoutLog>();
        public DbSet<DailyPlanNote> DailyPlanNotes => Set<DailyPlanNote>();
        public DbSet<PriorityTask> PriorityTasks => Set<PriorityTask>();
        public DbSet<FinanceCategory> FinanceCategories => Set<FinanceCategory>();
        public DbSet<FinanceEntry> FinanceEntries => Set<FinanceEntry>();
        public DbSet<NutritionEntry> NutritionEntries => Set<NutritionEntry>();
        public DbSet<WorkoutPlanDay> WorkoutPlanDays => Set<WorkoutPlanDay>();
        public DbSet<WorkoutPlanExercise> WorkoutPlanExercises => Set<WorkoutPlanExercise>();
        public DbSet<NutritionPlanItem> NutritionPlanItems => Set<NutritionPlanItem>();
        public DbSet<NutritionPlanSeedLog> NutritionPlanSeedLogs => Set<NutritionPlanSeedLog>();
        public DbSet<ZeloQueryLog> ZeloQueryLogs => Set<ZeloQueryLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Nome de foco é rótulo curto de tela ("beber agua"). Sem limite a coluna nasceria
            // nvarchar(max), que nem indexar dá — e o mesmo 100 vale no CreateFocusRequest e no
            // maxLength do campo no frontend.
            modelBuilder.Entity<DailyFocus>()
                .Property(f => f.Name)
                .HasMaxLength(100);

            // Um único score por usuário/dia: garante no banco a semântica de upsert do DailyScoreRepository.
            modelBuilder.Entity<DailyScore>()
                .HasIndex(s => new { s.UserId, s.Date })
                .IsUnique();

            // Percentage guarda uma fração entre 0 e 1 (ex.: 0.7143); o default do SQL Server
            // seria decimal(18,2) e arredondaria para 0.71.
            modelBuilder.Entity<DailyScore>()
                .Property(s => s.Percentage)
                .HasPrecision(5, 4);

            // Streak e FreezeBank são um-por-usuário: o índice único garante no banco a semântica
            // de upsert dos repositórios.
            modelBuilder.Entity<Streak>()
                .HasIndex(s => s.UserId)
                .IsUnique();

            modelBuilder.Entity<FreezeBank>()
                .HasIndex(b => b.UserId)
                .IsUnique();

            // NÃO é único por usuário: o mesmo marco pode ser atingido de novo depois de o streak
            // zerar. O índice serve à consulta de pendentes, que é a única leitura da tabela.
            modelBuilder.Entity<PendingMilestone>()
                .HasIndex(m => new { m.UserId, m.AcknowledgedAt });

            modelBuilder.Entity<PendingMilestone>()
                .Property(m => m.AveragePercentage)
                .HasPrecision(5, 4);

            // Mesmo padrão do PendingMilestone: não é único (o mesmo dia nunca reaparece, mas
            // vários dias perdoados podem ficar pendentes ao mesmo tempo). O índice serve à
            // consulta de pendentes, a única leitura da tabela.
            modelBuilder.Entity<PendingFreezeUse>()
                .HasIndex(f => new { f.UserId, f.AcknowledgedAt });

            // Precisão explícita: o default do SQL Server para decimal é (18,2), que arredondaria
            // o pace (ex.: 5,375 min/km) e a distância de treinos curtos.
            modelBuilder.Entity<WorkoutLog>()
                .Property(w => w.LoadKg)
                .HasPrecision(6, 2);

            modelBuilder.Entity<WorkoutLog>()
                .Property(w => w.DistanceKm)
                .HasPrecision(6, 3);

            modelBuilder.Entity<WorkoutLog>()
                .Property(w => w.PaceMinPerKm)
                .HasPrecision(5, 3);

            // Limite explícito porque o nome entra num índice — nvarchar(max) não pode ser indexado.
            modelBuilder.Entity<WorkoutLog>()
                .Property(w => w.ExerciseName)
                .HasMaxLength(200);

            // Os dois caminhos de leitura do módulo: a listagem (opcionalmente filtrada por tipo)
            // e o histórico de um exercício de Academia.
            modelBuilder.Entity<WorkoutLog>()
                .HasIndex(w => new { w.UserId, w.Date });

            modelBuilder.Entity<WorkoutLog>()
                .HasIndex(w => new { w.UserId, w.Type, w.ExerciseName });

            // Uma única nota por usuário/dia: garante no banco a semântica de upsert do
            // DailyPlanNoteRepository, mesmo critério do DailyScore.
            modelBuilder.Entity<DailyPlanNote>()
                .HasIndex(n => new { n.UserId, n.Date })
                .IsUnique();

            // NÃO é único: o dia tem várias tarefas. O índice serve às duas leituras do módulo —
            // as tarefas de um dia e as pendentes de um intervalo de dias.
            modelBuilder.Entity<PriorityTask>()
                .HasIndex(t => new { t.UserId, t.Date });

            // Título é texto curto de tela; sem limite viraria nvarchar(max) à toa.
            modelBuilder.Entity<PriorityTask>()
                .Property(t => t.Title)
                .HasMaxLength(500);

            // Dinheiro: (18,2) explícito. O default do SQL Server para decimal também é (18,2),
            // mas aqui a escala é regra de negócio (centavos), não coincidência de default.
            modelBuilder.Entity<FinanceEntry>()
                .Property(e => e.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<FinanceEntry>()
                .HasIndex(e => new { e.UserId, e.Date });

            // Mesmo limite que o DTO já impõe: sem isso a coluna viraria nvarchar(max) e o
            // banco aceitaria o que a API recusa.
            modelBuilder.Entity<FinanceEntry>()
                .Property(e => e.Description)
                .HasMaxLength(500);

            modelBuilder.Entity<FinanceCategory>()
                .Property(c => c.Name)
                .HasMaxLength(100);

            // Cobre a listagem, que busca as padrão (UserId null) e as do usuário numa query só.
            modelBuilder.Entity<FinanceCategory>()
                .HasIndex(c => c.UserId);

            // Cobre as duas leituras do módulo: o dia (igualdade) e a semana (intervalo).
            modelBuilder.Entity<NutritionEntry>()
                .HasIndex(e => new { e.UserId, e.Date });

            modelBuilder.Entity<NutritionEntry>()
                .Property(e => e.ItemName)
                .HasMaxLength(200);

            modelBuilder.Entity<NutritionEntry>()
                .Property(e => e.Quantity)
                .HasMaxLength(100);

            // Um plano por usuário/dia da semana: o índice único garante no banco a
            // semântica de upsert do WorkoutPlanDayRepository.
            modelBuilder.Entity<WorkoutPlanDay>()
                .HasIndex(d => new { d.UserId, d.DayOfWeek })
                .IsUnique();

            modelBuilder.Entity<WorkoutPlanDay>()
                .Property(d => d.Label)
                .HasMaxLength(200);

            // NÃO é único: um dia tem vários exercícios planejados.
            modelBuilder.Entity<WorkoutPlanExercise>()
                .HasIndex(e => new { e.UserId, e.DayOfWeek });

            modelBuilder.Entity<WorkoutPlanExercise>()
                .Property(e => e.ExerciseName)
                .HasMaxLength(200);

            // NÃO é único: uma refeição planejada tem vários itens.
            modelBuilder.Entity<NutritionPlanItem>()
                .HasIndex(i => new { i.UserId, i.DayOfWeek });

            modelBuilder.Entity<NutritionPlanItem>()
                .Property(i => i.ItemName)
                .HasMaxLength(200);

            modelBuilder.Entity<NutritionPlanItem>()
                .Property(i => i.Quantity)
                .HasMaxLength(100);

            // Uma marca por usuário/dia. O índice único é o que garante a idempotência da
            // semeadura mesmo com duas requisições simultâneas.
            modelBuilder.Entity<NutritionPlanSeedLog>()
                .HasIndex(l => new { l.UserId, l.Date })
                .IsUnique();

            // Um contador por usuário/dia: o índice único garante no banco a semântica de upsert do
            // ZeloQueryLogRepository, mesmo critério do DailyScore.
            modelBuilder.Entity<ZeloQueryLog>()
                .HasIndex(l => new { l.UserId, l.Date })
                .IsUnique();
        }
    }
}