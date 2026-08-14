using MediatR;

namespace HiMentor.IdentidadeAcesso.Application.Commands.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email) : IRequest;
