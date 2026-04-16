using Serilog;
using Serilog.Events;

namespace BarberShop.API.Extensions;

public static class SerilogExtensions
{
    public static void AddSerilogBootstrap()
    {
        Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .WriteTo.Console()
        .CreateBootstrapLogger();
    }

    public static WebApplicationBuilder AddCustomSerilg(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, services, configuration) => configuration
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File("Logs/seguranca-.txt",
                rollingInterval: RollingInterval.Day,
                restrictedToMinimumLevel: LogEventLevel.Warning)
        );

        return builder;
    }
}