using System;
using System.Collections.Generic;
using Pyrra.Domain.Focos;

namespace Pyrra.Application.Focos {
    // status do foco ativo no dia consultado
    public record FocusStatus(Guid FocusId, string Name, int Weight, bool Completed);

    // retorna os totais do dia junto com o detalhamento dos focos
    public record DailyScoreResult(DailyScore Score, IReadOnlyList<FocusStatus> Focuses);
}
