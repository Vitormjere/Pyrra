using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Treinos;

namespace Pyrra.Application.Common.Interfaces {
    public interface IWorkoutTemplateRepository {
        // Todos os templates do catálogo, já com dias e exercícios carregados: a tela lista os
        // cards com preview expansível, então precisa da estrutura inteira numa leitura só.
        Task<IReadOnlyList<WorkoutTemplate>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default);

        // Um template com dias e exercícios — o que "aplicar" precisa para copiar a estrutura.
        Task<WorkoutTemplate?> GetByIdWithDetailsAsync(Guid templateId, CancellationToken cancellationToken = default);
    }
}
