namespace OmniTrade.Brokers.Alpaca.Models;

public sealed record AccountDto(
    string Id,
    string AccountNumber,
    string Status,
    decimal Cash,
    decimal PortfolioValue
);
