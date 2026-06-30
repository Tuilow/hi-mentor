using DogMaster.Application.Common.Interfaces;
using DogMaster.Domain.Common.Interfaces;
using DogMaster.Domain.Contexts.Identity.Interfaces;
using MediatR;

namespace DogMaster.Application.Contexts.Identity.Commands.ForgotPassword;

public sealed class ForgotPasswordCommandHandler(
    IUserRepository userRepository, IUnitOfWork uow, IEmailService emailService
) : IRequestHandler<ForgotPasswordCommand>
{
    public async Task Handle(ForgotPasswordCommand request, CancellationToken ct)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, ct);
        if (user is null) return; // Não revela se e-mail existe

        var token = user.RequestPasswordReset();
        userRepository.Update(user);
        await uow.SaveChangesAsync(ct);

        await emailService.SendPasswordResetAsync(user.Email.Value, user.Profile.FirstName, token, ct);
    }
}
