using System.Threading;
using System.Threading.Tasks;

namespace Pyrra.Application.Zelo {
    // interface para comunicação com o modelo de linguagem
    public interface IZeloAssistant {
        Task<ZeloAssistantResult> AskAsync(string question, string context, CancellationToken cancellationToken = default);
    }
}
