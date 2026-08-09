using System;
using System.Linq;
using System.Threading.Tasks;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Tests.Comunidade;
using Pyrra.Application.Zelo;
using Pyrra.Domain.Users;
using Pyrra.Domain.Zelo;
using Xunit;

namespace Pyrra.Application.Tests.Zelo {
    public class ZeloPlanServiceTests {
        private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid OtherUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        private static (ZeloPlanService service, FakeZeloPlanSessionRepository sessions,
            FakeZeloPlanAnswerRepository answers, FakeZeloPlanQueryLogRepository logs,
            FakeZeloPlanAssistant assistant, FakeClock clock)
            Build() {
            var sessions  = new FakeZeloPlanSessionRepository();
            var answers   = new FakeZeloPlanAnswerRepository();
            var logs      = new FakeZeloPlanQueryLogRepository();
            var context   = new FakeZeloContextBuilder();
            var assistant = new FakeZeloPlanAssistant();
            var users     = new FakeUserRepository(new User { Id = UserId, Name = "User", Email = "user@x.com", Timezone = "America/Sao_Paulo" });
            var clock     = new FakeClock();

            var service = new ZeloPlanService(sessions, answers, logs, context, assistant, users, clock);
            return (service, sessions, answers, logs, assistant, clock);
        }

        // responde as N primeiras perguntas com respostas válidas (primeira opção de cada uma), devolve o estado final
        private static async Task<ZeloPlanSessionState> AnswerAllAsync(
            ZeloPlanService service, Guid sessionId, params string[] answersInOrder) {
            ZeloPlanSessionState? last = null;
            foreach (var answer in answersInOrder) {
                last = await service.AnswerAsync(UserId, sessionId, answer);
            }
            return last!;
        }

        [Fact]
        public async Task StartOrResumeAsync_SemSessaoAtiva_CriaNovaComPrimeiraPergunta() {
            var (service, _, _, _, _, _) = Build();

            var state = await service.StartOrResumeAsync(UserId);

            Assert.Equal(ZeloPlanSessionStatus.Coletando, state.Status);
            Assert.NotNull(state.NextQuestion);
            Assert.Equal(ZeloPlanQuestionFlow.KeyObjetivo, state.NextQuestion!.Key);
            Assert.Equal(0, state.AnsweredCount);
        }

        [Fact]
        public async Task StartOrResumeAsync_ComSessaoColetandoAtiva_Retoma() {
            var (service, _, _, _, _, _) = Build();
            var first = await service.StartOrResumeAsync(UserId);

            var resumed = await service.StartOrResumeAsync(UserId);

            Assert.Equal(first.SessionId, resumed.SessionId);
        }

        [Fact]
        public async Task StartOrResumeAsync_SessaoExpirada_CriaNova() {
            var (service, sessions, _, _, _, clock) = Build();
            var first = await service.StartOrResumeAsync(UserId);

            clock.UtcNow = clock.UtcNow.AddHours(25);

            var resumed = await service.StartOrResumeAsync(UserId);

            Assert.NotEqual(first.SessionId, resumed.SessionId);
        }

        [Fact]
        public async Task AnswerAsync_OpcaoInvalida_Lanca() {
            var (service, _, _, _, _, _) = Build();
            var start = await service.StartOrResumeAsync(UserId);

            await Assert.ThrowsAsync<InvalidZeloPlanException>(
                () => service.AnswerAsync(UserId, start.SessionId, "opção que não existe"));
        }

        [Fact]
        public async Task AnswerAsync_RespostaVazia_Lanca() {
            var (service, _, _, _, _, _) = Build();
            var start = await service.StartOrResumeAsync(UserId);

            await Assert.ThrowsAsync<InvalidZeloPlanException>(
                () => service.AnswerAsync(UserId, start.SessionId, "   "));
        }

        [Fact]
        public async Task AnswerAsync_UsuarioNaoDono_LancaNotFound() {
            var (service, _, _, _, _, _) = Build();
            var start = await service.StartOrResumeAsync(UserId);

            await Assert.ThrowsAsync<NotFoundException>(
                () => service.AnswerAsync(OtherUserId, start.SessionId, "Emagrecimento"));
        }

        [Fact]
        public async Task AnswerAsync_ObjetivoEmagrecimento_DisparaPerguntasDinamicasEsperadas() {
            var (service, _, _, _, _, _) = Build();
            var start = await service.StartOrResumeAsync(UserId);

            // objetivo -> restricoes -> equipamento(Nenhum) -> dias
            var afterObjetivo = await service.AnswerAsync(UserId, start.SessionId, "Emagrecimento");
            Assert.Equal(ZeloPlanQuestionFlow.KeyRestricoes, afterObjetivo.NextQuestion!.Key);

            var afterRestricoes = await service.AnswerAsync(UserId, start.SessionId, "Nenhuma");
            Assert.Equal(ZeloPlanQuestionFlow.KeyEquipamento, afterRestricoes.NextQuestion!.Key);

            var afterEquipamento = await service.AnswerAsync(UserId, start.SessionId, "Nenhum (casa)");
            Assert.Equal(ZeloPlanQuestionFlow.KeyDias, afterEquipamento.NextQuestion!.Key);

            var afterDias = await service.AnswerAsync(UserId, start.SessionId, "2-3 dias");
            // Emagrecimento -> cardio/musculação; Equipamento Nenhum -> espaço em casa; Emagrecimento -> refeições/dia
            Assert.Equal(ZeloPlanQuestionFlow.KeyCardioMusculacao, afterDias.NextQuestion!.Key);

            var afterCardio = await service.AnswerAsync(UserId, start.SessionId, "Equilibrado");
            Assert.Equal(ZeloPlanQuestionFlow.KeyEspacoCasa, afterCardio.NextQuestion!.Key);

            var afterEspaco = await service.AnswerAsync(UserId, start.SessionId, "Sim");
            Assert.Equal(ZeloPlanQuestionFlow.KeyRefeicoesDia, afterEspaco.NextQuestion!.Key);
        }

