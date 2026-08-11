using MediatR;

namespace Tuilow.Learning.Application.Queries.GetAccessCodesAdmin;

/// <summary>Lista todos os códigos de acesso da plataforma — tela Admin "Códigos de acesso".</summary>
public sealed record GetAccessCodesAdminQuery : IRequest<IReadOnlyList<AccessCodeAdminResponse>>;

public sealed record AccessCodeAdminResponse(
    Guid Id,
    string Code,
    Guid CourseId,
    string CourseTitle,
    int? MaxUses,
    int UsesCount,
    DateTime? ExpiresAt,
    bool IsActive,
    DateTime CreatedAt
);
