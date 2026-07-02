using Tuilow.IdentidadeAcesso.Domain.Entities;
using Tuilow.IdentidadeAcesso.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Tuilow.Host.Api.Data;

/// <summary>
/// Popula dados iniciais necessários para a aplicação funcionar. Roda apenas se os dados
/// ainda não existirem (idempotente). Reaproveitado de Tuilow.Infrastructure.Data.DbSeeder,
/// adaptado para os novos namespaces do módulo IdentidadeAcesso — a parte de seed de planos
/// de assinatura fica pra quando o módulo Sales for migrado.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, ILogger logger, IConfiguration config)
    {
        await SeedRolesAsync(db, logger);
        await SeedAdminAsync(db, logger, config);
    }

    /// <summary>Garante que os roles padrão do sistema existam (Student, Creator, Admin, ChannelMember).</summary>
    private static async Task SeedRolesAsync(AppDbContext db, ILogger logger)
    {
        var existingNames = await db.Roles.Select(r => r.Name).ToListAsync();

        foreach (var name in RoleNames.All)
        {
            if (existingNames.Contains(name)) continue;
            db.Roles.Add(Role.Create(name));
            logger.LogInformation("Role seed criado: {Role}", name);
        }

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Cria o usuário administrador inicial a partir de appsettings:
    ///   "AdminSeed": { "Email": "...", "Password": "...", "FirstName": "...", "LastName": "..." }
    /// Se a seção não existir ou o e-mail já estiver cadastrado, não faz nada.
    /// </summary>
    private static async Task SeedAdminAsync(AppDbContext db, ILogger logger, IConfiguration config)
    {
        var section = config.GetSection("AdminSeed");
        var email    = section["Email"];
        var password = section["Password"];
        var first    = section["FirstName"] ?? "Admin";
        var last     = section["LastName"]  ?? "Tuilow";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogDebug("AdminSeed não configurado — pulando criação do admin.");
            return;
        }

        var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == RoleNames.Admin)
            ?? throw new InvalidOperationException("Role Admin não encontrado — SeedRolesAsync deveria ter criado.");

        var exists = await db.Users.AnyAsync(u => u.Email == email);
        if (exists)
        {
            var existing = await db.Users
                .Include(u => u.UserRoleAssignments).ThenInclude(ur => ur.Role)
                .FirstAsync(u => u.Email == email);

            if (!existing.HasRole(RoleNames.Admin))
            {
                // NÃO chama db.Users.Update(existing) — ver comentário em PromoteUserCommandHandler.
                var assignment = existing.AssignRole(adminRole);
                if (assignment is not null)
                    await db.UserRoleAssignments.AddAsync(assignment);

                await db.SaveChangesAsync();
                logger.LogInformation("Usuário {Email} promovido para Admin.", email);
            }
            return;
        }

        logger.LogInformation("Criando usuário admin: {Email}", email);

        var admin = User.Register(email, password, first, last, adminRole);
        admin.ConfirmEmail(admin.EmailConfirmationToken!); // já confirma e-mail

        await db.Users.AddAsync(admin);
        await db.SaveChangesAsync();

        logger.LogInformation("Admin criado com sucesso: {Email}", email);
    }
}
