namespace OmniTrade.Domain.Effects;

public interface IEffectsExecutor
{
    Task ExecuteAsync(IEnumerable<IOrderEvent> events, CancellationToken ct);
}
