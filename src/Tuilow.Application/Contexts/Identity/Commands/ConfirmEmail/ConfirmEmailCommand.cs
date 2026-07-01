using MediatR;

namespace Tuilow.Application.Contexts.Identity.Commands.ConfirmEmail;

public sealed record ConfirmEmailCommand(Guid UserId, string Token) : IRequest<bool>;
