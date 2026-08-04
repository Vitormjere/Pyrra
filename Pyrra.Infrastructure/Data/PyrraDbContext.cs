using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pyrra.Domain.Chat;
using Pyrra.Domain.Comunidade;
using Pyrra.Domain.Desafios;
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
        public DbSet<Friendship> Friendships => Set<Friendship>();
        public DbSet<Team> Teams => Set<Team>();
        public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
        public DbSet<TeamInvite> TeamInvites => Set<TeamInvite>();
        public DbSet<Tournament> Tournaments => Set<Tournament>();
        public DbSet<TournamentRequest> TournamentRequests => Set<TournamentRequest>();
        public DbSet<TournamentTeam> TournamentTeams => Set<TournamentTeam>();
        public DbSet<ChallengeCategory> ChallengeCategories => Set<ChallengeCategory>();
        public DbSet<Challenge> Challenges => Set<Challenge>();
        public DbSet<TeamActiveCategory> TeamActiveCategories => Set<TeamActiveCategory>();
        public DbSet<ChallengeSubmission> ChallengeSubmissions => Set<ChallengeSubmission>();
        public DbSet<TeamMemberScore> TeamMemberScores => Set<TeamMemberScore>();
        public DbSet<TournamentChallenge> TournamentChallenges => Set<TournamentChallenge>();
        public DbSet<TournamentOwnChallenge> TournamentOwnChallenges => Set<TournamentOwnChallenge>();
        public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // username é opcional
            modelBuilder.Entity<User>()
                .Property(u => u.Username)
                .HasMaxLength(20);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique()
                .HasFilter("[Username] IS NOT NULL");

            modelBuilder.Entity<User>()
                .Property(u => u.InviteToken)
                .HasMaxLength(32);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.InviteToken)
                .IsUnique()
                .HasFilter("[InviteToken] IS NOT NULL");

            // rótulo curto de tela ("beber agua") 
            modelBuilder.Entity<DailyFocus>()
                .Property(f => f.Name)
                .HasMaxLength(100);

            // um único score por usuário/dia 
            modelBuilder.Entity<DailyScore>()
                .HasIndex(s => new { s.UserId, s.Date })
                .IsUnique();

            // percentage guarda fração entre 0 e 1 (ex.: 0.7143)
            modelBuilder.Entity<DailyScore>()
                .Property(s => s.Percentage)
                .HasPrecision(5, 4);

            // streak e FreezeBank são um-por-usuário 
            modelBuilder.Entity<Streak>()
                .HasIndex(s => s.UserId)
                .IsUnique();

            modelBuilder.Entity<FreezeBank>()
                .HasIndex(b => b.UserId)
                .IsUnique();

            // não é único por usuário 
            modelBuilder.Entity<PendingMilestone>()
                .HasIndex(m => new { m.UserId, m.AcknowledgedAt });

            modelBuilder.Entity<PendingMilestone>()
                .Property(m => m.AveragePercentage)
                .HasPrecision(5, 4);

            // mesmo padrão do PendingMilestone
            modelBuilder.Entity<PendingFreezeUse>()
                .HasIndex(f => new { f.UserId, f.AcknowledgedAt });

            // precisão explícita
            modelBuilder.Entity<WorkoutLog>()
                .Property(w => w.LoadKg)
                .HasPrecision(6, 2);

            modelBuilder.Entity<WorkoutLog>()
                .Property(w => w.DistanceKm)
                .HasPrecision(6, 3);

            modelBuilder.Entity<WorkoutLog>()
                .Property(w => w.PaceMinPerKm)
                .HasPrecision(5, 3);

            // limite explícito porque o nome entra num índice 
            modelBuilder.Entity<WorkoutLog>()
                .Property(w => w.ExerciseName)
                .HasMaxLength(200);

            // cobre os dois caminhos de leitura do módulo 
            modelBuilder.Entity<WorkoutLog>()
                .HasIndex(w => new { w.UserId, w.Date });

            modelBuilder.Entity<WorkoutLog>()
                .HasIndex(w => new { w.UserId, w.Type, w.ExerciseName });

            // uma nota por usuário/dia 
            modelBuilder.Entity<DailyPlanNote>()
                .HasIndex(n => new { n.UserId, n.Date })
                .IsUnique();

            // não é único, um dia tem várias tarefas
            modelBuilder.Entity<PriorityTask>()
                .HasIndex(t => new { t.UserId, t.Date });

            // título é texto curto de tela 
            modelBuilder.Entity<PriorityTask>()
                .Property(t => t.Title)
                .HasMaxLength(500);

            // (18,2) explícito porque aqui é regra de negócio (centavos), não coincidência com o default do SQL Server
            modelBuilder.Entity<FinanceEntry>()
                .Property(e => e.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<FinanceEntry>()
                .HasIndex(e => new { e.UserId, e.Date });

            // mesmo limite que o DTO já impõe
            modelBuilder.Entity<FinanceEntry>()
                .Property(e => e.Description)
                .HasMaxLength(500);

            modelBuilder.Entity<FinanceCategory>()
                .Property(c => c.Name)
                .HasMaxLength(100);

            // cobre a listagem, que busca as padrão (UserId null) e as do usuário numa query só
            modelBuilder.Entity<FinanceCategory>()
                .HasIndex(c => c.UserId);

            // cobre as duas leituras do módulo
            modelBuilder.Entity<NutritionEntry>()
                .HasIndex(e => new { e.UserId, e.Date });

            modelBuilder.Entity<NutritionEntry>()
                .Property(e => e.ItemName)
                .HasMaxLength(200);

            modelBuilder.Entity<NutritionEntry>()
                .Property(e => e.Quantity)
                .HasMaxLength(100);

            // um plano por usuário/dia da semana 
            modelBuilder.Entity<WorkoutPlanDay>()
                .HasIndex(d => new { d.UserId, d.DayOfWeek })
                .IsUnique();

            modelBuilder.Entity<WorkoutPlanDay>()
                .Property(d => d.Label)
                .HasMaxLength(200);

            // não é único, um dia tem vários exercícios planejados
            modelBuilder.Entity<WorkoutPlanExercise>()
                .HasIndex(e => new { e.UserId, e.DayOfWeek });

            modelBuilder.Entity<WorkoutPlanExercise>()
                .Property(e => e.ExerciseName)
                .HasMaxLength(200);

            // não é único, uma refeição planejada tem vários itens
            modelBuilder.Entity<NutritionPlanItem>()
                .HasIndex(i => new { i.UserId, i.DayOfWeek });

            modelBuilder.Entity<NutritionPlanItem>()
                .Property(i => i.ItemName)
                .HasMaxLength(200);

            modelBuilder.Entity<NutritionPlanItem>()
                .Property(i => i.Quantity)
                .HasMaxLength(100);

            // uma marca por usuário/dia
            modelBuilder.Entity<NutritionPlanSeedLog>()
                .HasIndex(l => new { l.UserId, l.Date })
                .IsUnique();

            // um contador por usuário/dia 
            modelBuilder.Entity<ZeloQueryLog>()
                .HasIndex(l => new { l.UserId, l.Date })
                .IsUnique();

            // sem FK pra Users, convenção do projeto 
            modelBuilder.Entity<Friendship>()
                .HasIndex(f => new { f.RequesterId, f.AddresseeId })
                .IsUnique();

            // cobre as duas leituras do módulo
            modelBuilder.Entity<Friendship>()
                .HasIndex(f => new { f.AddresseeId, f.Status });

            modelBuilder.Entity<Friendship>()
                .HasIndex(f => new { f.RequesterId, f.Status });

            // sem FK pra Users, mesma convenção de Amigos 
            modelBuilder.Entity<Team>()
                .Property(t => t.Name)
                .HasMaxLength(100);

            modelBuilder.Entity<Team>()
                .Property(t => t.Description)
                .HasMaxLength(500);

            // token de convite do time, mesmo padrão do InviteToken do User 
            modelBuilder.Entity<Team>()
                .Property(t => t.InviteToken)
                .HasMaxLength(32);

            modelBuilder.Entity<Team>()
                .HasIndex(t => t.InviteToken)
                .IsUnique()
                .HasFilter("[InviteToken] IS NOT NULL");

            // usado no filtro de times por visibilidade na aba Explorar
            modelBuilder.Entity<Team>()
                .HasIndex(t => t.Visibility);

            // url do blob no Azure Storage 
            modelBuilder.Entity<Team>()
                .Property(t => t.BannerImageUrl)
                .HasMaxLength(500);

            // um usuário não pode ter duas linhas no mesmo time; o índice solto em UserId cobre GetForUserAsync
            modelBuilder.Entity<TeamMember>()
                .HasIndex(m => new { m.TeamId, m.UserId })
                .IsUnique();

            modelBuilder.Entity<TeamMember>()
                .HasIndex(m => m.UserId);

            // convite direto — uma linha por (TeamId, InviteeId), reaproveitada em Recusado, mesmo padrão de Friendship
            modelBuilder.Entity<TeamInvite>()
                .HasIndex(i => new { i.TeamId, i.InviteeId })
                .IsUnique();

            modelBuilder.Entity<TeamInvite>()
                .HasIndex(i => new { i.InviteeId, i.Status });

            // sem FK, mesma convenção do projeto
            modelBuilder.Entity<ChallengeCategory>()
                .Property(c => c.Name)
                .HasMaxLength(100);

            modelBuilder.Entity<ChallengeCategory>()
                .Property(c => c.Description)
                .HasMaxLength(500);

            modelBuilder.Entity<ChallengeCategory>()
                .Property(c => c.Icon)
                .HasMaxLength(50);

            modelBuilder.Entity<Challenge>()
                .Property(c => c.Title)
                .HasMaxLength(200);

            modelBuilder.Entity<Challenge>()
                .Property(c => c.Description)
                .HasMaxLength(1000);

            // cobre a listagem por categoria (admin) e os desafios disponíveis a partir das categorias ativas do time
            modelBuilder.Entity<Challenge>()
                .HasIndex(c => c.CategoryId);

            // um time não pode ativar a mesma categoria duas vezes 
            modelBuilder.Entity<TeamActiveCategory>()
                .HasIndex(a => new { a.TeamId, a.CategoryId })
                .IsUnique();

            // url do blob — nunca filtrada, só exibida, mesmo critério do BannerImageUrl de Team
            modelBuilder.Entity<ChallengeSubmission>()
                .Property(s => s.PhotoUrl)
                .HasMaxLength(500);

            // quantidade da contribuição — mesma precisão de TournamentChallenge.Goal, já que uma é comparada contra a outra pro progresso
            modelBuilder.Entity<ChallengeSubmission>()
                .Property(s => s.Quantity)
                .HasPrecision(9, 2);

            // cobre a checagem de duplicidade ao enviar e a listagem de desafios disponíveis (usuário+time)
            modelBuilder.Entity<ChallengeSubmission>()
                .HasIndex(s => new { s.UserId, s.ChallengeId, s.TeamId });

            // cobre a fila de aprovação do dono do time
            modelBuilder.Entity<ChallengeSubmission>()
                .HasIndex(s => new { s.TeamId, s.Status });

            // cobre a fila de aprovação do dono do torneio, separada da fila do dono do time acima
            modelBuilder.Entity<ChallengeSubmission>()
                .HasIndex(s => new { s.TournamentId, s.Status });

            // sem FK pra Users, mesma convenção de Team
            modelBuilder.Entity<Tournament>()
                .Property(t => t.Name)
                .HasMaxLength(100);

            modelBuilder.Entity<Tournament>()
                .Property(t => t.Description)
                .HasMaxLength(500);

            // token de convite do torneio, mesmo padrão de Team.InviteToken
            modelBuilder.Entity<Tournament>()
                .Property(t => t.InviteToken)
                .HasMaxLength(32);

            modelBuilder.Entity<Tournament>()
                .HasIndex(t => t.InviteToken)
                .IsUnique()
                .HasFilter("[InviteToken] IS NOT NULL");

            // url do blob — nunca filtrada, só exibida, mesmo critério do BannerImageUrl de Team
            modelBuilder.Entity<Tournament>()
                .Property(t => t.BannerImageUrl)
                .HasMaxLength(500);

            modelBuilder.Entity<TournamentRequest>()
                .Property(r => r.ProposedName)
                .HasMaxLength(100);

            modelBuilder.Entity<TournamentRequest>()
                .Property(r => r.ProposedDescription)
                .HasMaxLength(500);

            // cobre a listagem de solicitações pendentes (admin)
            modelBuilder.Entity<TournamentRequest>()
                .HasIndex(r => r.Status);

            modelBuilder.Entity<TournamentTeam>()
                .HasIndex(t => t.TeamId);

            // cobre a fila de aprovação do dono do torneio e o ranking
            modelBuilder.Entity<TournamentTeam>()
                .HasIndex(t => new { t.TournamentId, t.Status });

            // placar individual — um por (TeamId, UserId), mesma pessoa em times diferentes tem linhas separadas; TeamId à esquerda já cobre o ranking completo do time
            modelBuilder.Entity<TeamMemberScore>()
                .HasIndex(s => new { s.TeamId, s.UserId })
                .IsUnique();

            // vínculo de um desafio do catálogo a um torneio 
            modelBuilder.Entity<TournamentChallenge>()
                .HasIndex(l => new { l.TournamentId, l.ChallengeId })
                .IsUnique();

            // meta cumulativa do vínculo 
            modelBuilder.Entity<TournamentChallenge>()
                .Property(l => l.Goal)
                .HasPrecision(9, 2);

            modelBuilder.Entity<TournamentChallenge>()
                .Property(l => l.Unit)
                .HasMaxLength(30);

            // desafio próprio de um torneio, sem categoria 
            modelBuilder.Entity<TournamentOwnChallenge>()
                .Property(c => c.Title)
                .HasMaxLength(200);

            modelBuilder.Entity<TournamentOwnChallenge>()
                .Property(c => c.Description)
                .HasMaxLength(1000);

            // meta cumulativa 
            modelBuilder.Entity<TournamentOwnChallenge>()
                .Property(c => c.Goal)
                .HasPrecision(9, 2);

            modelBuilder.Entity<TournamentOwnChallenge>()
                .Property(c => c.Unit)
                .HasMaxLength(30);

            // cobre a listagem de desafios próprios de um torneio
            modelBuilder.Entity<TournamentOwnChallenge>()
                .HasIndex(c => c.TournamentId);

            // chat entre admin e jogadores —
            modelBuilder.Entity<ChatMessage>()
                .Property(m => m.Content)
                .HasMaxLength(2000);

            modelBuilder.Entity<ChatMessage>()
                .HasIndex(m => new { m.SenderId, m.RecipientId });

            modelBuilder.Entity<ChatMessage>()
                .HasIndex(m => new { m.RecipientId, m.SenderId });

            // este é só pra contagem de não lidas (RecipientId + ReadAt) 
            modelBuilder.Entity<ChatMessage>()
                .HasIndex(m => new { m.RecipientId, m.ReadAt });
        }
    }
}