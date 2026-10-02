using System.Text.Json;

namespace OmniTrade.Domain;

public sealed record BrokerAccountDto(
    string Id,
    string? AccountNumber,
    string? Status,
    string? Currency,
    DateTimeOffset? CreatedAt,
    string? LastEquity,
    string? PrimaryAccountHolderId
);

public sealed record BrokerPositionDto(
    string Symbol,
    decimal Quantity,
    decimal MarketValue,
    decimal AverageEntryPrice
);

public sealed record BrokerOrderDto(
    string Id,
    string ClientOrderId,
    string Symbol,
    string Side,
    string Type,
    decimal Quantity,
    string Status
);

public interface IBrokerAccountService
{
    Task<BrokerAccountDto> CreateAccountAsync(JsonElement requestBody, CancellationToken ct);

    Task<BrokerAccountDto> GetAccountAsync(string accountId, CancellationToken ct);

    Task<IEnumerable<BrokerPositionDto>> GetPositionsAsync(CancellationToken ct);

    Task<IEnumerable<BrokerOrderDto>> GetOrdersAsync(CancellationToken ct);
}
