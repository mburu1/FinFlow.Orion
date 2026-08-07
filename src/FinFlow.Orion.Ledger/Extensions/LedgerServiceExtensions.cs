using FinFlow.Orion.Ledger.Abstractions;
using FinFlow.Orion.Ledger.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection; // for TryAddScoped
using System;
using System.Collections.Generic;
using System.Text;

namespace FinFlow.Orion.Ledger.Extensions;

public static class LedgerServiceExtensions
{
    /// <summary>
    /// Registers Ledger domain services.
    /// Note: IJournalEntryRepository and ILedgerRepository must be registered
    /// by the Infrastructure layer (AddInfrastructure) which provides the
    /// concrete EF Core implementations.
    /// </summary>
    public static IServiceCollection AddLedger(this IServiceCollection services)
    {
        // Only register if not already registered by Infrastructure
        // This prevents double registration when both AddInfrastructure
        // and AddLedger are called together
        services.TryAddScoped<ILedgerService, LedgerService>();

        return services;
    }
}