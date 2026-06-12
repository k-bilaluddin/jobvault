using JobVault.Contracts.Events;

namespace JobVault.Application.Interfaces;

/// <summary>
/// Publisher for sending messages to RabbitMQ.
/// </summary>
public interface IRabbitMqPublisher : IDisposable
{
    /// <summary>
    /// Publishes a job application event to RabbitMQ.
    /// </summary>
    /// <param name="jobEvent">The event to publish.</param>
    Task PublishJobApplicationEventAsync(JobApplicationEvent jobEvent);
}