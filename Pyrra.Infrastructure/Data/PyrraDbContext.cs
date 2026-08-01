using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
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

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Username é público e único, mas OPCIONAL (nulo até o usuário escolher). Índice único
            // FILTRADO em NOT NULL: sem o filtro, os vários nulos dos usuários antigos colidiriam
            // entre si. O nome IX_Users_Username é o que o UserRepository procura para traduzir a
            // violação de corrida.
            modelBuilder.Entity<User>()
                .Property(u => u.Username)
                .HasMaxLength(20);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique()
                .HasFilter("[Username] IS NOT NULL");

            // Token do link de convite: 32 hex, único e opcional. Índice único filtrado pelo mesmo
            // motivo do username. Usado na busca por token ao abrir um convite.
            modelBuilder.Entity<User>()
                .Property(u => u.InviteToken)
                .HasMaxLength(32);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.InviteToken)
                .IsUnique()
                .HasFilter("[InviteToken] IS NOT NULL");

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

            // Amizade: sem FK para Users, seguindo a convenção do projeto (os módulos guardam UserId
            // sem constraint). Uma linha por par (A,B) numa direção: o índice único barra o pedido
            // duplicado exato no banco; o pedido recíproco (B→A) é barrado no FriendshipService.
            modelBuilder.Entity<Friendship>()
                .HasIndex(f => new { f.RequesterId, f.AddresseeId })
                .IsUnique();

            // As duas leituras do módulo: pedidos recebidos/contagem (addressee + status) e a lista
            // de amigos (que filtra por status e pelos dois lados).
            modelBuilder.Entity<Friendship>()
                .HasIndex(f => new { f.AddresseeId, f.Status });

            modelBuilder.Entity<Friendship>()
                .HasIndex(f => new { f.RequesterId, f.Status });

            // Time: sem FK para Users, mesma convenção do módulo de Amigos. O dono é só um campo
            // (OwnerId) — não tem linha própria em TeamMember, ver comentário na entidade Team.
            modelBuilder.Entity<Team>()
                .Property(t => t.Name)
                .HasMaxLength(100);

            modelBuilder.Entity<Team>()
                .Property(t => t.Description)
                .HasMaxLength(500);

            // Token do link de convite do time: 32 hex, único. Mesmo padrão do InviteToken do User,
            // ainda que aqui o token seja sempre gerado na criação (nunca nulo na prática) — o filtro
            // continua inofensivo e consistente com o restante do contexto.
            modelBuilder.Entity<Team>()
                .Property(t => t.InviteToken)
                .HasMaxLength(32);

            modelBuilder.Entity<Team>()
                .HasIndex(t => t.InviteToken)
                .IsUnique()
                .HasFilter("[InviteToken] IS NOT NULL");

            // Único filtro novo de leitura (aba Explorar): times por Visibilidade.
            modelBuilder.Entity<Team>()
                .HasIndex(t => t.Visibility);

            // URL do blob no Azure Storage — nunca é filtrada, só exibida, então sem índice.
            modelBuilder.Entity<Team>()
                .Property(t => t.BannerImageUrl)
                .HasMaxLength(500);

            // Um usuário não pode ter duas linhas no mesmo time. O índice solto em UserId cobre a
            // consulta "times de um usuário" usada por TeamRepository.GetForUserAsync.
            modelBuilder.Entity<TeamMember>()
                .HasIndex(m => new { m.TeamId, m.UserId })
                .IsUnique();

            modelBuilder.Entity<TeamMember>()
                .HasIndex(m => m.UserId);

            // Convite direto de time: uma linha por (TeamId, InviteeId), reaproveitada em Recusado —
            // mesmo padrão do índice único de Friendship. O segundo índice cobre os pedidos pendentes
            // recebidos por usuário (equivalente ao (AddresseeId, Status) de Friendship).
            modelBuilder.Entity<TeamInvite>()
                .HasIndex(i => new { i.TeamId, i.InviteeId })
                .IsUnique();

            modelBuilder.Entity<TeamInvite>()
                .HasIndex(i => new { i.InviteeId, i.Status });

            // Categoria de desafio: sem FK para nada, mesma convenção do projeto. Nome curto de
            // tela e ícone (nome do componente lucide-react) não precisam de nvarchar(max).
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

            // Cobre a listagem por categoria (admin) e os desafios disponíveis de um time a partir
            // das categorias ativas dele.
            modelBuilder.Entity<Challenge>()
                .HasIndex(c => c.CategoryId);

            // Um time não pode ativar a mesma categoria duas vezes — a existência da linha É a
            // ativação. Mesmo padrão do índice único de TeamMember (TeamId, UserId).
            modelBuilder.Entity<TeamActiveCategory>()
                .HasIndex(a => new { a.TeamId, a.CategoryId })
                .IsUnique();

            // URL do blob — nunca filtrada, só exibida, mesmo critério do BannerImageUrl de Team.
            modelBuilder.Entity<ChallengeSubmission>()
                .Property(s => s.PhotoUrl)
                .HasMaxLength(500);

            // Cobre a checagem de duplicidade ao enviar (usuário+desafio+time) e a listagem de
            // desafios disponíveis (usuário+time, todas as submissões pra achar a mais recente).
            modelBuilder.Entity<ChallengeSubmission>()
                .HasIndex(s => new { s.UserId, s.ChallengeId, s.TeamId });

            // Cobre a fila de aprovação do dono (time+status=Pendente).
            modelBuilder.Entity<ChallengeSubmission>()
                .HasIndex(s => new { s.TeamId, s.Status });

            // Cobre a fila de aprovação do dono do torneio (Fase 5b: torneio+status=Pendente),
            // separada da fila do dono do time acima.
            modelBuilder.Entity<ChallengeSubmission>()
                .HasIndex(s => new { s.TournamentId, s.Status });

            // Torneio: sem FK pra Users, mesma convenção de Team.
            modelBuilder.Entity<Tournament>()
                .Property(t => t.Name)
                .HasMaxLength(100);

            modelBuilder.Entity<Tournament>()
                .Property(t => t.Description)
                .HasMaxLength(500);

            // Token do link de convite do torneio: mesmo padrão de Team.InviteToken.
            modelBuilder.Entity<Tournament>()
                .Property(t => t.InviteToken)
                .HasMaxLength(32);

            modelBuilder.Entity<Tournament>()
                .HasIndex(t => t.InviteToken)
                .IsUnique()
                .HasFilter("[InviteToken] IS NOT NULL");

            // URL do blob — nunca filtrada, só exibida, mesmo critério do BannerImageUrl de Team.
            modelBuilder.Entity<Tournament>()
                .Property(t => t.BannerImageUrl)
                .HasMaxLength(500);

            modelBuilder.Entity<TournamentRequest>()
                .Property(r => r.ProposedName)
                .HasMaxLength(100);

            modelBuilder.Entity<TournamentRequest>()
                .Property(r => r.ProposedDescription)
                .HasMaxLength(500);

            // Cobre a listagem de solicitações pendentes (admin) — tabela pequena, mas custa nada.
            modelBuilder.Entity<TournamentRequest>()
                .HasIndex(r => r.Status);

            // Cobre GetActiveEntriesForTeamAsync — limite de MaxTournamentsPerTeam entradas
            // simultâneas (Fase 5b), checado a cada pedido de entrada novo (qualquer torneio,
            // sem filtro por TournamentId aqui).
            modelBuilder.Entity<TournamentTeam>()
                .HasIndex(t => t.TeamId);

            // Cobre a fila de aprovação do dono do torneio (torneio+status=Pendente) e o ranking
            // (torneio+status=Aprovado).
            modelBuilder.Entity<TournamentTeam>()
                .HasIndex(t => new { t.TournamentId, t.Status });

            // Placar individual (Fase 5a): um placar por (TeamId, UserId) — a mesma pessoa em times
            // diferentes tem linhas separadas. O índice único garante a semântica de upsert do
            // TeamMemberScoreRepository (mesmo padrão do índice único de TeamActiveCategory) e o
            // TeamId à esquerda já cobre a leitura do ranking completo de um time.
            modelBuilder.Entity<TeamMemberScore>()
                .HasIndex(s => new { s.TeamId, s.UserId })
                .IsUnique();

            // Vínculo de um desafio do catálogo geral a um torneio (Fase 5b): não pode repetir o
            // mesmo desafio no mesmo torneio — mesmo padrão do índice único de TeamActiveCategory.
            modelBuilder.Entity<TournamentChallenge>()
                .HasIndex(l => new { l.TournamentId, l.ChallengeId })
                .IsUnique();

            // Desafio próprio de um torneio (Fase 5b): sem categoria, lista flat por torneio.
            modelBuilder.Entity<TournamentOwnChallenge>()
                .Property(c => c.Title)
                .HasMaxLength(200);

            modelBuilder.Entity<TournamentOwnChallenge>()
                .Property(c => c.Description)
                .HasMaxLength(1000);

            // Cobre a listagem de desafios próprios de um torneio.
            modelBuilder.Entity<TournamentOwnChallenge>()
                .HasIndex(c => c.TournamentId);
        }
    }
}