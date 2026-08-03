using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Comunidade;
using Pyrra.Infrastructure.Data;

namespace Pyrra.Infrastructure.Repositories {
    public class TeamRepository : ITeamRepository {
        private readonly PyrraDbContext _context;

        public TeamRepository(PyrraDbContext context) {
            _context = context;
        }

        public Task<Team?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            _context.Teams.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        public Task<Team?> GetByInviteTokenAsync(string inviteToken, CancellationToken cancellationToken = default) =>
            _context.Teams.FirstOrDefaultAsync(t => t.InviteToken == inviteToken, cancellationToken);

        // Times onde o usuário é dono OU tem uma linha em TeamMember — sem navigation property
        // (convenção do projeto), então o vínculo com TeamMembers é feito via Any() dentro do Where.
        public async Task<IReadOnlyList<Team>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
            await _context.Teams
                .Where(t => t.OwnerId == userId
                    || _context.TeamMembers.Any(m => m.TeamId == t.Id && m.UserId == userId))
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<Team>> GetPublicAsync(CancellationToken cancellationToken = default) =>
            await _context.Teams
                .Where(t => t.Visibility == TeamVisibility.Publico)
                .OrderBy(t => t.Name)
                .ToListAsync(cancellationToken);

        // Sem filtro de visibilidade ou dono — só a listagem administrativa usa isso.
        public async Task<IReadOnlyList<Team>> GetAllAsync(CancellationToken cancellationToken = default) =>
            await _context.Teams
                .OrderBy(t => t.Name)
                .ToListAsync(cancellationToken);

        public async Task AddAsync(Team team, CancellationToken cancellationToken = default) {
            await _context.Teams.AddAsync(team, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(Team team, CancellationToken cancellationToken = default) {
            _context.Teams.Update(team);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Team team, CancellationToken cancellationToken = default) {
            _context.Teams.Remove(team);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
