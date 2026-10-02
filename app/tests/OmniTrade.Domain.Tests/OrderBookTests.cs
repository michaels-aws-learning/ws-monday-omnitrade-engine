using OmniTrade.Domain;

namespace OmniTrade.Domain.Tests;

public class OrderBookTests
{
    [Fact]
    public void SubmitParentEmitsAcceptedAndChildRequested()
    {
        var book = new OrderBook();

        var cmd = new SubmitParentOrder("p1", "AAPL", 100m, null);

        var events = book.Handle(cmd).ToList();

        Assert.Contains(events, e => e is ParentOrderAccepted pa && pa.ParentOrderId == "p1");
        Assert.Contains(events, e => e is ChildOrderRequested cr && cr.ParentOrderId == "p1");
    }

    [Fact]
    public void ExecutionReportProducesFillAndParentCompleted()
    {
        var book = new OrderBook();
        var submit = new SubmitParentOrder("p2", "MSFT", 50m, null);
        var events = book.Handle(submit).ToList();

        // child id is deterministic p2-1
        var exec = new BrokerExecutionReportCommand("p2-1", 50m, 200m);
        var outEvents = book.Handle(exec).ToList();

        Assert.Contains(outEvents, e => e is FillReceived fr && fr.ChildOrderId == "p2-1");
        Assert.Contains(outEvents, e => e is ParentOrderCompleted poc && poc.ParentOrderId == "p2");
    }
}
