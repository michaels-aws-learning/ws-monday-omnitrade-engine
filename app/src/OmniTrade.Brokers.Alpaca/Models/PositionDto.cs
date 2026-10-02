namespace OmniTrade.Brokers.Alpaca.Models;

public sealed record PositionDto(
    string Symbol,
    decimal Quantity,
    decimal MarketValue,
    decimal AverageEntryPrice
);
