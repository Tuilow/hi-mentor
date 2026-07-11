using Tuilow.IdentidadeAcesso.Application.Common;
using MediatR;

namespace Tuilow.IdentidadeAcesso.Application.Commands.BecomeCreator;

/// <summary>
/// Auto-promoção a Creator: qualquer usuário autenticado pode se tornar um criador de
/// conteúdo por conta própria, sem depender de aprovação administrativa — condizente com o
/// modelo de plataforma aberta do Tuilow ("qualquer pessoa ou empresa pode criar canal e
/// publicar cursos, sem pagamento e sem aprovação"). Diferente de PromoteUserCommand (que
/// continua existindo, restrito a Admin, para promover QUALQUER usuário a QUALQUER role).
/// Multi-role: não remove o role Student existente.
///
/// Retorna AuthTokens (como Login/Register/RefreshToken) com um access token já contendo o
/// claim de role "Creator" — evita depender de uma chamada separada a /auth/refresh-token
/// logo em seguida, que competia pelo refresh token de uso único e causava erro intermitente
/// nesta ação (o handler tinha rodado com sucesso, mas o refresh subsequente podia falhar
/// por race condition — daí o bug reportado de "dá erro mas funciona no F5").
/// </summary>
public sealed record BecomeCreatorCommand(Guid UserId) : IRequest<AuthTokens>;
