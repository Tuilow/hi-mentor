using System.Text;
using System.Text.Json.Serialization;
using Tuilow.SharedKernel.Infrastructure;
using Tuilow.IdentidadeAcesso.Api;
using Tuilow.Catalog.Api;
using Tuilow.Learning.Api;
// "Meus Perfis" removido temporariamente da experiência do usuário (era um conceito de perfil
// de aprendizado, sem relação com Role/Permission de IdentidadeAcesso). Módulo Journey mantido
// intacto — código, entidades e tabelas — só desligado do pipeline (ver linhas comentadas
// abaixo). Futuro: pode virar a base de "Troca de Contexto" (Aluno/Criador/Admin/Colaborador).
// using Tuilow.Journey.Api;
using Tuilow.Sales.Api;
using Tuilow.Streaming.Api;
using Tuilow.Finance.Api;
using Tuilow.Payout.Api;
using Tuilow.CreatorStudio.Api;
using Tuilow.Channel.Api;
using Tuilow.Host.Api.Data;
using Tuilow.Host.Api.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ─── HOSTING (Railway/contêiner) ───────────────────────────────────────────────
// O Railway injeta a variável de ambiente PORT em runtime e espera que a aplicação
// escute nela em 0.0.0.0. Sem isso, o Kestrel ficaria só na porta fixa da imagem (8080)
// e o proxy do Railway não encontraria a aplicação.
var railwayPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(railwayPort))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{railwayPort}");
}

// ─── SHARED KERNEL + MÓDULOS ──────────────────────────────────────────────────
builder.Services.AddSharedKernel();
builder.Services.AddIdentidadeAcessoModule();
builder.Services.AddCatalogModule();
builder.Services.AddLearningModule();
// builder.Services.AddJourneyModule(); // "Meus Perfis" desligado — ver comentário acima
builder.Services.AddSalesModule(builder.Configuration);
builder.Services.AddStreamingModule(builder.Configuration);
// Novo modelo de negócio (venda avulsa de curso + comissão da plataforma): Finance precisa
// ser registrado antes de Payout, que depende de ICreatorWalletRepository (Finance.Domain).
builder.Services.AddFinanceModule();
builder.Services.AddPayoutModule();
// Jornada Guiada de Criação de Produtos: orquestra Catalog/Sales/Learning/Finance — não
// possui regra de negócio própria sobre essas entidades, só compõe (ver csproj do módulo).
builder.Services.AddCreatorStudioModule(builder.Configuration);
// Canal do Criador: vitrine pública (@handle + redes sociais) com o catálogo de cursos
// publicados do criador — compõe Catalog/Learning, não duplica dado (ver csproj do módulo).
builder.Services.AddChannelModule();
// Próximo módulo migrado entra aqui (ex.: Growth quando tiver domínio real).

// ─── DATABASE ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default"),
        npg => npg.MigrationsHistoryTable("__EFMigrationsHistory", "public"));

    
});

builder.Services.AddScoped<Tuilow.SharedKernel.Application.Interfaces.IUnitOfWork>(
    sp => sp.GetRequiredService<AppDbContext>());

// Repositórios dos módulos pedem "DbContext" genérico no construtor — resolve pro AppDbContext concreto.
builder.Services.AddScoped<Microsoft.EntityFrameworkCore.DbContext>(
    sp => sp.GetRequiredService<AppDbContext>());

// ─── CONTROLLERS (Controllers vivem nos projetos .Api de cada módulo) ──────────
builder.Services.AddControllers()
    .AddApplicationPart(typeof(Tuilow.IdentidadeAcesso.Api.Controllers.AuthController).Assembly)
    .AddApplicationPart(typeof(Tuilow.Catalog.Api.Controllers.CoursesController).Assembly)
    .AddApplicationPart(typeof(Tuilow.Learning.Api.Controllers.EnrollmentsController).Assembly)
    // .AddApplicationPart(typeof(Tuilow.Journey.Api.Controllers.LearnerProfilesController).Assembly) // "Meus Perfis" desligado
    .AddApplicationPart(typeof(Tuilow.Sales.Api.Controllers.SubscriptionsController).Assembly)
    .AddApplicationPart(typeof(Tuilow.Streaming.Api.Controllers.VideosController).Assembly)
    .AddApplicationPart(typeof(Tuilow.Finance.Api.Controllers.FinanceController).Assembly)
    .AddApplicationPart(typeof(Tuilow.Payout.Api.Controllers.PayoutsController).Assembly)
    .AddApplicationPart(typeof(Tuilow.CreatorStudio.Api.Controllers.CreatorStudioController).Assembly)
    .AddApplicationPart(typeof(Tuilow.Channel.Api.Controllers.ChannelController).Assembly)
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        opt.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddEndpointsApiExplorer();

