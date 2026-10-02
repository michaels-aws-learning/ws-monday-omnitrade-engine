namespace OmniTrade.Domain.Events;

public interface IEventPublisher
{
    Task PublishAsync(IEnumerable<IOrderEvent> events, CancellationToken ct);
}
