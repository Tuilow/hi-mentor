using HiMentor.Catalog.Application.Interfaces;
using HiMentor.IdentidadeAcesso.Domain.Interfaces;

namespace HiMentor.Catalog.Infrastructure.Services;

/// <summary>Implementação real de <see cref="IInstructorLookup"/> — consulta o módulo IdentidadeAcesso.</summary>
public sealed class IdentidadeAcessoInstructorLookup(IUserRepository userRepository) : IInstructorLookup
{
    public async Task<InstructorProfile?> GetProfileAsync(Guid instructorId, CancellationToken ct = default)
    {
        var user = await userRepository.GetByIdAsync(instructorId, ct);
        return user is null
            ? null
            : new InstructorProfile(user.Profile.FullName, user.Profile.AvatarUrl, user.Profile.Bio);
    }
}
