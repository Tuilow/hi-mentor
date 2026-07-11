using System.Reflection;

namespace Tuilow.SharedKernel.Domain.Common;

/// <summary>
/// Base para "smart enums" (Enumeration pattern) — para quando um valor fixo do domínio
/// precisa carregar comportamento/metadados além do nome (algo que um enum comum não permite).
/// Novo componente do SharedKernel — nenhum módulo usava esse padrão ainda.
/// </summary>
public abstract class Enumeration : IComparable
{
    public string Name { get; }
    public int Id { get; }

    protected Enumeration(int id, string name)
    {
        Id = id;
        Name = name;
    }

    public override string ToString() => Name;

    public static IEnumerable<T> GetAll<T>() where T : Enumeration =>
        typeof(T)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Select(f => f.GetValue(null))
            .Cast<T>();

    public override bool Equals(object? obj)
    {
        if (obj is not Enumeration other) return false;
        return GetType() == other.GetType() && Id == other.Id;
    }

    public override int GetHashCode() => Id.GetHashCode();

    public int CompareTo(object? obj) => Id.CompareTo((obj as Enumeration)?.Id ?? 0);
}
