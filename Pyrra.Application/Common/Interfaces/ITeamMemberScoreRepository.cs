using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Desafios;

namespace Pyrra.Application.Common.Interfaces {
    public interface ITeamMemberScoreRepository {
        // A linha de placar de um usuário nesse time, se já existir — nulo antes da primeira
        // submissão aprovada dele nesse time (criada sob demanda em ApproveSubmissionAsync).
        Task<TeamMemberScore?> GetAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default);

        // Todos os placares desse time — base do ranking individual (GetTeamRankingAsync monta a
        // lista completa de membros e usa isso pra saber quem já tem pontos e quanto).
        Task<IReadOnlyList<TeamMemberScore>> GetByTeamAsync(Guid teamId, CancellationToken cancellationToken = default);

        Task AddAsync(TeamMemberScore score, CancellationToken cancellationToken = default);
        Task UpdateAsync(TeamMemberScore score, CancellationToken cancellationToken = default);
    }
}
