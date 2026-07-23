using Tuilow.IdentidadeAcesso.Domain.Enums;
using Tuilow.SharedKernel.Application.Common;
using MediatR;

namespace Tuilow.IdentidadeAcesso.Application.Queries.ListUsers;

/// <summary>Listagem paginada de usuários para o painel do dono da plataforma.</summary>
public sealed record ListUsersQuery(
    string? Search = null,
    string? RoleFilter = null,
    UserStatus? StatusFilter = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedList<UserSummaryResponse>>;
