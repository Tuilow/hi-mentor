using Tuilow.Application.Common.Models;
using MediatR;

namespace Tuilow.Application.Contexts.Identity.Commands.GoogleLogin;

public sealed record GoogleLoginCommand(string IdToken) : IRequest<AuthTokens>;
