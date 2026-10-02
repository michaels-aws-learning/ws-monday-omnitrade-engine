using OmniTrade.ApiHandler;
using OmniTrade.Brokers.Alpaca.Models;
using OmniTrade.Domain;

namespace OmniTrade.Brokers.Alpaca;

public sealed class AlpacaBrokerClient : IAlpacaBrokerClient
{
    private readonly IApiHandler<AccountDto, object> _accountHandler;
    private readonly IApiHandler<IEnumerable<PositionDto>, object> _positionsHandler;
    private readonly IApiHandler<IEnumerable<OrderDto>, object> _ordersHandler;
    private readonly string _baseUrl;

    public AlpacaBrokerClient(IApiHandler<AccountDto, object> accountHandler,
        IApiHandler<IEnumerable<PositionDto>, object> positionsHandler,
        IApiHandler<IEnumerable<OrderDto>, object> ordersHandler,
        bool paper = true)
    {
        _accountHandler = accountHandler;
        _positionsHandler = positionsHandler;
        _ordersHandler = ordersHandler;
        _baseUrl = paper ? "https://api.alpaca.markets" : "https://api.alpaca.markets"; // same base for broker endpoints; paper/live can change keys
    }

    public Task<AccountDto> GetAccountAsync(CancellationToken ct)
    {
        var url = $"{_baseUrl}/v2/account";
        var headers = new Dictionary<string, string>();
        return _accountHandler.HttpHandlerAsync(null!, url, HttpMethod.Get, headers, "GetAccount");
    }

    public Task<IEnumerable<PositionDto>> GetPositionsAsync(CancellationToken ct)
    {
        var url = $"{_baseUrl}/v2/positions";
        var headers = new Dictionary<string, string>();
        return _positionsHandler.HttpHandlerAsync(null!, url, HttpMethod.Get, headers, "GetPositions");
    }

    public Task<IEnumerable<OrderDto>> GetOrdersAsync(CancellationToken ct)
    {
        var url = $"{_baseUrl}/v2/orders";
        var headers = new Dictionary<string, string>();
        return _ordersHandler.HttpHandlerAsync(null!, url, HttpMethod.Get, headers, "GetOrders");
    }
}
