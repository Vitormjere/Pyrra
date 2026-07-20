using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pyrra.Domain.Focos;
using Pyrra.Domain.Users;

namespace Pyrra.Infrastructure.Data {
    public class PyrraDbContext : DbContext {
        public PyrraDbContext(DbContextOptions<PyrraDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<DailyFocus> DailyFocuses => Set<DailyFocus>();
        public DbSet<FocusLog> FocusLogs => Set<FocusLog>();
        public DbSet<DailyScore> DailyScores => Set<DailyScore>();

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
        }
    }
}