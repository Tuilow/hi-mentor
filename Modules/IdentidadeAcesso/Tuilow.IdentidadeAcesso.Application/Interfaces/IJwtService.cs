using Tuilow.IdentidadeAcesso.Domain.Entities;

namespace Tuilow.IdentidadeAcesso.Application.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    Guid? GetUserIdFromToken(string token);
}