// ─── SWAGGER ─────────────────────────────────────────────────────────────────
builder.Services.AddSwaggerGen(opt =>
{
    opt.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Tuilow API (Host modular)",
        Version = "v1",
        Description = "Composição dos módulos Tuilow — IdentidadeAcesso, Catalog, Learning, Sales, Streaming, Finance, Payout, CreatorStudio e Channel. Plataforma aberta a criadores: sem mensalidade, comissão retida apenas sobre vendas de curso. Assistente guiado de criação de produto em CreatorStudio; Canal do Criador (vitrine pública) em Channel. Só Growth segue como stub. (Módulo Journey existe no código mas está temporariamente desligado do pipeline — ver comentários no topo deste arquivo.)",
        Contact = new OpenApiContact { Name = "Tuilow", Email = "api@tuilow.com.br" }
    });

    opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Insira o JWT token: Bearer {token}",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    opt.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ─── JWT AUTHENTICATION ───────────────────────────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret não configurado.");

// ─── ASAAS WEBHOOK SECRET (obrigatório fora de Development) ───────────────────
// Sem esse secret, AsaasPaymentService.ValidateWebhookSignature aceita qualquer POST em
// /api/v1/webhooks/asaas sem checar o header "asaas-access-token" — aceitável só em
// Development, para testar o fluxo antes de configurar o webhook de verdade no painel da
// Asaas. Fora de Development, falha o startup para nunca subir com o webhook de pagamento
// aberto a qualquer um (quem descobrisse um AsaasPaymentId poderia forjar confirmação de
// pagamento e liberar acesso/comissão sem pagar).
if (!builder.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(builder.Configuration["Asaas:WebhookSecret"]))
{
    throw new InvalidOperationException(
        "Asaas:WebhookSecret não configurado. Obrigatório fora de Development — configure o mesmo token cadastrado no painel da Asaas ao criar o webhook.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

// ─── CORS ────────────────────────────────────────────────────────────────────
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowFrontend", policy =>
        policy
            .WithOrigins(
                builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
                ?? ["http://localhost:3000"])
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            // Necessário para o protocolo TUS (upload resumível de vídeo): sem expor esses
            // headers, o cliente JS (tus-js-client) não consegue ler Upload-Offset/Upload-Length
            // da resposta e falha com "failed to resume upload".
            .WithExposedHeaders("Upload-Offset", "Upload-Length", "Upload-Defer-Length",
                "Tus-Resumable", "Tus-Version", "Tus-Max-Size", "Location"));
});

// ─── HEALTH CHECKS ────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

// ─── BUILD ───────────────────────────────────────────────────────────────────
var app = builder.Build();

// ─── MIDDLEWARE PIPELINE ─────────────────────────────────────────────────────
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(opt =>
    {
        opt.SwaggerEndpoint("/swagger/v1/swagger.json", "Tuilow API v1");
        opt.RoutePrefix = "swagger";
    });
}

// O Railway (e proxies equivalentes) termina o TLS na borda e encaminha para o contêiner
// em HTTP puro. Sem confiar em X-Forwarded-Proto aqui, UseHttpsRedirection abaixo acharia
// que toda requisição chegou em HTTP e ficaria redirecionando para HTTPS infinitamente
// (loop de redirect que quebra chamadas do front-end na Vercel).
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedHeadersOptions.KnownNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

// ─── AUTO-MIGRATION + SEED ────────────────────────────────────────────────────
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var seedLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    // Antes só rodava em Development. No Railway o ambiente é Production por padrão,
    // então sem isso o banco nunca recebia as migrations e a API quebraria em qualquer
    // endpoint que tocasse tabela nenhuma existiria. Aplicar migrations no start-up é
    // seguro aqui (app single-instance, sem múltiplos deploys concorrentes).
    await db.Database.MigrateAsync();

    await DbSeeder.SeedAsync(db, seedLogger, builder.Configuration);
}

app.Run();
