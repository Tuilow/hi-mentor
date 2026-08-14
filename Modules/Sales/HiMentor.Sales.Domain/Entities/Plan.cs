using HiMentor.SharedKernel.Domain.Common;
using HiMentor.Catalog.Domain.ValueObjects;
using HiMentor.Sales.Domain.Enums;

namespace HiMentor.Sales.Domain.Entities;

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

    /// <summary>
    /// Null = plano da plataforma (modelo legado, dá acesso a todo o catálogo — não removido).
    /// Preenchido = plano de assinatura de UM produto específico (passo 5 do assistente:
    /// "Assinatura" como opção de preço), criado pelo próprio criador para o seu curso.
    /// </summary>
    public Guid? CourseId { get; private set; }

    public IReadOnlyCollection<PlanFeature> Features => _features.AsReadOnly();

    private Plan() { }

    public static Plan Create(string name, decimal price, BillingCycle billingCycle, int trialDays = 0, Guid? courseId = null)
    {
        return new Plan
        {
            Name = name.Trim(),
            Slug = Slug.Create(name),
            Price = Money.Of(price),
            BillingCycle = billingCycle,
            TrialDays = trialDays,
            CourseId = courseId
        };
    }

    public void SetDescription(string description) { Description = description?.Trim(); Touch(); }
    public void SetAsaasPlanId(string id) { AsaasPlanId = id; Touch(); }
    public void Deactivate() { IsActive = false; Touch(); }
    public void Reactivate() { IsActive = true; Touch(); }

    /// <summary>
    /// Atualiza preço/ciclo/trial de um plano já existente — usado quando o criador edita o preço
    /// da assinatura do produto. Reaproveita a mesma linha (mesmo Slug) em vez de desativar e criar
    /// um novo Plan, o que violaria IX_plans_Slug (Slug é derivado do nome do produto, então seria
    /// idêntico ao do plano anterior, ainda presente na tabela mesmo desativado).
    /// </summary>
    public void UpdatePricing(decimal price, BillingCycle billingCycle, int trialDays)
    {
        Price = Money.Of(price);
        BillingCycle = billingCycle;
        TrialDays = trialDays;
        Touch();
    }

    public void AddFeature(string key, string value, string displayName)
    {
        _features.Add(PlanFeature.Create(Id, key, value, displayName));
        Touch();
    }
}
