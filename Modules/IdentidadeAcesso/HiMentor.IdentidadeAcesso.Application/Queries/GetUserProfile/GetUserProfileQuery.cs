using MediatR;

namespace HiMentor.IdentidadeAcesso.Application.Queries.GetUserProfile;

public sealed record GetUserProfileQuery(Guid UserId) : IRequest<GetUserProfileResponse>;
