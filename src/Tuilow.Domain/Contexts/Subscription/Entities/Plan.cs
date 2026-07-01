using Tuilow.Domain.Common.Abstractions;
using Tuilow.Domain.Contexts.Catalog.ValueObjects;
using Tuilow.Domain.Contexts.Subscription.Enums;

namespace Tuilow.Domain.Contexts.Subscription.Entities;

public sealed class Plan : AggregateRoot
{
    private readonly List<PlanFeature> _features = [];

    public string Name { get; private set; } = string.Empty;
    public Slug Slug { get; private set; } = null!;
    public string? Description { get; private set; }
    public Money Price { get; private set; } = null!;
    public BillingCycle BillingCycle { get; private set; }
    public int TrialDays { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string? AsaasPlanId { get; private set; }

    public IReadOnlyCollection<PlanFeature> Features => _features.AsReadOnly();

    private Plan() { }

    public static Plan Create(string name, decimal price, BillingCycle billingCycle, int trialDays = 0)
    {
        return new Plan
        {
            Name = name.Trim(),
            Slug = Slug.Create(name),
            Price = Money.Of(price),
            BillingCycle = billingCycle,
            TrialDays = trialDays
        };
    }

    public void SetDescription(string description) { Description = description?.Trim(); Touch(); }
    public void SetAsaasPlanId(string id) { AsaasPlanId = id; Touch(); }
    public void Deactivate() { IsActive = false; Touch(); }

    public void AddFeature(string key, string value, string displayName)
    {
        _features.Add(PlanFeature.Create(Id, key, value, displayName));
        Touch();
    }
}
