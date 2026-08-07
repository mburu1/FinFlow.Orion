namespace FinFlow.Orion.Infrastructure.Providers.Card
{
    public class CardConfiguration
    {
        public const string SectionName = "Card";
        public string BaseUrl { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; } = 30;
        public string ApiKey { get; set; } = string.Empty;
    }
}