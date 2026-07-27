using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Users;
using Pyrra.Infrastructure.Data;

namespace Pyrra.Infrastructure.Repositories {
    public class UserRepository : IUserRepository {
        // Teto da busca: o suficiente para achar quem se procura sem devolver a base inteira quando
        // o termo é curto e casa com muita gente.
        private const int SearchLimit = 20;

        private readonly PyrraDbContext _context;

        public UserRepository(PyrraDbContext context) {
            _context = context;
        }

        // Toda leitura filtra DeletedAt == null: uma conta excluída (soft delete) simplesmente não
        // existe para login, busca, amigos ou qualquer serviço que carregue o usuário pelo id do
        // token — sem precisar repetir a checagem em cada um deles.
        private IQueryable<User> ActiveUsers => _context.Users.Where(u => u.DeletedAt == null);

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
            ActiveUsers.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            ActiveUsers.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
            ActiveUsers.FirstOrDefaultAsync(u => u.Username == username, cancellationToken);

        public Task<User?> GetByInviteTokenAsync(string inviteToken, CancellationToken cancellationToken = default) =>
            ActiveUsers.FirstOrDefaultAsync(u => u.InviteToken == inviteToken, cancellationToken);

        public async Task<IReadOnlyList<User>> SearchAsync(string term, Guid excludeUserId, CancellationToken cancellationToken = default) {
            // Normaliza como o username/email são guardados (minúsculas, sem "@"), para o Contains
            // casar independentemente de como o usuário digitou.
            var normalized = term.Trim().TrimStart('@').ToLowerInvariant();
            if (normalized.Length == 0) {
                return Array.Empty<User>();
            }

            return await ActiveUsers
                .Where(u => u.Id != excludeUserId
                    && u.Username != null
                    && (u.Username.Contains(normalized) || u.Email.Contains(normalized)))
                .OrderBy(u => u.Username)
                .Take(SearchLimit)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<User>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) {
            if (ids.Count == 0) {
                return Array.Empty<User>();
            }

            return await ActiveUsers
                .Where(u => ids.Contains(u.Id))
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(User user, CancellationToken cancellationToken = default) {
            await _context.Users.AddAsync(user, cancellationToken);
            try {
                await _context.SaveChangesAsync(cancellationToken);
            } catch (DbUpdateException ex) when (IsUniqueViolation(ex, "IX_Users_Email")) {
                // Protege contra corrida: entre a checagem prévia de e-mail e este insert, outra
                // requisição pode ter cadastrado o mesmo e-mail. O índice único IX_Users_Email
                // barra a duplicata no banco; traduzimos aqui para uma exceção de domínio, sem
                // deixar o detalhe do EF Core vazar para a camada Application.
                throw new EmailAlreadyRegisteredException(user.Email);
            }
        }

        public async Task UpdateAsync(User user, CancellationToken cancellationToken = default) {
            _context.Users.Update(user);
            try {
                await _context.SaveChangesAsync(cancellationToken);
            } catch (DbUpdateException ex) when (IsUniqueViolation(ex, "IX_Users_Username")) {
                // Mesma proteção de corrida do e-mail, agora para o username: o índice único é a
                // última linha de defesa quando dois usuários pegam o mesmo ao mesmo tempo.
                throw new UsernameAlreadyTakenException(user.Username ?? string.Empty);
            } catch (DbUpdateException ex) when (IsUniqueViolation(ex, "IX_Users_Email")) {
                // Mesma proteção de corrida, agora para troca de e-mail (UserAccountService.ChangeEmailAsync).
                throw new EmailAlreadyRegisteredException(user.Email);
            }
        }

        private static bool IsUniqueViolation(DbUpdateException ex, string indexName) {
            // A mensagem do SQL Server para chave duplicada inclui o nome do índice violado
            // (ex.: "...with unique index 'IX_Users_Username'..."), definido nas migrations.
            var message = ex.InnerException?.Message ?? ex.Message;
            return message.Contains(indexName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
