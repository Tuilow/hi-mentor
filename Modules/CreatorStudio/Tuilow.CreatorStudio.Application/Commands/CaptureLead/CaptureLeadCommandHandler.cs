using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.CreatorStudio.Domain.Entities;
using Tuilow.CreatorStudio.Domain.Interfaces;
using MediatR;

namespace Tuilow.CreatorStudio.Application.Commands.CaptureLead;

public sealed class CaptureLeadCommandHandler(
    ILeadRepository leadRepository, IUnitOfWork uow
) : IRequestHandler<CaptureLeadCommand, Guid>
{
    public async Task<Guid> Handle(CaptureLeadCommand request, CancellationToken ct)
    {
        var lead = Lead.Create(request.CourseId, request.Name, request.Email, request.Phone, request.Source);
        await leadRepository.AddAsync(lead, ct);
        await uow.SaveChangesAsync(ct);
        return lead.Id;
    }
}
