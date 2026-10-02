using Microsoft.Extensions.DependencyInjection;
using OmniTrade.ApiHandler;
using OmniTrade.Domain;

namespace OmniTrade.Brokers.Alpaca;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAlpacaBroker(this IServiceCollection services)
    {
        // Register generic ApiHandler instances for the DTOs used by the client
        services.AddSingleton<IApiHandler<OmniTrade.Brokers.Alpaca.Models.AccountDto, object>, ApiHandler<OmniTrade.Brokers.Alpaca.Models.AccountDto, object>>();
        services.AddSingleton<IApiHandler<IEnumerable<OmniTrade.Brokers.Alpaca.Models.PositionDto>, object>, ApiHandler<IEnumerable<OmniTrade.Brokers.Alpaca.Models.PositionDto>, object>>();
        services.AddSingleton<IApiHandler<IEnumerable<OmniTrade.Brokers.Alpaca.Models.OrderDto>, object>, ApiHandler<IEnumerable<OmniTrade.Brokers.Alpaca.Models.OrderDto>, object>>();

        services.AddSingleton<IAlpacaBrokerClient, AlpacaBrokerClient>();
        // Account service: implement IBrokerAccountService using Alpaca
        services.AddSingleton<IBrokerAccountService, AlpacaAccountService>();
        services.AddSingleton<IAlpacaAccountService>(sp => (IAlpacaAccountService)sp.GetRequiredService<IBrokerAccountService>()!);

        return services;
    }
}
