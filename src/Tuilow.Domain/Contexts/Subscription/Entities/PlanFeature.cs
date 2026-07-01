using Tuilow.Domain.Common.Abstractions;

namespace Tuilow.Domain.Contexts.Subscription.Entities;

public sealed class PlanFeature : Entity
{
    public Guid PlanId { get; private set; }
    public string FeatureKey { get; private set; } = string.Empty;
    public string FeatureValue { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;

    private PlanFeature() { }

    public static PlanFeature Create(Guid planId, string key, string value, string displayName) =>
        new() { PlanId = planId, FeatureKey = key, FeatureValue = value, DisplayName = displayName };
}
