using System.Threading.Channels;
using OmniTrade.Domain;
using OmniTrade.Domain.Effects;
using OmniTrade.Domain.Events;

namespace OmniTrade.Engine.Services;

public sealed class OrderManager : BackgroundService
{
    private readonly Channel<IOrderCommand> _commands = Channel.CreateUnbounded<IOrderCommand>();
    private readonly IOrderBook _orderBook;
    private readonly IEventStore _eventStore;
    private readonly IEffectsExecutor _effects;
    private readonly IEventPublisher _publisher;
    private readonly ILogger<OrderManager> _logger;

    public OrderManager(IOrderBook orderBook, IEventStore eventStore, IEffectsExecutor effects, IEventPublisher publisher, ILogger<OrderManager> logger)
    {
        _orderBook = orderBook;
        _eventStore = eventStore;
        _effects = effects;
        _publisher = publisher;
        _logger = logger;
    }

    public ChannelWriter<IOrderCommand> Writer => _commands.Writer;

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("OrderManager starting");
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var command in _commands.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                var events = _orderBook.Handle(command);
                await _eventStore.AppendAsync(events, stoppingToken);
                await _effects.ExecuteAsync(events, stoppingToken);
                await _publisher.PublishAsync(events, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order command");
            }
        }
    }
}
