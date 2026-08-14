using HiMentor.SharedKernel.Application.Exceptions;
using System.Text.Json;

namespace HiMentor.Host.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro não tratado: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, title, errors) = exception switch
        {
            ValidationException ve => (400, "Erros de Validação",
                ve.Errors.ToDictionary(k => k.Key, v => (object)v.Value)),
            NotFoundException nfe => (404, nfe.Message, (Dictionary<string, object>?)null),
            UnauthorizedException ue => (401, ue.Message, (Dictionary<string, object>?)null),
            ForbiddenException fe => (403, fe.Message, (Dictionary<string, object>?)null),
            BusinessException be => (422, be.Message, (Dictionary<string, object>?)null),
            // Achado M5 da avaliação: diferente do InvalidOperationException genérico abaixo
            // (usado também por validações de domínio que DEVEM chegar ao usuário, ex.:
            // Course.Publish), ExternalServiceException é sempre uma falha de integração
            // externa (Asaas, Cloudflare Stream) — a Message pode conter texto cru do
            // provedor terceiro, então nunca repassamos ela ao cliente. O log em InvokeAsync
            // já registrou a Message completa para investigação interna.
            ExternalServiceException => (502,
                "Não foi possível completar a operação com um serviço externo no momento. Tente novamente em instantes.",
                (Dictionary<string, object>?)null),
            InvalidOperationException ioe => (422, ioe.Message, (Dictionary<string, object>?)null),
            _ => (500, "Ocorreu um erro interno. Tente novamente mais tarde.", (Dictionary<string, object>?)null)
        };

        context.Response.StatusCode = statusCode;

        var response = new
        {
            status = statusCode,
            title,
            errors,
            traceId = context.TraceIdentifier,
            timestamp = DateTime.UtcNow
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}
