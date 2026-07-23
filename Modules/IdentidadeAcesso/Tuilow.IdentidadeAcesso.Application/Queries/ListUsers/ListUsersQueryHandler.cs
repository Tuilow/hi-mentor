using Tuilow.IdentidadeAcesso.Domain.Interfaces;
using Tuilow.SharedKernel.Application.Common;
using MediatR;

namespace Tuilow.IdentidadeAcesso.Application.Queries.ListUsers;

public sealed class ListUsersQueryHandler(IUserRepository userRepository)
    : IRequestHandler<ListUsersQuery, PagedList<UserSummaryResponse>>
{
    public async Task<PagedList<UserSummaryResponse>> Handle(ListUsersQuery request, CancellationToken ct)
    {
        var (users, total) = await userRepository.ListAllAsync(
            request.Search, request.RoleFilter, request.StatusFilter, request.Page, request.PageSize, ct);

        // Profile é ?-acessado de propósito: o relacionamento é modelado como obrigatório no
        // domínio (User.Profile nunca é null!), mas essa listagem cobre TODOS os usuários do
        // banco, incluindo contas antigas/seed que podem ter sido criadas antes de existir
        // Profile — sem essa proteção, uma única conta órfã derrubava o painel inteiro com
        // NullReferenceException em vez de simplesmente mostrar "" para o nome dela.
        var items = users.Select(u => new UserSummaryResponse(
            u.Id,
            u.Email.Value,
            u.Profile?.FirstName ?? "",
            u.Profile?.LastName ?? "",
            u.Status.ToString(),
            u.Roles.Select(r => r.Name).ToList(),
            u.CreatedAt,
            u.RefreshTokens.Select(t => (DateTime?)t.CreatedAt).Max()
        ));

        return new PagedList<UserSummaryResponse>(items, total, request.Page, request.PageSize);
    }
}
