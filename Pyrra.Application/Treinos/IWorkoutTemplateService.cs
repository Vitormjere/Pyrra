using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Treinos;

namespace Pyrra.Application.Treinos {
    public interface IWorkoutTemplateService {
        // O catálogo inteiro, com dias e exercícios, para a tela montar os cards com preview.
        Task<IReadOnlyList<WorkoutTemplate>> GetAllAsync(CancellationToken cancellationToken = default);

        // Copia a estrutura do template para o Plano da Semana do usuário, SOBRESCREVENDO o que
        // houver: todos os labels dos 7 dias passam a ser os do template (inclusive "Descanso") e
        // a lista de exercícios é trocada por inteiro. A cópia não guarda vínculo com o template,
        // então tudo continua editável/removível como no fluxo manual.
        //
        // Template "Personalizado" (IsCustom) é no-op: não há estrutura a aplicar, o front só abre
        // o editor manual. Template inexistente lança NotFoundException.
        Task ApplyAsync(Guid userId, Guid templateId, CancellationToken cancellationToken = default);
    }
}
