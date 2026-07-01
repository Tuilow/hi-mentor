namespace Tuilow.Application.Common.Models;

public sealed record AuthTokens(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpires,
    DateTime RefreshTokenExpires
);
