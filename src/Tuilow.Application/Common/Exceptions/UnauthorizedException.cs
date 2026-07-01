namespace Tuilow.Application.Common.Exceptions;

public sealed class UnauthorizedException(string message = "Não autorizado.")
    : Exception(message);
