namespace OmniTrade.Domain;

public interface IEventStore
{
    Task AppendAsync(IEnumerable<IOrderEvent> events, CancellationToken ct);

    Task<IEnumerable<IOrderEvent>> ReadAllAsync(CancellationToken ct);
}
