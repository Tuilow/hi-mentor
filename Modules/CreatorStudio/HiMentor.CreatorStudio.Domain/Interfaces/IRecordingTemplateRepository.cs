using HiMentor.SharedKernel.Domain.Interfaces;
using HiMentor.CreatorStudio.Domain.Entities;

namespace HiMentor.CreatorStudio.Domain.Interfaces;

public interface IRecordingTemplateRepository : IRepository<RecordingTemplate>
{
    Task<IEnumerable<RecordingTemplate>> ListByCreatorAsync(Guid creatorId, CancellationToken ct = default);
    Task<RecordingTemplate?> GetDefaultByCreatorAsync(Guid creatorId, CancellationToken ct = default);
}
