using OmniTrade.Domain;

namespace OmniTrade.Engine.Services;

/// <summary>
/// Long-running engine loop. Owns the broker connection for the life of the process.
/// </summary>
public sealed class EngineWorker(IBrokerAdapter broker, ILogger<EngineWorker> logger, OrderManager orderManager) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Engine starting with broker {Broker}", broker.Name);

        try
        {
            // Start broker connection
            await broker.ConnectAsync(stoppingToken);

            // OrderManager is hosted as a service and will process commands independently.
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // normal shutdown
        }
        finally
        {
            logger.LogInformation("Shutdown requested: pausing algos and halting new child orders");
            // TODO: pause algo schedules and decide whether to cancel open child orders
        }
    }
}