        [Fact]
        public async Task AnswerAsync_ObjetivoCondicionamento_NaoDisparaPerguntasDinamicas() {
            var (service, _, _, _, assistant, _) = Build();
            var start = await service.StartOrResumeAsync(UserId);

            var final = await AnswerAllAsync(service, start.SessionId,
                "Condicionamento físico", "Nenhuma", "Academia completa", "4-5 dias");

            // nenhum dos 4 gatilhos dinâmicos se aplica: objetivo != Emagrecimento/GanhoDeMassa, equipamento != Nenhum
            Assert.Equal(ZeloPlanSessionStatus.PlanoGerado, final.Status);
            Assert.Equal(1, assistant.CallCount);
        }

        [Fact]
        public async Task AnswerAsync_FormularioCompleto_GeraPlanoEConsomeCota() {
            var (service, sessions, _, logs, assistant, clock) = Build();
            var start = await service.StartOrResumeAsync(UserId);

            var final = await AnswerAllAsync(service, start.SessionId,
                "Condicionamento físico", "Nenhuma", "Academia completa", "4-5 dias");

            Assert.Equal(ZeloPlanSessionStatus.PlanoGerado, final.Status);
            Assert.Null(final.NextQuestion);
            Assert.Null(final.Error);

            var session = sessions.Sessions.Single(s => s.Id == start.SessionId);
            Assert.NotNull(session.GeneratedPlanJson);

            var today = clock.TodayIn("America/Sao_Paulo");
            var log = logs.Logs.Single(l => l.UserId == UserId && l.Date == today);
            Assert.Equal(1, log.Count);
        }

        [Fact]
        public async Task AnswerAsync_GeracaoFalha_MantemColetandoSemConsumirCota() {
            var (service, sessions, _, logs, assistant, _) = Build();
            assistant.NextResult = new ZeloPlanGenerationResult(false, null, "O Zelo não conseguiu montar seu plano agora.");
            var start = await service.StartOrResumeAsync(UserId);

            var final = await AnswerAllAsync(service, start.SessionId,
                "Condicionamento físico", "Nenhuma", "Academia completa", "4-5 dias");

            Assert.Equal(ZeloPlanSessionStatus.Coletando, final.Status);
            Assert.Null(final.NextQuestion);
            Assert.NotNull(final.Error);
            Assert.Empty(logs.Logs);

            var session = sessions.Sessions.Single(s => s.Id == start.SessionId);
            Assert.Null(session.GeneratedPlanJson);
        }

        [Fact]
        public async Task RetryGenerationAsync_ApósFalha_ConcluiComSucesso() {
            var (service, _, _, logs, assistant, _) = Build();
            assistant.NextResult = new ZeloPlanGenerationResult(false, null, "falhou");
            var start = await service.StartOrResumeAsync(UserId);
            await AnswerAllAsync(service, start.SessionId, "Condicionamento físico", "Nenhuma", "Academia completa", "4-5 dias");

            assistant.NextResult = null; // próxima chamada usa o plano válido padrão
            var retried = await service.RetryGenerationAsync(UserId, start.SessionId);

            Assert.Equal(ZeloPlanSessionStatus.PlanoGerado, retried.Status);
            Assert.Equal(2, assistant.CallCount);
            Assert.Single(logs.Logs);
        }

        [Fact]
        public async Task AnswerAsync_CotaDiariaEstourada_Lanca() {
            var (service, _, _, logs, _, clock) = Build();
            var today = clock.TodayIn("America/Sao_Paulo");
            logs.Logs.Add(new ZeloPlanQueryLog { Id = Guid.NewGuid(), UserId = UserId, Date = today, Count = 20 });

            var start = await service.StartOrResumeAsync(UserId);

            await Assert.ThrowsAsync<ZeloPlanRateLimitExceededException>(() =>
                AnswerAllAsync(service, start.SessionId, "Condicionamento físico", "Nenhuma", "Academia completa", "4-5 dias"));
        }

        [Fact]
        public async Task GetPreviewAsync_PlanoGerado_DevolvePlanoDeserializado() {
            var (service, _, _, _, _, _) = Build();
            var start = await service.StartOrResumeAsync(UserId);
            await AnswerAllAsync(service, start.SessionId, "Condicionamento físico", "Nenhuma", "Academia completa", "4-5 dias");

            var preview = await service.GetPreviewAsync(UserId, start.SessionId);

            Assert.Equal(7, preview.Plan.WorkoutDays.Count);
            Assert.Equal(7, preview.Plan.NutritionDays.Count);
        }

        [Fact]
        public async Task GetPreviewAsync_AindaColetando_Lanca() {
            var (service, _, _, _, _, _) = Build();
            var start = await service.StartOrResumeAsync(UserId);

            await Assert.ThrowsAsync<InvalidZeloPlanException>(() => service.GetPreviewAsync(UserId, start.SessionId));
        }
    }
}
