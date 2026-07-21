using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Focos;
using Pyrra.Infrastructure.Data;

namespace Pyrra.Infrastructure.Repositories {
    public class FreezeBankRepository : IFreezeBankRepository {
        private readonly PyrraDbContext _context;

        public FreezeBankRepository(PyrraDbContext context) {
            _context = context;
        }

        public Task<FreezeBank?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            _context.FreezeBanks.FirstOrDefaultAsync(b => b.UserId == userId, cancellationToken);

        public async Task<FreezeBank> UpsertAsync(FreezeBank bank, CancellationToken cancellationToken = default) {
            var existing = await GetByUserIdAsync(bank.UserId, cancellationToken);

            if (existing is null) {
                if (bank.Id == Guid.Empty) {
                    bank.Id = Guid.NewGuid();
                }
                await _context.FreezeBanks.AddAsync(bank, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                return bank;
            }

            existing.FreezesAvailable     = bank.FreezesAvailable;
            existing.LastGrantedWeekStart = bank.LastGrantedWeekStart;

            await _context.SaveChangesAsync(cancellationToken);
            return existing;
        }
    }
}
