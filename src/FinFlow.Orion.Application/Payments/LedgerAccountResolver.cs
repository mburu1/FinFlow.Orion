using FinFlow.Orion.Domain.Enums;
using FinFlow.Orion.Ledger.Configurations;

namespace FinFlow.Orion.Application.Payments;

/// <summary>
/// Maps a payment channel to the ledger account pair that should be debited/credited
/// when funds move. Funds received on behalf of a customer increase the relevant
/// settlement/float asset account and increase what we owe the customer (liability).
/// </summary>
public static class LedgerAccountResolver
{
    public static (string DebitAccountCode, string CreditAccountCode) ResolveForProvider(PaymentProvider provider)
        => provider switch
        {
            PaymentProvider.MPesa => (LedgerAccountSeeds.Codes.MpesaFloat, LedgerAccountSeeds.Codes.CustomerPayable),
            PaymentProvider.Card => (LedgerAccountSeeds.Codes.CardSettlement, LedgerAccountSeeds.Codes.CustomerPayable),
            PaymentProvider.BankTransfer => (LedgerAccountSeeds.Codes.BankSettlement, LedgerAccountSeeds.Codes.CustomerPayable),
            _ => throw new NotSupportedException($"No ledger account mapping is defined for provider '{provider}'.")
        };
}
