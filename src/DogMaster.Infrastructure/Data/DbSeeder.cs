using DogMaster.Domain.Contexts.Subscription.Entities;
using DogMaster.Domain.Contexts.Subscription.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DogMaster.Infrastructure.Data;

/// <summary>
/// Popula dados iniciais necessários para a aplicação funcionar.
/// Roda apenas se os dados ainda não existirem (idempotente).
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db, ILogger logger)
    {
        await SeedPlansAsync(db, logger);
    }

    private static async Task SeedPlansAsync(ApplicationDbContext db, ILogger logger)
    {
        if (await db.Plans.AnyAsync()) return;

        logger.LogInformation("Seeding subscription plans...");

        // ─── Plano Básico ─────────────────────────────────────────────────────
        var basic = Plan.Create("Básico", 29.90m, BillingCycle.Monthly, trialDays: 7);
        basic.SetDescription("Ideal para começar a treinar seu cão");
        basic.AddFeature("courses",     "5",         "Acesso a 5 cursos");
        basic.AddFeature("dogs",        "1",         "Cadastro de 1 cachorro");
        basic.AddFeature("videos",      "included",  "Vídeos inclusos nos cursos");
        basic.AddFeature("support",     "email",     "Suporte por e-mail");

        // ─── Plano Pro ────────────────────────────────────────────────────────
        var pro = Plan.Create("Pro", 59.90m, BillingCycle.Monthly, trialDays: 7);
        pro.SetDescription("Para tutores dedicados e seus cães");
        pro.AddFeature("courses",      "unlimited", "Todos os cursos disponíveis");
        pro.AddFeature("dogs",         "3",         "Cadastro de até 3 cachorros");
        pro.AddFeature("videos",       "hd",        "Vídeos em HD");
        pro.AddFeature("certificate",  "true",      "Certificados de conclusão");
        pro.AddFeature("support",      "priority",  "Suporte prioritário");

        // ─── Plano Expert (anual) ─────────────────────────────────────────────
        var expert = Plan.Create("Expert", 499.90m, BillingCycle.Annual, trialDays: 14);
        expert.SetDescription("Acesso completo por um ano — melhor custo-benefício");
        expert.AddFeature("courses",      "unlimited", "Todos os cursos + lançamentos");
        expert.AddFeature("dogs",         "unlimited", "Cachorros ilimitados");
        expert.AddFeature("videos",       "4k",        "Vídeos em 4K");
        expert.AddFeature("certificate",  "true",      "Certificados de conclusão");
        expert.AddFeature("support",      "whatsapp",  "Suporte via WhatsApp");
        expert.AddFeature("live",         "monthly",   "Aulas ao vivo mensais");
        expert.AddFeature("community",    "vip",       "Grupo VIP de tutores");

        db.Plans.AddRange(basic, pro, expert);
        await db.SaveChangesAsync();

        logger.LogInformation("Plans seeded: Básico (R$29,90/mês), Pro (R$59,90/mês), Expert (R$499,90/ano).");
    }
}
