using MassTransit;

namespace Krizovka.Users.Infrastructure.Services;

/// <summary>
/// No-op implementation of IPublishEndpoint for development when MassTransit is disabled
/// </summary>
public class NoOpPublishEndpoint : IPublishEndpoint
{
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
    {
        return new NoOpConnectHandle();
    }

    public Task Publish<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        // No-op: just return completed task
        return Task.CompletedTask;
    }

    public Task Publish<T>(T message, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default) where T : class
    {
        return Task.CompletedTask;
    }

    public Task Publish<T>(T message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default) where T : class
    {
        return Task.CompletedTask;
    }

    public Task Publish(object message, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task Publish(object message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task Publish(object message, Type messageType, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task Publish(object message, Type messageType, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task Publish<T>(object values, CancellationToken cancellationToken = default) where T : class
    {
        return Task.CompletedTask;
    }

    public Task Publish<T>(object values, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default) where T : class
    {
        return Task.CompletedTask;
    }

    public Task Publish<T>(object values, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default) where T : class
    {
        return Task.CompletedTask;
    }

    private class NoOpConnectHandle : ConnectHandle
    {
        public void Disconnect()
        {
        }

        public void Dispose()
        {
        }
    }
}
