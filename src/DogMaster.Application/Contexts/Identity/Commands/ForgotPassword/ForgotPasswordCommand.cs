using MediatR;

namespace DogMaster.Application.Contexts.Identity.Commands.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email) : IRequest;
