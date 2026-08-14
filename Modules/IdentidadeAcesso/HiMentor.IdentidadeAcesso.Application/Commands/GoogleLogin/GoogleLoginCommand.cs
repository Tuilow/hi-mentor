using HiMentor.IdentidadeAcesso.Application.Common;
using MediatR;

namespace HiMentor.IdentidadeAcesso.Application.Commands.GoogleLogin;

public sealed record GoogleLoginCommand(string IdToken) : IRequest<AuthTokens>;
