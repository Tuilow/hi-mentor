using MediatR;

namespace Tuilow.IdentidadeAcesso.Application.Commands.Logout;

/// <summary>
/// Achado C1 da avaliação www/app: hoje não existe forma de invalidar um refresh token no
/// servidor antes dele expirar sozinho (30 dias) — se um token vazar (ex.: XSS antes desta
/// migração para cookie HttpOnly), só resta esperar. Logout agora revoga o token no banco,
/// não só remove o cookie no cliente. RefreshToken nulo/ausente é aceito (idempotente — chamar
/// logout sem sessão ativa não deve dar erro).
/// </summary>
public sealed record LogoutCommand(string? RefreshToken) : IRequest;
