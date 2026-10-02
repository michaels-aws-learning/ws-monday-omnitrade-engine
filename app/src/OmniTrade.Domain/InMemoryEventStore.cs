namespace OmniTrade.Domain;

public sealed class InMemoryEventStore : IEventStore
{
    private readonly List<IOrderEvent> _events = new();

    public Task AppendAsync(IEnumerable<IOrderEvent> events, CancellationToken ct)
    {
        _events.AddRange(events);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<IOrderEvent>> ReadAllAsync(CancellationToken ct)
    {
        return Task.FromResult<IEnumerable<IOrderEvent>>(_events.ToArray());
    }
}
