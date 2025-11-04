namespace QuickBooksDemo.Models.DTOs
{
    public class OAuthResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string TokenType { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
        public string CompanyId { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public string ErrorDescription { get; set; } = string.Empty;
        public bool IsSuccess => string.IsNullOrEmpty(Error);
    }
}