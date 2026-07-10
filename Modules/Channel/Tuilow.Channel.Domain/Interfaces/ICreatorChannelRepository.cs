using Tuilow.SharedKernel.Domain.Interfaces;
using Tuilow.Channel.Domain.Entities;

namespace Tuilow.Channel.Domain.Interfaces;

public interface ICreatorChannelRepository : IRepository<CreatorChannel>
{
    Task<CreatorChannel?> GetByCreatorIdAsync(Guid creatorId, CancellationToken ct = default);
    Task<CreatorChannel?> GetByHandleAsync(string handle, CancellationToken ct = default);
    Task<bool> HandleExistsAsync(string handle, Guid? excludeChannelId = null, CancellationToken ct = default);
}
