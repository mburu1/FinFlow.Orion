using FinFlow.Orion.Ledger.Abstractions;
using FinFlow.Orion.Ledger.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FinFlow.Orion.Ledger;

public static class DependencyInjection
{
    public static IServiceCollection AddLedger(this IServiceCollection services)
    {
        services.AddScoped<ILedgerService, LedgerService>();

        // ILedgerRepository and IJournalEntryRepository are registered
        // in Infrastructure where the EF Core implementations live
        return services;
    }
}