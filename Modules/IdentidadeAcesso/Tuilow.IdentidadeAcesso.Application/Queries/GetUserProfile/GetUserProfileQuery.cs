using MediatR;

namespace Tuilow.IdentidadeAcesso.Application.Queries.GetUserProfile;

public sealed record GetUserProfileQuery(Guid UserId) : IRequest<GetUserProfileResponse>;
