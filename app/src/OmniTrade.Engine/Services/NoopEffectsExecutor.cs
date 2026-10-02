using OmniTrade.Domain.Effects;

namespace OmniTrade.Engine.Services;

public sealed class NoopEffectsExecutor : IEffectsExecutor
{
    public Task ExecuteAsync(IEnumerable<OmniTrade.Domain.IOrderEvent> events, CancellationToken ct)
    {
        // No-op for now; broker adapter and event sourcing will implement real effects later.
        return Task.CompletedTask;
    }
}
