namespace QuickBooksDemo.Models.DTOs
{
    public class QuickBooksAuthResult
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string CompanyId { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
        public bool IsSuccess { get; set; }
        public string Error { get; set; } = string.Empty;
        public string ErrorDescription { get; set; } = string.Empty;
    }
}