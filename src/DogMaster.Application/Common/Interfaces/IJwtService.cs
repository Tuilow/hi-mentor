using DogMaster.Application.Common.Models;
using DogMaster.Domain.Contexts.Identity.Entities;

namespace DogMaster.Application.Common.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    Guid? GetUserIdFromToken(string token);
}
