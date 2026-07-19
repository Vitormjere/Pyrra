using Pyrra.Domain.Users;

namespace Pyrra.Application.Common.Interfaces {
    public interface ITokenService {
        string GenerateToken(User user);
    }
}
