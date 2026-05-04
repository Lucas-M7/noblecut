using BarberShop.Application.Resolvers;
using BarberShop.Application.Services;
using BarberShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BarberShop.API.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("DefaultConnection")));

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<BarberProfileResolver>();
        
        services.AddScoped<EmailService>();
        services.AddScoped<AuthService>();
        services.AddScoped<ProfileService>();
        services.AddScoped<ServiceService>();
        services.AddScoped<WorkingHoursService>();
        services.AddScoped<BlockService>();
        services.AddScoped<AppointmentService>();
        services.AddScoped<AvailabilityService>();
        services.AddScoped<SpecialHoursService>();
        services.AddScoped<ReportService>();
        services.AddScoped<CloudinaryService>();

        return services;
    }
}