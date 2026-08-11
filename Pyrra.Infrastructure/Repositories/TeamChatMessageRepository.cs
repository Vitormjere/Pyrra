using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Chat;
using Pyrra.Infrastructure.Data;

namespace Pyrra.Infrastructure.Repositories {
    public class TeamChatMessageRepository : ITeamChatMessageRepository {
        private readonly PyrraDbContext _context;

        public TeamChatMessageRepository(PyrraDbContext context) {
            _context = context;
        }

        public async Task<IReadOnlyList<TeamChatMessage>> GetByTeamAsync(Guid teamId, CancellationToken cancellationToken = default) =>
            await _context.TeamChatMessages
                .Where(m => m.TeamId == teamId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync(cancellationToken);

        public async Task AddAsync(TeamChatMessage message, CancellationToken cancellationToken = default) {
            await _context.TeamChatMessages.AddAsync(message, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
