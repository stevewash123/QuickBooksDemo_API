using Microsoft.Extensions.Options;
using QuickBooksDemo.Models.Configuration;
using QuickBooksDemo.Models.DTOs;
using QuickBooksDemo.Service.Interfaces;
using System.Text;
using System.Text.Json;
using System.Web;

namespace QuickBooksDemo.Service.Services
{
    public class QuickBooksOAuthService : IQuickBooksOAuthService
    {
        private readonly QuickBooksConfig _config;
        private readonly HttpClient _httpClient;

        public QuickBooksOAuthService(IOptions<QuickBooksConfig> config, HttpClient httpClient)
        {
            _config = config.Value;
            _httpClient = httpClient;
        }

        public string GetAuthorizationUrl()
        {
            var state = Guid.NewGuid().ToString();
            var queryParams = HttpUtility.ParseQueryString(string.Empty);

            queryParams["client_id"] = _config.ClientId;
            queryParams["scope"] = _config.Scope;
            queryParams["redirect_uri"] = _config.RedirectUri;
            queryParams["response_type"] = "code";
            queryParams["access_type"] = "offline";
            queryParams["state"] = state;

            return $"https://appcenter.intuit.com/connect/oauth2?{queryParams}";
        }

        public async Task<OAuthResponse> ExchangeCodeForTokensAsync(string code, string state)
        {
            try
            {
                var tokenUrl = "https://oauth.platform.intuit.com/oauth2/v1/tokens/bearer";

                var formData = new List<KeyValuePair<string, string>>
                {
                    new("grant_type", "authorization_code"),
                    new("code", code),
                    new("redirect_uri", _config.RedirectUri)
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

                    return new OAuthResponse
                    {
                        AccessToken = tokenResponse.GetProperty("access_token").GetString() ?? "",
                        RefreshToken = tokenResponse.GetProperty("refresh_token").GetString() ?? "",
                        TokenType = tokenResponse.GetProperty("token_type").GetString() ?? "",
                        ExpiresIn = tokenResponse.GetProperty("expires_in").GetInt32(),
                        CompanyId = state // Will be set by the controller
                    };
                }
                else
                {
                    var errorResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    return new OAuthResponse
                    {
                        Error = errorResponse.TryGetProperty("error", out var error) ? error.GetString() ?? "Unknown error" : "Unknown error",
                        ErrorDescription = errorResponse.TryGetProperty("error_description", out var desc) ? desc.GetString() ?? "" : ""
                    };
                }
            }
            catch (Exception ex)
            {
                return new OAuthResponse
                {
                    Error = "Exception",
                    ErrorDescription = ex.Message
                };
            }
        }

        public async Task<OAuthResponse> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                var tokenUrl = "https://oauth.platform.intuit.com/oauth2/v1/tokens/bearer";

                var formData = new List<KeyValuePair<string, string>>
                {
                    new("grant_type", "refresh_token"),
                    new("refresh_token", refreshToken)
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

                    return new OAuthResponse
                    {
                        AccessToken = tokenResponse.GetProperty("access_token").GetString() ?? "",
                        RefreshToken = tokenResponse.TryGetProperty("refresh_token", out var rt) ? rt.GetString() ?? refreshToken : refreshToken,
                        TokenType = tokenResponse.GetProperty("token_type").GetString() ?? "",
                        ExpiresIn = tokenResponse.GetProperty("expires_in").GetInt32()
                    };
                }
                else
                {
                    var errorResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    return new OAuthResponse
                    {
                        Error = errorResponse.TryGetProperty("error", out var error) ? error.GetString() ?? "Unknown error" : "Unknown error",
                        ErrorDescription = errorResponse.TryGetProperty("error_description", out var desc) ? desc.GetString() ?? "" : ""
                    };
                }
            }
            catch (Exception ex)
            {
                return new OAuthResponse
                {
                    Error = "Exception",
                    ErrorDescription = ex.Message
                };
            }
        }

        public async Task<bool> ValidateTokenAsync(string accessToken)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "https://sandbox-quickbooks.api.intuit.com/v3/company/companyid/companyinfo/companyid");
                request.Headers.Add("Authorization", $"Bearer {accessToken}");
                request.Headers.Add("Accept", "application/json");

                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}