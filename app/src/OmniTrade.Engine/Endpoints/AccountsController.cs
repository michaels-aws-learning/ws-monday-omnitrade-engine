using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace OmniTrade.Engine.Endpoints
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountsController : ControllerBase
    {
        public AccountsController()
        {

        }

        [HttpGet("{id}")]
        public IActionResult GetAccount(string id)
        {
            // Implementation for getting a specific account
            return Ok($"Account details for ID: {id}");
        }
    }
}
