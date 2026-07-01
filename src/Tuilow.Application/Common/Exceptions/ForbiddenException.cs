namespace Tuilow.Application.Common.Exceptions;

public sealed class ForbiddenException(string message = "Acesso negado.")
    : Exception(message);
