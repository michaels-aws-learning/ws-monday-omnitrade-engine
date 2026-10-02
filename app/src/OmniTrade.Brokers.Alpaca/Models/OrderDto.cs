namespace OmniTrade.Brokers.Alpaca.Models;

public sealed record OrderDto(
    string Id,
    string ClientOrderId,
    string Symbol,
    string Side,
    string Type,
    decimal Quantity,
    string Status
);
