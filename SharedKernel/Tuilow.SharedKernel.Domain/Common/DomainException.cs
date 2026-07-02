namespace Tuilow.SharedKernel.Domain.Common;

/// <summary>
/// Exceção base para violações de regra de negócio no domínio de qualquer bounded context.
/// Novo componente do SharedKernel — módulos deveriam derivar suas exceções de domínio daqui
/// em vez de usar InvalidOperationException/Exception genérica diretamente.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}
