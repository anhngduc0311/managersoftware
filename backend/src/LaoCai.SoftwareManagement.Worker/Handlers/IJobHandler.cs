namespace LaoCai.SoftwareManagement.Worker.Handlers;

public interface IJobHandler
{
    string JobType { get; }
    Task HandleAsync(string payloadJson, CancellationToken cancellationToken);
}
