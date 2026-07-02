using MediatR;

namespace Tuilow.IdentidadeAcesso.Application.Commands.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email) : IRequest;
