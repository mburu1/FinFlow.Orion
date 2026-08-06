namespace FinFlow.Orion.Infrastructure.Providers.MPesa;

public sealed class MpesaConfiguration
{
    public const string SectionName = "Providers:MPesa";

    public string BaseUrl { get; init; } = string.Empty;
    public string ConsumerKey { get; init; } = string.Empty;
    public string ConsumerSecret { get; init; } = string.Empty;
    public string BusinessShortCode { get; init; } = string.Empty;
    public string PassKey { get; init; } = string.Empty;
    public string CallbackUrl { get; init; } = string.Empty;
    public string AccountReference { get; init; } = "FinFlowOrion";
    public string TransactionDesc { get; init; } = "Payment";
    public int TimeoutSeconds { get; init; } = 30;
    public int MaxRetries { get; init; } = 3;
}