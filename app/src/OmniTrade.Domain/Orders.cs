namespace OmniTrade.Domain;

public enum OrderSide { Buy, Sell }

public enum OrderType { Market, Limit }

public enum OrderStatus
{
    New,
    PendingNew,
    PartiallyFilled,
    Filled,
    PendingCancel,
    Canceled,
    Rejected
}

/// <summary>A child order sent to a broker. ClientOrderId links fills back to the parent.</summary>
public sealed record OrderRequest(
    string ClientOrderId,
    string Symbol,
    OrderSide Side,
    OrderType Type,
    decimal Quantity,
    decimal? LimitPrice = null);

/// <summary>A broker-neutral execution report (fill, partial fill, cancel, reject).</summary>
public sealed record ExecutionReport(
    string ClientOrderId,
    string? BrokerOrderId,
    OrderStatus Status,
    decimal FilledQuantity,
    decimal? AverageFillPrice,
    DateTimeOffset Timestamp);