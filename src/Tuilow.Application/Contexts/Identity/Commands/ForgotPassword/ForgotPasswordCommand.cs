using MediatR;

namespace Tuilow.Application.Contexts.Identity.Commands.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email) : IRequest;
