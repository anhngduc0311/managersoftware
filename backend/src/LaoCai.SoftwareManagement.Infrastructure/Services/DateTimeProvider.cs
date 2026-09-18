using LaoCai.SoftwareManagement.Application.Common.Interfaces;

namespace LaoCai.SoftwareManagement.Infrastructure.Services;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
