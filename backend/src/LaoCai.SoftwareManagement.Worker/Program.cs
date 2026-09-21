using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using LaoCai.SoftwareManagement.Infrastructure;
using LaoCai.SoftwareManagement.Worker.Handlers;
using LaoCai.SoftwareManagement.Worker.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructureServices(builder.Configuration);

// Background Worker has its own internal system identity for audit logs
builder.Services.AddScoped<ICurrentUserService, WorkerCurrentUserService>();

// Register Handlers
builder.Services.AddScoped<IJobHandler, WorkflowNotificationJobHandler>();
builder.Services.AddScoped<IJobHandler, DocumentScanJobHandler>();
builder.Services.AddScoped<IJobHandler, DeploymentImportValidateJobHandler>();
builder.Services.AddScoped<IJobHandler, DeploymentExportJobHandler>();
builder.Services.AddScoped<IJobHandler, ExpirationReminderJobHandler>();

// Register Worker as HostedService
builder.Services.AddSingleton<BackgroundJobWorker>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<BackgroundJobWorker>());

var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Lao Cai Software Management Background Worker starting up...");

await host.RunAsync();

public class WorkerCurrentUserService : ICurrentUserService
{
    public Guid? UserId => null;
    public string? UserName => "system-worker";
    public string? CorrelationId => Guid.NewGuid().ToString();
    public bool IsAuthenticated => false;
}
