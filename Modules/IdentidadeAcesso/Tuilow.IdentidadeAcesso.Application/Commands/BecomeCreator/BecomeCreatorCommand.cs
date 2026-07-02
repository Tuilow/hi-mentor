using MediatR;

namespace Tuilow.IdentidadeAcesso.Application.Commands.BecomeCreator;

/// <summary>
/// Auto-promoção a Creator: qualquer usuário autenticado pode se tornar um criador de
/// conteúdo por conta própria, sem depender de aprovação administrativa — condizente com o
/// modelo de plataforma aberta do Tuilow ("qualquer pessoa ou empresa pode criar canal e
/// publicar cursos, sem pagamento e sem aprovação"). Diferente de PromoteUserCommand (que
/// continua existindo, restrito a Admin, para promover QUALQUER usuário a QUALQUER role).
/// Multi-role: não remove o role Student existente.
/// </summary>
public sealed record BecomeCreatorCommand(Guid UserId) : IRequest;
