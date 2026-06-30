using DogMaster.Application.Common.Models;
using MediatR;

namespace DogMaster.Application.Contexts.Identity.Commands.GoogleLogin;

public sealed record GoogleLoginCommand(string IdToken) : IRequest<AuthTokens>;
