using OmniTrade.Domain;

namespace OmniTrade.Domain.Tests;

public class OrderRequestTests
{
    [Fact]
    public void Limit_order_keeps_its_price()
    {
        var order = new OrderRequest("c-1", "AAPL", OrderSide.Buy, OrderType.Limit, 100m, 190.25m);

        Assert.Equal(190.25m, order.LimitPrice);
    }
}