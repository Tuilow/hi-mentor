using Tuilow.SharedKernel.Application.Interfaces;

namespace Tuilow.SharedKernel.Infrastructure.Clock;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
