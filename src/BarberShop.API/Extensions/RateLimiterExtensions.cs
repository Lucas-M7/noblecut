using System.Runtime.CompilerServices;
using System.Threading.RateLimiting;

namespace BarberShop.API.Extensions;

public static class RateLimiterExtensions
{
    public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.OnRejected = async (context, token) =>
            {
                var clientIp = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "IP_Desconhecido";
                var requestPath = context.HttpContext.Request.Path;

                var loggerFactory = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("Security.RateLimit");

                logger.LogWarning("🚨 ALERTA DE SEGURANÇA: IP {IP} bloqueado por excesso de tentativas em {Path}.", clientIp, requestPath);

                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    Error = "Muitas tentativas. Aguarde um momento e tente novamente."
                }, cancellationToken: token);
            };

            options.AddPolicy("login", httpContext =>
            {
                var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(
                 partitionKey: clientIp,
                 factory: partition => new FixedWindowRateLimiterOptions
                 {
                     PermitLimit = 5,
                     Window = TimeSpan.FromMinutes(1),
                     QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                     QueueLimit = 0
                 });
            });

            options.AddPolicy("register", httpContext =>
            {
                var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(
                 partitionKey: clientIp,
                 factory: partition => new FixedWindowRateLimiterOptions
                 {
                     Window = TimeSpan.FromHours(1),
                     PermitLimit = 3,
                     QueueLimit = 0,
                     QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                 });

            });

            options.AddPolicy("public-appointments", httpContext =>
            {
                var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(
                 partitionKey: clientIp,
                 factory: partition => new FixedWindowRateLimiterOptions
                 {
                     Window = TimeSpan.FromHours(1),
                     PermitLimit = 10,
                     QueueLimit = 0,
                     QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                 });

            });

            options.AddPolicy("global", httpContext =>
            {
                var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(
                 partitionKey: clientIp,
                 factory: partition => new FixedWindowRateLimiterOptions
                 {
                     Window = TimeSpan.FromHours(1),
                     PermitLimit = 100,
                     QueueLimit = 0,
                     QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                 });

            });
        });

        return services;
    }
}