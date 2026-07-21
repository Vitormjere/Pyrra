using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pyrra.Domain.Focos;
using Pyrra.Domain.Planejamento;
using Pyrra.Domain.Treinos;
using Pyrra.Domain.Users;

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
        public DbSet<WorkoutLog> WorkoutLogs => Set<WorkoutLog>();
        public DbSet<DailyPlanNote> DailyPlanNotes => Set<DailyPlanNote>();

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

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
        }
    }
}