using Tuilow.SharedKernel.Application.Exceptions;
using System.Text.Json;

namespace Tuilow.Host.Api.Middleware;

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
