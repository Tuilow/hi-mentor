using Tuilow.IdentidadeAcesso.Application.Common;
using MediatR;

namespace Tuilow.IdentidadeAcesso.Application.Commands.GoogleLogin;

public sealed record GoogleLoginCommand(string IdToken) : IRequest<AuthTokens>;
