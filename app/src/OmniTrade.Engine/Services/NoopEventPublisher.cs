using OmniTrade.Domain.Events;

namespace OmniTrade.Engine.Services;

public sealed class NoopEventPublisher : IEventPublisher
{
    public Task PublishAsync(IEnumerable<OmniTrade.Domain.IOrderEvent> events, CancellationToken ct)
    {
        // No-op for now; later wire SignalR/BlotterHub
        return Task.CompletedTask;
    }
}
