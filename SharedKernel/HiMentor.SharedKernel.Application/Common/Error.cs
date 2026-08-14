namespace HiMentor.SharedKernel.Application.Common;

/// <summary>Novo componente do SharedKernel: erro padronizado usado pelo Result pattern.</summary>
public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
}
