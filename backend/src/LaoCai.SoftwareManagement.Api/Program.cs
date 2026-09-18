using LaoCai.SoftwareManagement.Api.Filters;
using LaoCai.SoftwareManagement.Api.Middlewares;
using LaoCai.SoftwareManagement.Api.Services;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using LaoCai.SoftwareManagement.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add Infrastructure & Application DI
builder.Services.AddInfrastructureServices(builder.Configuration);

// Add HttpContextAccessor and CurrentUserService
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Add Controllers with ETag Filter
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ETagResultFilter>();
});

// Configure Antiforgery for CSRF protection
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
    options.Cookie.Name = "XSRF-TOKEN-COOKIE";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCorsPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Configure Native OpenAPI for .NET 10
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure Middleware Pipeline
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("DefaultCorsPolicy");

app.UseRouting();

app.MapControllers();

app.Run();

// Required for IntegrationTests WebApplicationFactory
public partial class Program { }
