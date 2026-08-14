namespace HiMentor.IdentidadeAcesso.Domain.Exceptions;

public sealed class DuplicateEmailException(string email)
    : Exception($"Já existe um usuário com o e-mail '{email}'.");
