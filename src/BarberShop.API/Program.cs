using BarberShop.API.Extensions;
using BarberShop.API.Filters;
using BarberShop.API.Middlewares;
using BarberShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

SerilogExtensions.AddSerilogBootstrap();

try
{
    Log.Information("Iniciando a API...");

    var builder = WebApplication.CreateBuilder(args);

    builder.AddCustomSerilg();

    builder.Services.AddAuthRateLimiting();

    builder.Services.AddControllers(options =>
    {
        options.Filters.Add<ValidationFilter>();
    });

    builder.Services.AddDatabase(builder.Configuration);
    builder.Services.AddApplicationServices();
    builder.Services.AddJwtAuthentication(builder.Configuration);

    builder.Services.AddCors(options =>
    {
        var allowedOrigins = builder.Configuration["Cors:AllowedOrigins"]!.Split(",");
        options.AddDefaultPolicy(policy =>
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod());
    });

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
    }

    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate =
            "{RequestMethod} {RequestPath} -> {StatusCode} em {Elapsed:0}ms";
    });
    app.UseRouting();
    app.UseMiddleware<SecurityHeadersMiddleware>();
    app.UseMiddleware<ErrorHandlingMiddleware>();
    app.UseCors();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    Log.Information("API iniciada com sucesso.");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Erro fatal: a API falhou ao iniciar.");
}
finally
{
    // Garante que todos os logs pendentes são escritos antes de encerrar
    Log.CloseAndFlush();
}
