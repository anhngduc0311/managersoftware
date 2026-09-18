using LaoCai.SoftwareManagement.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructureServices(builder.Configuration);

var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Lao Cai Software Management Background Worker starting up...");

await host.RunAsync();
