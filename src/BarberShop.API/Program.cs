using BarberShop.API.Extensions;
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
    builder.Services.AddControllers();
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

    app.UseRouting();
    app.UseRateLimiter();
    app.UseMiddleware<ErrorHandlingMiddleware>();
    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Erro fatal: a aplicação falhou ao iniciar.");
}
finally
{
    Log.CloseAndFlush();
}
