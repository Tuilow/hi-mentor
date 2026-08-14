using HiMentor.SharedKernel.Application.Interfaces;
using HiMentor.Finance.Domain.Entities;
using HiMentor.Finance.Domain.Interfaces;
using MediatR;

namespace HiMentor.Finance.Application.Commands.UpdatePlatformFee;

public sealed class UpdatePlatformFeeCommandHandler(
    IPlatformFeeConfigurationRepository repository,
    IUnitOfWork uow
) : IRequestHandler<UpdatePlatformFeeCommand, Guid>
{
    public async Task<Guid> Handle(UpdatePlatformFeeCommand request, CancellationToken ct)
    {
        var current = await repository.GetActiveAsync(ct);
        if (current is not null)
        {
            current.Deactivate();
            repository.Update(current);
        }

        var newConfig = PlatformFeeConfiguration.Create(request.Percentage, request.AdminUserId, request.Notes);
        await repository.AddAsync(newConfig, ct);
        await uow.SaveChangesAsync(ct);

        return newConfig.Id;
    }
}
