using System.Collections.Concurrent;

namespace OmniTrade.Domain;

public sealed class OrderBook : IOrderBook
{
    // In-memory state for deterministic id generation and quick lookups
    private readonly ConcurrentDictionary<string, ParentOrder> _parents = new();
    private readonly ConcurrentDictionary<string, ChildOrder> _children = new();
    private readonly ConcurrentDictionary<string, int> _parentChildSeq = new();

    public IEnumerable<IOrderEvent> Handle(IOrderCommand command)
    {
        return command switch
        {
            SubmitParentOrder s => HandleSubmitParent(s),
            BrokerExecutionReportCommand b => HandleExecutionReport(b),
            _ => Array.Empty<IOrderEvent>()
        };
    }

    private IEnumerable<IOrderEvent> HandleSubmitParent(SubmitParentOrder cmd)
    {
        var events = new List<IOrderEvent>();

        if (_parents.ContainsKey(cmd.ParentOrderId))
        {
            // duplicate submission ignored
            return events;
        }

        var parent = new ParentOrder(cmd.ParentOrderId, cmd.Symbol, cmd.Quantity, 0m, ParentOrderState.New);
        _parents[cmd.ParentOrderId] = parent;

        events.Add(new ParentOrderAccepted(cmd.ParentOrderId, cmd.Symbol, cmd.Quantity, cmd.LimitPrice));

        // As a first simple algo: immediately request one child equal to full quantity
        var childSeq = _parentChildSeq.AddOrUpdate(cmd.ParentOrderId, 1, (_, old) => old + 1);
        var childId = $"{cmd.ParentOrderId}-{childSeq}";

        var child = new ChildOrder(childId, cmd.ParentOrderId, cmd.Symbol, cmd.Quantity, 0m, ChildOrderState.PendingNew);
        _children[childId] = child;

        events.Add(new ChildOrderRequested(cmd.ParentOrderId, childId, cmd.Symbol, cmd.Quantity, cmd.LimitPrice));

        return events;
    }

    private IEnumerable<IOrderEvent> HandleExecutionReport(BrokerExecutionReportCommand cmd)
    {
        var events = new List<IOrderEvent>();

        if (!_children.TryGetValue(cmd.ClientOrderId, out var child))
            return events; // unknown child

        var newFilled = cmd.CumulativeFilledQuantity;
        var delta = newFilled - child.Filled;
        if (delta <= 0)
            return events; // nothing new

        // update child
        var updatedChild = child with { Filled = newFilled, State = newFilled >= child.Quantity ? ChildOrderState.Filled : ChildOrderState.PartiallyFilled };
        _children[cmd.ClientOrderId] = updatedChild;

        events.Add(new FillReceived(cmd.ClientOrderId, delta, cmd.AverageFillPrice, DateTimeOffset.UtcNow));

        // update parent filled
        if (_parents.TryGetValue(child.ParentOrderId, out var parent))
        {
            var newParentFilled = parent.Filled + delta;
            var updatedParent = parent with { Filled = newParentFilled, State = newParentFilled >= parent.Quantity ? ParentOrderState.Completed : ParentOrderState.Working };
            _parents[child.ParentOrderId] = updatedParent;

            if (updatedParent.State == ParentOrderState.Completed)
                events.Add(new ParentOrderCompleted(parent.ParentOrderId));
        }

        return events;
    }
}
