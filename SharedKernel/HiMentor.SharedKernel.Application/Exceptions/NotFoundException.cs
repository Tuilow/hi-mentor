namespace HiMentor.SharedKernel.Application.Exceptions;

public sealed class NotFoundException(string entityName, object key)
    : Exception($"{entityName} com identificador '{key}' não encontrado.");
