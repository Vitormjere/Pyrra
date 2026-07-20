using System;
using System.Collections.Generic;
using Pyrra.Domain.Focos;

namespace Pyrra.Application.Focos {
    // Status de um foco ativo no dia consultado. Foco sem FocusLog naquela data conta como não concluído.
    public record FocusStatus(Guid FocusId, string Name, int Weight, bool Completed);

    // Agregados do dia + o detalhamento foco a foco que os originou.
    public record DailyScoreResult(DailyScore Score, IReadOnlyList<FocusStatus> Focuses);
}
