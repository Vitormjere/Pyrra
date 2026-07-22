using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Users;
using Pyrra.Infrastructure.Data;

namespace Pyrra.Infrastructure.Repositories {
    public class UserRepository : IUserRepository {
        private readonly PyrraDbContext _context;

        public UserRepository(PyrraDbContext context) {
            _context = context;
        }

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
            _context.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        public async Task AddAsync(User user, CancellationToken cancellationToken = default) {
            await _context.Users.AddAsync(user, cancellationToken);
            try {
                await _context.SaveChangesAsync(cancellationToken);
            } catch (DbUpdateException ex) when (IsEmailUniqueViolation(ex)) {
                // Protege contra corrida: entre a checagem prévia de e-mail e este insert, outra
                // requisição pode ter cadastrado o mesmo e-mail. O índice único IX_Users_Email
                // barra a duplicata no banco; traduzimos aqui para uma exceção de domínio, sem
                // deixar o detalhe do EF Core vazar para a camada Application.
                throw new EmailAlreadyRegisteredException(user.Email);
            }
        }

        public async Task UpdateAsync(User user, CancellationToken cancellationToken = default) {
            _context.Users.Update(user);
            await _context.SaveChangesAsync(cancellationToken);
        }

        private static bool IsEmailUniqueViolation(DbUpdateException ex) {
            // A mensagem do SQL Server para chave duplicada inclui o nome do índice violado
            // (ex.: "...with unique index 'IX_Users_Email'..."), definido na migration
            // AddUniqueIndexToUserEmail.
            var message = ex.InnerException?.Message ?? ex.Message;
            return message.Contains("IX_Users_Email", StringComparison.OrdinalIgnoreCase);
        }
    }
}
