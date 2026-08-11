using System.Threading;
using System.Threading.Tasks;

namespace Pyrra.Application.Desafios {
    public interface IDailyChallengeGeneratorService {
        // sorteia os 3 desafios do dia pros times que ainda não têm sorteio pra hoje; devolve
        // quantos times foram processados
        Task<int> GenerateMissingForTodayAsync(CancellationToken cancellationToken = default);
    }
}
