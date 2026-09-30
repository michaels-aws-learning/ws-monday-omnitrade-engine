namespace OmniTrade.Domain;

/// <summary>
/// Broker-neutral boundary. Alpaca is one implementation; add a SimulatedBroker for tests and replay.
/// </summary>
public interface IBrokerAdapter
{
    string Name { get; }

    Task ConnectAsync(CancellationToken ct);

    Task SubmitAsync(OrderRequest order, CancellationToken ct);

    Task CancelAsync(string clientOrderId, CancellationToken ct);

    event Action<ExecutionReport>? ExecutionReceived;
}