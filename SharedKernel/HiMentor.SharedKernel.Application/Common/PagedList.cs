namespace HiMentor.SharedKernel.Application.Common;

/// <summary>Reaproveitado de HiMentor.Application.Common.Models.PagedList — movido para o SharedKernel.</summary>
public sealed record PagedList<T>(
    IEnumerable<T> Items,
    int Total,
    int Page,
    int PageSize
)
{
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
