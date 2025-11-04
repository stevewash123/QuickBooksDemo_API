using QuickBooksDemo.Models.DTOs;

namespace QuickBooksDemo.Service.Interfaces
{
    public interface IQuickBooksOAuthService
    {
        string GetAuthorizationUrl();
        Task<OAuthResponse> ExchangeCodeForTokensAsync(string code, string state);
        Task<OAuthResponse> RefreshTokenAsync(string refreshToken);
        Task<bool> ValidateTokenAsync(string accessToken);
    }
}