using MediatR;

namespace Tuilow.Application.Contexts.Identity.Queries.GetUserProfile;

public sealed record GetUserProfileQuery(Guid UserId) : IRequest<GetUserProfileResponse>;
