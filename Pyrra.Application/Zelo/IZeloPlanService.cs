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

        // sobrescreve o Plano da Semana (Treino) e o plano de Nutrição do usuário com o plano gerado
        Task ApplyAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default);

        // descarta o plano gerado, mantém o que o usuário já tinha
        Task DiscardAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default);

        // histórico do chat livre, disponível depois que o plano foi gerado (PlanoGerado ou Aplicada)
        Task<IReadOnlyList<ZeloPlanChatMessage>> GetMessagesAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default);

        // mesma cota diária da geração do plano (DailyLimit); a mensagem do usuário é salva mesmo se o Zelo não conseguir responder.
        // Quando a sessão já está Aplicada, o Zelo pode responder com uma proposta de edição pontual
        // (ZeloPlanChatMessage.EditProposal) em vez de só texto — ver ConfirmEditAsync/DismissEditAsync.
        Task<ZeloPlanChatResult> SendMessageAsync(Guid userId, Guid sessionId, string message, CancellationToken cancellationToken = default);

        // aplica a edição pontual proposta numa mensagem do Zelo (EditStatus precisa estar Proposta)
        // direto nas tabelas reais de Treino/Nutrição — sem chamar a IA de novo, só usa o que já foi
        // estruturado na proposta
        Task ConfirmEditAsync(Guid userId, Guid sessionId, Guid messageId, CancellationToken cancellationToken = default);

        // descarta a edição proposta sem aplicar nada — o plano continua como estava
        Task DismissEditAsync(Guid userId, Guid sessionId, Guid messageId, CancellationToken cancellationToken = default);
    }
}
