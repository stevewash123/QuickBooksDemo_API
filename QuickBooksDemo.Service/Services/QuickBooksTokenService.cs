using Microsoft.Extensions.Options;
using QuickBooksDemo.Models.Configuration;
using QuickBooksDemo.Service.Interfaces;
using System.Text;
using System.Text.Json;

namespace QuickBooksDemo.Service.Services
{
    public class QuickBooksTokenService : IQuickBooksTokenService
    {
        private readonly QuickBooksConfig _config;
        private readonly HttpClient _httpClient;
        private readonly IEmailService _emailService;
        private readonly object _lockObject = new object();
        private static string _cachedAccessToken = string.Empty;
        private static DateTime _cachedTokenExpiry = DateTime.MinValue;

        public QuickBooksTokenService(IOptions<QuickBooksConfig> config, HttpClient httpClient, IEmailService emailService)
        {
            _config = config.Value;
            _httpClient = httpClient;
            _emailService = emailService;
        }

        public async Task<string> GetValidAccessTokenAsync()
        {
            // For demo purposes, if we have demo values, return them directly
            if (_config.AccessToken.Contains("demo") || _config.RefreshToken.Contains("demo"))
            {
                return _config.AccessToken;
            }

            lock (_lockObject)
            {
                // Check if we have a valid cached token
                if (!string.IsNullOrEmpty(_cachedAccessToken) && DateTime.UtcNow < _cachedTokenExpiry.AddMinutes(-5))
                {
                    return _cachedAccessToken;
                }
            }

            // Check if config has a still-valid token
            if (!string.IsNullOrEmpty(_config.AccessToken) && DateTime.UtcNow < _config.AccessTokenExpiry.AddMinutes(-5))
            {
                lock (_lockObject)
                {
                    _cachedAccessToken = _config.AccessToken;
                    _cachedTokenExpiry = _config.AccessTokenExpiry;
                }
                return _config.AccessToken;
            }

            // Need to refresh the token
            return await RefreshAccessTokenAsync();
        }

        public async Task<bool> IsConnectionValidAsync()
        {
            try
            {
                // Check if we have required configuration
                if (string.IsNullOrEmpty(_config.RefreshToken) || string.IsNullOrEmpty(_config.RealmId))
                {
                    return false;
                }

                // For demo purposes, if we have demo/placeholder values, consider it valid
                if (_config.RefreshToken.Contains("demo") || _config.RealmId.Contains("demo"))
                {
                    return true;
                }

                // Try to get a valid access token
                var accessToken = await GetValidAccessTokenAsync();
                return !string.IsNullOrEmpty(accessToken);
            }
            catch
            {
                return false;
            }
        }

        private async Task<string> RefreshAccessTokenAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_config.RefreshToken))
                {
                    throw new Exception("No refresh token available");
                }

                var tokenUrl = "https://oauth.platform.intuit.com/oauth2/v1/tokens/bearer";

                var formData = new List<KeyValuePair<string, string>>
                {
                    new("grant_type", "refresh_token"),
                    new("refresh_token", _config.RefreshToken)
                };

                var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl)
                {
                    Content = new FormUrlEncodedContent(formData)
                };

                var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.ClientId}:{_config.ClientSecret}"));
                request.Headers.Add("Authorization", $"Basic {authValue}");
                request.Headers.Add("Accept", "application/json");

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var tokenResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    var newAccessToken = tokenResponse.GetProperty("access_token").GetString() ?? "";
                    var expiresIn = tokenResponse.GetProperty("expires_in").GetInt32();
                    var newExpiry = DateTime.UtcNow.AddSeconds(expiresIn);

                    // Update cached token
                    lock (_lockObject)
                    {
                        _cachedAccessToken = newAccessToken;
                        _cachedTokenExpiry = newExpiry;
                    }

                    // Update config values (in-memory only)
                    _config.AccessToken = newAccessToken;
                    _config.AccessTokenExpiry = newExpiry;

                    return newAccessToken;
                }
                else
                {
                    throw new Exception($"Token refresh failed: {responseContent}");
                }
            }
            catch (Exception ex)
            {
                // Check if this is specifically a token refresh failure (invalid_grant error)
                if (ex.Message.Contains("invalid_grant") || ex.Message.Contains("Incorrect or invalid refresh token"))
                {
                    // Send email notification for token refresh failures
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _emailService.SendQuickBooksTokenErrorAsync(ex.Message, ex);
                        }
                        catch
                        {
                            // Don't let email failures prevent the main exception from being thrown
                        }
                    });
                }

                throw new Exception($"Failed to refresh access token: {ex.Message}", ex);
            }
        }
    }
}