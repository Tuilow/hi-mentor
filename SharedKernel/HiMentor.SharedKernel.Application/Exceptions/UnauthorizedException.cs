namespace HiMentor.SharedKernel.Application.Exceptions;

public sealed class UnauthorizedException(string message = "Não autorizado.") : Exception(message);
