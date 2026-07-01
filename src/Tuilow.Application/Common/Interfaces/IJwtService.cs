using Tuilow.Application.Common.Models;
using Tuilow.Domain.Contexts.Identity.Entities;

namespace Tuilow.Application.Common.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    Guid? GetUserIdFromToken(string token);
}
