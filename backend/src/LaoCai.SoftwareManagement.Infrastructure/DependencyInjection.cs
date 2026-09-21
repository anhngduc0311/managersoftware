using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using LaoCai.SoftwareManagement.Infrastructure.Persistence;
using LaoCai.SoftwareManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LaoCai.SoftwareManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=laocai_software_db;Username=laocai_user;Password=laocai_secret_dev_pass_2026;";

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "public");
            })
            .UseSnakeCaseNamingConvention();
        });

        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<IPasswordHasherService, PasswordHasherService>();
        services.AddScoped<IOrganizationService, LaoCai.SoftwareManagement.Application.Services.OrganizationService>();
        services.AddScoped<IScopeAuthorizationService, LaoCai.SoftwareManagement.Application.Services.ScopeAuthorizationService>();
        services.AddScoped<ICatalogService, LaoCai.SoftwareManagement.Application.Services.CatalogService>();
        services.AddScoped<IBackgroundJobService, LaoCai.SoftwareManagement.Application.Services.BackgroundJobService>();
        services.AddScoped<INotificationService, LaoCai.SoftwareManagement.Application.Services.NotificationService>();
        services.AddScoped<IDeploymentService, LaoCai.SoftwareManagement.Application.Services.DeploymentService>();

        return services;
    }
}
