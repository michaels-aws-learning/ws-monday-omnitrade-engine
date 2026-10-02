using System.Text.Json;
using OmniTrade.ApiHandler;
using OmniTrade.Domain;

namespace OmniTrade.Brokers.Alpaca;

public sealed class AlpacaAccountService : IAlpacaAccountService
{
    private readonly IApiHandler<BrokerAccountDto, object> _apiHandler;
    private readonly bool _useSandbox;

    public AlpacaAccountService(IApiHandler<BrokerAccountDto, object> apiHandler, bool useSandbox = true)
    {
        _apiHandler = apiHandler;
        _useSandbox = useSandbox;
    }

    public Task<BrokerAccountDto> CreateAccountAsync(JsonElement requestBody, CancellationToken ct)
    {
        var baseUrl = _useSandbox ? "https://broker-api.sandbox.alpaca.markets" : "https://broker-api.alpaca.markets";
        var url = $"{baseUrl}/v1/accounts";
        var headers = new Dictionary<string, string>();
        // ApiHandler expects a typed request body; to keep generic, serialize to object (string) and let ApiHandler pass it
        return _apiHandler.HttpHandlerAsync(requestBody, url, HttpMethod.Post, headers, "CreateAccount");
    }

    public Task<BrokerAccountDto> GetAccountAsync(string accountId, CancellationToken ct)
    {
        var baseUrl = _useSandbox ? "https://broker-api.sandbox.alpaca.markets" : "https://broker-api.alpaca.markets";
        var url = $"{baseUrl}/v1/accounts/{accountId}"; // Alpaca: get account by id
        var headers = new Dictionary<string, string>();
        return _apiHandler.HttpHandlerAsync(null!, url, HttpMethod.Get, headers, "GetAccountById");
    }

    public Task<IEnumerable<BrokerPositionDto>> GetPositionsAsync(CancellationToken ct)
    {
        // Use separate ApiHandler registration for positions in DI when needed. For now reuse BrokerAccountDto handler with different generic.
        // TODO: Register and inject typed handlers for positions and orders.
        throw new NotImplementedException("GetPositionsAsync not implemented - register a typed ApiHandler for positions.");
    }

    public Task<IEnumerable<BrokerOrderDto>> GetOrdersAsync(CancellationToken ct)
    {
        throw new NotImplementedException("GetOrdersAsync not implemented - register a typed ApiHandler for orders.");
    }
}
