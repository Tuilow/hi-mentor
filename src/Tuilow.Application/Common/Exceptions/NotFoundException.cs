namespace Tuilow.Application.Common.Exceptions;

public sealed class NotFoundException(string entityName, object key)
    : Exception($"{entityName} com identificador '{key}' não encontrado.");
