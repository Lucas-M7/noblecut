using System.Net;
using System.Text.Json;

namespace BarberShop.API.Middlewares;

public class ErrorHandlingMiddleware(
    RequestDelegate next,
    ILogger<ErrorHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = exception switch
        {
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            KeyNotFoundException        => HttpStatusCode.NotFound,
            InvalidOperationException   => HttpStatusCode.BadRequest,
            _                          => HttpStatusCode.InternalServerError
        };

        // Erros 500 são logados com todos os detalhes — só internamente
        if (statusCode == HttpStatusCode.InternalServerError)
        {
            logger.LogError(exception,
                "Erro interno não tratado. Path: {Path} Method: {Method}",
                context.Request.Path,
                context.Request.Method);
        }
        else
        {
            // Erros esperados (400, 401, 404) logados como warning
            logger.LogWarning(
                "Erro tratado [{Status}]: {Message} Path: {Path}",
                (int)statusCode,
                exception.Message,
                context.Request.Path);
        }

        // Para erros 500: mensagem genérica — nunca expõe detalhes ao cliente
        var clientMessage = statusCode == HttpStatusCode.InternalServerError
            ? "Ocorreu um erro interno. Tente novamente mais tarde."
            : exception.Message;

        var json = JsonSerializer.Serialize(
            new { error = clientMessage },
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;
        await context.Response.WriteAsync(json);
    }
}