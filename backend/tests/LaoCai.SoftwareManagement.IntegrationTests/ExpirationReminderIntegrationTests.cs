using FluentAssertions;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LaoCai.SoftwareManagement.IntegrationTests;

public class ExpirationReminderIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ExpirationReminderIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProcessReminders_ShouldDetectExpiringContracts_AndNotDuplicateOnRerun()
    {
        using var scope = _factory.Services.CreateScope();
        var reminderService = scope.ServiceProvider.GetRequiredService<IExpirationReminderService>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        // First run
        var firstResult = await reminderService.ProcessRemindersAsync();
        firstResult.TotalScanned.Should().BeGreaterThan(0);

        // Re-running immediately should not generate duplicate notifications due to deduplication keys
        var secondResult = await reminderService.ProcessRemindersAsync();
        secondResult.RemindersSent.Should().Be(0);
    }
}
