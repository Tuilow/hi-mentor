using MediatR;

namespace Tuilow.IdentidadeAcesso.Application.Commands.ConfirmEmail;

public sealed record ConfirmEmailCommand(Guid UserId, string Token) : IRequest<bool>;
