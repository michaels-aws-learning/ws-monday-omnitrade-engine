using Microsoft.AspNetCore.Mvc;
using OmniTrade.Domain;
using OmniTrade.Engine.Services;

namespace OmniTrade.Engine.Controllers.Broker;

[ApiController]
[Route("api/broker/orders")]
public class OrdersController : ControllerBase
{
    private readonly OrderManager _orderManager;

    public OrdersController(OrderManager orderManager)
    {
        _orderManager = orderManager;
    }

    [HttpPost]
    public async Task<IActionResult> SubmitParent([FromBody] SubmitParentOrder cmd, CancellationToken ct)
    {
        // post command into the engine and return accepted
        await _orderManager.Writer.WriteAsync(cmd, ct);
        return Accepted(new { cmd.ParentOrderId });
    }
}
