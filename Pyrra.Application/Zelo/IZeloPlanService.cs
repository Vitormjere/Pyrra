using System;
using System.Threading;
using System.Threading.Tasks;

namespace Pyrra.Application.Zelo {
    // orquestra o formulário guiado do Zelo conversacional: iniciar/retomar sessão, responder perguntas, gerar e pré-visualizar o plano
    public interface IZeloPlanService {
        // retoma a sessão ativa (Coletando ou PlanoGerado, não expirada) ou cria uma nova
        Task<ZeloPlanSessionState> StartOrResumeAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<ZeloPlanSessionState> AnswerAsync(Guid userId, Guid sessionId, string answer, CancellationToken cancellationToken = default);

        // tenta gerar de novo depois de uma falha, sem reabrir perguntas já respondidas
        Task<ZeloPlanSessionState> RetryGenerationAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default);

        Task<ZeloPlanPreview> GetPreviewAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default);
    }
}
