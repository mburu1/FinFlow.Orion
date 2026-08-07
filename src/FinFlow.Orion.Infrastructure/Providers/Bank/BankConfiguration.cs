namespace FinFlow.Orion.Infrastructure.Providers.Bank
{
    public class BankConfiguration
    {
        public const string SectionName = "Bank";
        public string BaseUrl { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; } = 30;
        public string ApiKey { get; set; } = string.Empty;
    }
}