using Microsoft.AspNetCore.Mvc;
using OmniTrade.Brokers.Alpaca;
using System.Text.Json;
using OmniTrade.Domain;

namespace OmniTrade.Engine.Controllers.Broker;

[ApiController]
[Route("api/broker/accounts")]
public class AccountsController : ControllerBase
{
    private readonly IAlpacaAccountService _alpacaAccountService;

    public AccountsController(IAlpacaAccountService alpacaAccountService)
    {
        _alpacaAccountService = alpacaAccountService;
    }

    [HttpGet("{accountId}")]
    public async Task<IActionResult> GetAccount([FromRoute] string accountId, CancellationToken ct)
    {
        var account = await _alpacaAccountService.GetAccountAsync(accountId, ct);
        return Ok(account);
    }

    [HttpGet("positions")]
    public async Task<IActionResult> GetPositions(CancellationToken ct)
    {
        var positions = await _alpacaAccountService.GetPositionsAsync(ct);
        return Ok(positions);
    }

    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders(CancellationToken ct)
    {
        var orders = await _alpacaAccountService.GetOrdersAsync(ct);
        return Ok(orders);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAccount([FromBody] JsonElement body, CancellationToken ct)
    {
        var res = await _alpacaAccountService.CreateAccountAsync(body, ct);
        return Ok(res);
    }
}
