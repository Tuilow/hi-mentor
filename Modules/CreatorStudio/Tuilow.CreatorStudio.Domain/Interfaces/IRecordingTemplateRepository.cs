using Tuilow.SharedKernel.Domain.Interfaces;
using Tuilow.CreatorStudio.Domain.Entities;

namespace Tuilow.CreatorStudio.Domain.Interfaces;

public interface IRecordingTemplateRepository : IRepository<RecordingTemplate>
{
    Task<IEnumerable<RecordingTemplate>> ListByCreatorAsync(Guid creatorId, CancellationToken ct = default);
    Task<RecordingTemplate?> GetDefaultByCreatorAsync(Guid creatorId, CancellationToken ct = default);
}
