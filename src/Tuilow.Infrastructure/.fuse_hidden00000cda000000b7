using DogMaster.Application.Common.Interfaces;
using DogMaster.Domain.Contexts.Catalog.Interfaces;
using DogMaster.Domain.Contexts.DogProfile.Interfaces;
using DogMaster.Domain.Contexts.Identity.Interfaces;
using DogMaster.Domain.Contexts.Learning.Interfaces;
using DogMaster.Domain.Contexts.Streaming.Interfaces;
using DogMaster.Domain.Contexts.Subscription.Interfaces;
using DogMaster.Domain.Common.Interfaces;
using DogMaster.Infrastructure.Data;
using DogMaster.Infrastructure.Repositories;
using DogMaster.Infrastructure.Services.Auth;
using DogMaster.Infrastructure.Services.Email;
using DogMaster.Infrastructure.Services.Payment;
using DogMaster.Infrastructure.Services.Streaming;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DogMaster.Infrastructure;

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
        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IDogRepository, DogRepository>();
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
            client.DefaultRequestHeaders.Add("User-Agent", "DogMasterPro/1.0");
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
