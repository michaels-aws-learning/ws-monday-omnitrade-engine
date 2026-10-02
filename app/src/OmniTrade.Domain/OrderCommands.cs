namespace OmniTrade.Domain;

public interface IOrderCommand { }

public sealed record SubmitParentOrder(
    string ParentOrderId,
    string Symbol,
    decimal Quantity,
    decimal? LimitPrice = null) : IOrderCommand;

public sealed record BrokerExecutionReportCommand(
    string ClientOrderId,
    decimal CumulativeFilledQuantity,
    decimal? AverageFillPrice) : IOrderCommand;
