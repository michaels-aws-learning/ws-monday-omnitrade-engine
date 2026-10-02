namespace OmniTrade.Domain;

public enum ChildOrderState
{
    PendingNew,
    New,
    PartiallyFilled,
    Filled,
    PendingCancel,
    Canceled,
    Rejected,
    Expired
}

public enum ParentOrderState
{
    New,
    Working,
    Paused,
    PendingCancel,
    Completed,
    Canceled,
    Rejected
}

public sealed record ChildOrder(
    string ChildOrderId,
    string ParentOrderId,
    string Symbol,
    decimal Quantity,
    decimal Filled,
    ChildOrderState State
);

public sealed record ParentOrder(
    string ParentOrderId,
    string Symbol,
    decimal Quantity,
    decimal Filled,
    ParentOrderState State
);
