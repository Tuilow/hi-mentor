namespace Tuilow.IdentidadeAcesso.Domain.Exceptions;

public sealed class DuplicateEmailException(string email)
    : Exception($"Já existe um usuário com o e-mail '{email}'.");
