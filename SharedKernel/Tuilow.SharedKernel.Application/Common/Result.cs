namespace Tuilow.SharedKernel.Application.Common;

/// <summary>
/// Novo componente do SharedKernel: resultado padrão de operações de aplicação para fluxos de
/// falha esperados (validação de negócio) sem precisar lançar exceção. Os handlers atuais usam
/// exceções (BusinessException, NotFoundException etc.) — este Result é um padrão complementar,
/// disponível para módulos novos que preferirem não usar exceção para controle de fluxo.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("Result de sucesso não pode carregar um Error.");
        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("Result de falha precisa de um Error.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
    public static Result<T> Success<T>(T value) => new(value, true, Error.None);
    public static Result<T> Failure<T>(Error error) => new(default, false, error);
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Não é possível acessar Value de um Result com falha.");

    internal Result(T? value, bool isSuccess, Error error) : base(isSuccess, error)
    {
        _value = value;
    }
}
