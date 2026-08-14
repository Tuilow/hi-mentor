using HiMentor.SharedKernel.Application.Interfaces;

namespace HiMentor.SharedKernel.Infrastructure.Clock;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
