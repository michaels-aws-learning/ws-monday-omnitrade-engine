using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OmniTrade.Domain;

namespace OmniTrade.Brokers.Alpaca;

public interface IAlpacaAccountService : IBrokerAccountService
{
    // No-op update to ensure file sync after domain changes.
    // specialized signature convenience overload
    Task<BrokerAccountDto> GetAccountAsync(string accountId, CancellationToken ct);
}
