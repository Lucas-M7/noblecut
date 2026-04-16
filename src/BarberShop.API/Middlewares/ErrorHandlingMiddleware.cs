using System.Net;
using System.Text.Json;

namespace BarberShop.API.Middlewares;

public class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
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

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = exception switch
        {
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            KeyNotFoundException => HttpStatusCode.NotFound,
            InvalidOperationException => HttpStatusCode.BadRequest,
            _ => HttpStatusCode.InternalServerError // ATENÇÃO AQUI -> Mascarar erros 500
        };

        // Log com nível adequado dependendo do tipo de erro
        if (statusCode == HttpStatusCode.InternalServerError)
        {
            logger.LogError(exception, 
            "Erro interno. Path: {Path} Method: {Method}",
            context.Request.Path, 
            context.Request.Method);
        }
        else
        {
            logger.LogWarning("Erro tratado [{StatusCode}]: {Message} Path: {Path}",
            (int)statusCode,
            exception.Message,
            context.Request.Path);
        }

        var response = new
        {
            error = statusCode == HttpStatusCode.InternalServerError
            ? "Ocorreu um erro interno. Tente novamente mais tarde."
            : exception.Message
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
        });

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        return context.Response.WriteAsync(json);
    }
}