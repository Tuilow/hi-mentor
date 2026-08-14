using HiMentor.IdentidadeAcesso.Domain.Interfaces;
using HiMentor.SharedKernel.Application.Exceptions;
using MediatR;

namespace HiMentor.IdentidadeAcesso.Application.Queries.GetUserProfile;

public sealed class GetUserProfileQueryHandler(IUserRepository userRepository)
    : IRequestHandler<GetUserProfileQuery, GetUserProfileResponse>
{
    public async Task<GetUserProfileResponse> Handle(GetUserProfileQuery request, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, ct)
            ?? throw new NotFoundException("Usuário", request.UserId);

        return new GetUserProfileResponse(
            user.Id, user.Email.Value,
            user.Profile.FirstName, user.Profile.LastName, user.Profile.FullName,
            user.Profile.AvatarUrl, user.Profile.Phone, user.Profile.BirthDate,
            user.Profile.Bio, user.Roles.Select(r => r.Name).ToList(), user.Status.ToString(), user.CreatedAt);
    }
}
