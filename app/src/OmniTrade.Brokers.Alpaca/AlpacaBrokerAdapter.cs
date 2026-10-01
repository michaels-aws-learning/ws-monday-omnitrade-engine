using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OmniTrade.Domain;

namespace OmniTrade.Brokers.Alpaca;

public sealed class AlpacaOptions
{
    public string KeyId { get; set; } = "";
    public string SecretKey { get; set; } = "";
    public bool Paper { get; set; } = true;
}

public sealed class AlpacaBrokerAdapter(
    IOptions<AlpacaOptions> options,
    ILogger<AlpacaBrokerAdapter> logger) : IBrokerAdapter
{
    private readonly AlpacaOptions _options = options.Value;

    public string Name => "Alpaca";

#pragma warning disable CS0067 // raised once the trade_updates stream is wired up
    public event Action<ExecutionReport>? ExecutionReceived;
#pragma warning restore CS0067

    public Task ConnectAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_options.KeyId) || string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            logger.LogWarning("Alpaca keys are not configured; running without a broker connection");
            return Task.CompletedTask;
        }

        // TODO:
        // 1. Create the Alpaca trading client (paper environment when _options.Paper is true).
        // 2. Connect the streaming client and subscribe to trade_updates.
        // 3. Map each update to an ExecutionReport and raise ExecutionReceived.
        // 4. After any reconnect, pull open orders over REST and reconcile before resuming.
        logger.LogInformation("Alpaca adapter configured for {Mode} trading", _options.Paper ? "paper" : "live");
        return Task.CompletedTask;
    }

    public Task SubmitAsync(OrderRequest order, CancellationToken ct) =>
        throw new NotImplementedException("Map OrderRequest to an Alpaca order and submit it with ClientOrderId set.");

    public Task CancelAsync(string clientOrderId, CancellationToken ct) =>
        throw new NotImplementedException("Look up the Alpaca order by client order id and cancel it.");
}