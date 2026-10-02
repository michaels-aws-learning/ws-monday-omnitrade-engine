namespace OmniTrade.Domain;

public interface IOrderEvent { }

public sealed record ParentOrderAccepted(string ParentOrderId, string Symbol, decimal Quantity, decimal? LimitPrice) : IOrderEvent;

public sealed record ChildOrderRequested(string ParentOrderId, string ChildOrderId, string Symbol, decimal Quantity, decimal? LimitPrice) : IOrderEvent;

public sealed record ChildOrderAcknowledged(string ChildOrderId, string BrokerOrderId) : IOrderEvent;

public sealed record FillReceived(string ChildOrderId, decimal FilledQuantity, decimal? FillPrice, DateTimeOffset Timestamp) : IOrderEvent;

public sealed record ParentOrderCompleted(string ParentOrderId) : IOrderEvent;
