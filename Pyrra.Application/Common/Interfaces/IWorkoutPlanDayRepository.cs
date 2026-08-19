using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Common;
using Pyrra.Domain.Treinos;

namespace Pyrra.Application.Common.Interfaces {
    public interface IWorkoutPlanDayRepository {
        // só os dias que já foram registrados no banco
        Task<IReadOnlyList<WorkoutPlanDay>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);

        // salva o plano completo, criando ou atualizando cada dia. Retorna as entidades persistidas
        // (mesma ordem da entrada) com o Id real de cada uma — quem grava exercícios em seguida
        // precisa desse Id pra montar o WorkoutPlanDayId (FK).
        Task<IReadOnlyList<WorkoutPlanDay>> UpsertManyAsync(Guid userId, IReadOnlyList<WorkoutPlanDay> days, CancellationToken cancellationToken = default);

        // garante que o dia exista no banco (cria com Label=null se ainda não existir) e devolve seu
        // Id real — usado antes de gravar um exercício avulso, que precisa de um WorkoutPlanDayId
        // válido pra referenciar
        Task<WorkoutPlanDay> GetOrCreateAsync(Guid userId, WeekDay dayOfWeek, CancellationToken cancellationToken = default);

        // Troca de lugar dois dias da semana: atualiza só o DayOfWeek de cada linha (Id e Label
        // preservados), então os exercícios ligados por WorkoutPlanDayId acompanham automaticamente.
        // Se um dos dois não tiver linha no banco (nunca teve label/exercício), o outro simplesmente
        // muda de DayOfWeek sozinho — o dia de origem "esvazia" por ausência de linha, como sempre
        // funcionou. Retorna as linhas afetadas (0, 1 ou 2), já com o DayOfWeek novo.
        Task<IReadOnlyList<WorkoutPlanDay>> SwapDaysAsync(Guid userId, WeekDay dayA, WeekDay dayB, CancellationToken cancellationToken = default);
    }
}
