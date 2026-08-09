using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Zelo;

namespace Pyrra.Application.Zelo {
    // interface para geração estruturada de plano e continuação de chat com o modelo de linguagem — separada de IZeloAssistant porque a tarefa (JSON estruturado, multi-turno) é bem diferente da pergunta livre de 2-4 frases do Zelo geral
    public interface IZeloPlanAssistant {
        Task<ZeloPlanGenerationResult> GeneratePlanAsync(
            string userContext, IReadOnlyList<ZeloPlanAnswer> answers, CancellationToken cancellationToken = default);
    }
}
