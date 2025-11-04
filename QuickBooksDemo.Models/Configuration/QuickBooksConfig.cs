namespace QuickBooksDemo.Models.Configuration
{
    public class QuickBooksConfig
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string Environment { get; set; } = "Development";
        public string BaseUrl { get; set; } = string.Empty;
        public string DiscoveryDocumentUrl { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = "http://localhost:5042/api/quickbooks/callback";
        public string Scope { get; set; } = "com.intuit.quickbooks.accounting";
        public string RefreshToken { get; set; } = string.Empty;
        public string RealmId { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public DateTime AccessTokenExpiry { get; set; } = DateTime.MinValue;
    }
}