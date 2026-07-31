using System;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Common.Interfaces;

namespace Pyrra.Application.Common {
    public class AdminAuthorizationService : IAdminAuthorizationService {
        private readonly IUserRepository _userRepository;

        public AdminAuthorizationService(IUserRepository userRepository) {
            _userRepository = userRepository;
        }

        public async Task EnsureAdminAsync(Guid userId, CancellationToken cancellationToken = default) {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

            // Não diferencia campos inexistentes de campos não marcados
            if (user is null || !user.IsAdmin) {
                throw new ForbiddenException("Esta ação é restrita a administradores.");
            }
        }
    }
}
