using FinFlow.Orion.Domain.Entities.Ledger;
using FinFlow.Orion.Domain.Enums;

namespace FinFlow.Orion.Ledger.Configurations;

/// <summary>
/// Seed data for the chart of accounts.
/// Codes follow the pattern: {Type}-{Provider}-{Purpose}
/// </summary>
public static class LedgerAccountSeeds
{
    public static IReadOnlyList<LedgerAccount> GetSeedAccounts() =>
    [
        // ── Assets ──────────────────────────────────────────────────────────
        LedgerAccount.Create(
            code: "1001-MPESA-FLOAT",
            name: "M-Pesa Float Account",
            accountType: LedgerAccountType.Asset,
            currencyCode: "KES"),

        LedgerAccount.Create(
            code: "1002-CARD-SETTLE",
            name: "Card Settlement Account",
            accountType: LedgerAccountType.Asset,
            currencyCode: "KES"),

        LedgerAccount.Create(
            code: "1003-BANK-SETTLE",
            name: "Bank Transfer Settlement Account",
            accountType: LedgerAccountType.Asset,
            currencyCode: "KES"),

        LedgerAccount.Create(
            code: "1004-RECEIVABLE",
            name: "Payments Receivable",
            accountType: LedgerAccountType.Asset,
            currencyCode: "KES"),

        // ── Liabilities ──────────────────────────────────────────────────────
        LedgerAccount.Create(
            code: "2001-CUSTOMER-PAYABLE",
            name: "Customer Funds Payable",
            accountType: LedgerAccountType.Liability,
            currencyCode: "KES"),

        LedgerAccount.Create(
            code: "2002-REVERSAL-PAYABLE",
            name: "Reversal Payable",
            accountType: LedgerAccountType.Liability,
            currencyCode: "KES"),

        // ── Revenue ──────────────────────────────────────────────────────────
        LedgerAccount.Create(
            code: "4001-TRANSACTION-FEE",
            name: "Transaction Fee Revenue",
            accountType: LedgerAccountType.Revenue,
            currencyCode: "KES"),

        // ── Expenses ─────────────────────────────────────────────────────────
        LedgerAccount.Create(
            code: "5001-PROVIDER-CHARGES",
            name: "Payment Provider Charges",
            accountType: LedgerAccountType.Expense,
            currencyCode: "KES"),

        LedgerAccount.Create(
            code: "5002-REVERSAL-COST",
            name: "Reversal Processing Cost",
            accountType: LedgerAccountType.Expense,
            currencyCode: "KES")
    ];

    // Well-known account code constants for use in LedgerService calls
    public static class Codes
    {
        public const string MpesaFloat = "1001-MPESA-FLOAT";
        public const string CardSettlement = "1002-CARD-SETTLE";
        public const string BankSettlement = "1003-BANK-SETTLE";
        public const string Receivable = "1004-RECEIVABLE";
        public const string CustomerPayable = "2001-CUSTOMER-PAYABLE";
        public const string ReversalPayable = "2002-REVERSAL-PAYABLE";
        public const string TransactionFee = "4001-TRANSACTION-FEE";
        public const string ProviderCharges = "5001-PROVIDER-CHARGES";
        public const string ReversalCost = "5002-REVERSAL-COST";
    }
}