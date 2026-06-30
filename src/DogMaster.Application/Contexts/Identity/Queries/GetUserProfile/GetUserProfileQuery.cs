using MediatR;

namespace DogMaster.Application.Contexts.Identity.Queries.GetUserProfile;

public sealed record GetUserProfileQuery(Guid UserId) : IRequest<GetUserProfileResponse>;
