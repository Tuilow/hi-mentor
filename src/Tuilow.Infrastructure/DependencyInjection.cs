using Tuilow.Application.Common.Interfaces;
using Tuilow.Domain.Contexts.Catalog.Interfaces;
using Tuilow.Domain.Contexts.Profiles.Interfaces;
using Tuilow.Domain.Contexts.Identity.Interfaces;
using Tuilow.Domain.Contexts.Learning.Interfaces;
using Tuilow.Domain.Contexts.Streaming.Interfaces;
using Tuilow.Domain.Contexts.Subscription.Interfaces;
using Tuilow.Domain.Common.Interfaces;
using Tuilow.Infrastructure.Data;
using Tuilow.Infrastructure.Repositories;
using Tuilow.Infrastructure.Services.Auth;
using Tuilow.Infrastructure.Services.Email;
using Tuilow.Infrastructure.Services.Payment;
using Tuilow.Infrastructure.Services.Streaming;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Tuilow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        // ─── Database ─────────────────────────────────────────────────────────
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default"),
                npg => npg.MigrationsHistoryTable("__EFMigrationsHistory", "public")));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());

        // ─── Repositories ─────────────────────────────────────────────────────
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<ILearnerProfileRepository, LearnerProfileRepository>();
        services.AddScoped<IVideoRepository, VideoRepository>();

        // ─── Services ─────────────────────────────────────────────────────────
        services.AddScoped<IJwtService, JwtService>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IEmailService, EmailService>();

        // ─── Asaas HTTP Client ────────────────────────────────────────────────
        services.AddHttpClient<IPaymentService, AsaasPaymentService>(client =>
        {
            var baseUrl = configuration["Asaas:BaseUrl"] ?? "https://sandbox.asaas.com";
            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Add("access_token", configuration["Asaas:ApiKey"]);
            client.DefaultRequestHeaders.Add("User-Agent", "Tuilow/1.0");
        });

        // ─── Cloudflare Stream HTTP Client (ou Mock) ────────────────────────────
        var cloudfareMock = configuration.GetValue<bool>("Cloudflare:MockMode");
        if (cloudfareMock)
        {
            services.AddScoped<IStreamingService, MockStreamingService>();
        }
        else
        {
            services.AddHttpClient<IStreamingService, CloudflareStreamService>(client =>
            {
                client.BaseAddress = new Uri("https://api.cloudflare.com");
                var apiToken = configuration["Cloudflare:ApiToken"];
                if (!string.IsNullOrWhiteSpace(apiToken))
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiToken}");
            });
        }

        return services;
    }
}
