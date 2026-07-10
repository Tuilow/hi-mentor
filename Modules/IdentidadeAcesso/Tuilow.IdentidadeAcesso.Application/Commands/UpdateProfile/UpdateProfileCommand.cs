using MediatR;

namespace Tuilow.IdentidadeAcesso.Application.Commands.UpdateProfile;

/// <summary>
/// Editar nome/telefone/bio/avatar do próprio usuário. Os métodos de domínio
/// (UserProfile.Update/SetAvatar) já existiam — só faltava um command/endpoint expondo-os.
/// Alimenta, entre outras telas, o editor do Canal do Criador (bio/avatar aparecem lá, mas são
/// dado do usuário, não duplicado em Channel).
/// </summary>
public sealed record UpdateProfileCommand(
    Guid UserId,
    string FirstName,
    string LastName,
    string? Phone,
    DateOnly? BirthDate,
    string? Bio,
    string? AvatarUrl
) : IRequest;
