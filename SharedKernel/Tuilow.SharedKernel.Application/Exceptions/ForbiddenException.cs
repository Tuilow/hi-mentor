namespace Tuilow.SharedKernel.Application.Exceptions;

public sealed class ForbiddenException(string message = "Acesso negado.") : Exception(message);
