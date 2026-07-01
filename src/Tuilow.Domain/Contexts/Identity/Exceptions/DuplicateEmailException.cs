namespace Tuilow.Domain.Contexts.Identity.Exceptions;

public sealed class DuplicateEmailException(string email)
    : Exception($"Já existe um usuário com o e-mail '{email}'.");
