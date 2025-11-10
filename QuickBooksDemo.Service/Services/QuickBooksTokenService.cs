using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
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
        private readonly ILogger<QuickBooksTokenService> _logger;
        private readonly IConfiguration _configuration;
        private readonly object _lockObject = new object();

        public QuickBooksTokenService(
            IOptions<QuickBooksConfig> config,
            HttpClient httpClient,
            IEmailService emailService,
            ILogger<QuickBooksTokenService> logger,
            IConfiguration configuration)
        {
            _config = config.Value;
            _httpClient = httpClient;
            _emailService = emailService;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<string> GetValidAccessTokenAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_config.AccessToken))
                {
                    throw new Exception("No QuickBooks access token configured. Please re-authenticate.");
                }

                // Check if access token is still valid (with 5 minute buffer)
                if (DateTime.UtcNow < _config.AccessTokenExpiry.AddMinutes(-5))
                {
                    _logger.LogDebug("Using valid access token from config");
                    return _config.AccessToken;
                }

                // Access token expired, try to refresh
                _logger.LogInformation("Access token expired, attempting refresh");
                return await RefreshAccessTokenAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting valid access token");
                throw;
            }
        }

        public async Task<bool> IsConnectionValidAsync()
        {
            try
            {
                // Try to get a valid access token from database
                var accessToken = await GetValidAccessTokenAsync();
                return !string.IsNullOrEmpty(accessToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "QuickBooks connection validation failed");
                return false;
            }
        }

        private async Task<string> RefreshAccessTokenAsync()
        {
            try
            {
                _logger.LogInformation("Attempting to refresh QuickBooks access token");

                if (string.IsNullOrEmpty(_config.RefreshToken))
                {
                    throw new Exception("No refresh token available. Re-authentication required.");
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
                    var newAccessTokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn);

                    // Check if we got a new refresh token (QuickBooks provides new refresh tokens)
                    var newRefreshToken = _config.RefreshToken; // Default to existing

                    if (tokenResponse.TryGetProperty("refresh_token", out var refreshTokenElement))
                    {
                        newRefreshToken = refreshTokenElement.GetString() ?? _config.RefreshToken;
                    }

                    // Update config file with new tokens
                    await UpdateConfigFileAsync(newAccessToken, newAccessTokenExpiry, newRefreshToken);

                    _logger.LogInformation("Successfully refreshed QuickBooks access token");
                    return newAccessToken;
                }
                else
                {
                    var errorMessage = $"Token refresh failed: {response.StatusCode} - {responseContent}";
                    _logger.LogError(errorMessage);
                    throw new Exception(errorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh QuickBooks access token");

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
                        catch (Exception emailEx)
                        {
                            _logger.LogError(emailEx, "Failed to send token error email notification");
                        }
                    });
                }

                throw new Exception($"Failed to refresh access token: {ex.Message}", ex);
            }
        }

        private async Task UpdateConfigFileAsync(string newAccessToken, DateTime newAccessTokenExpiry, string newRefreshToken)
        {
            try
            {
                lock (_lockObject)
                {
                    // Update the config object
                    _config.AccessToken = newAccessToken;
                    _config.AccessTokenExpiry = newAccessTokenExpiry;
                    _config.RefreshToken = newRefreshToken;
                }

                // Write the updated tokens to the config file
                var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
                var configFileName = environment == "Development" ? "appsettings.Development.json" : "appsettings.json";
                var configPath = Path.Combine(AppContext.BaseDirectory, configFileName);

                if (!File.Exists(configPath))
                {
                    // Try looking in the API project directory
                    var apiProjectPath = Path.Combine(Directory.GetCurrentDirectory(), configFileName);
                    if (File.Exists(apiProjectPath))
                    {
                        configPath = apiProjectPath;
                    }
                    else
                    {
                        _logger.LogWarning($"Config file not found at {configPath} or {apiProjectPath}");
                        return;
                    }
                }

                var configJson = await File.ReadAllTextAsync(configPath);
                var configDocument = JsonDocument.Parse(configJson);
                var configObject = new Dictionary<string, object>();

                // Convert the existing config to a dictionary for manipulation
                foreach (var property in configDocument.RootElement.EnumerateObject())
                {
                    configObject[property.Name] = JsonElementToObject(property.Value);
                }

                // Update the QuickBooks section
                if (configObject.ContainsKey("QuickBooks") && configObject["QuickBooks"] is Dictionary<string, object> qbSection)
                {
                    qbSection["AccessToken"] = newAccessToken;
                    qbSection["AccessTokenExpiry"] = newAccessTokenExpiry.ToString("yyyy-MM-ddTHH:mm:ssZ");
                    qbSection["RefreshToken"] = newRefreshToken;
                }

                // Write back to file
                var updatedJson = JsonSerializer.Serialize(configObject, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(configPath, updatedJson);

                _logger.LogInformation("Successfully updated tokens in config file");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update tokens in config file");
                throw;
            }
        }

        private static object JsonElementToObject(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString()!,
                JsonValueKind.Number => element.TryGetInt32(out var intValue) ? intValue : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null!,
                JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => JsonElementToObject(p.Value)),
                JsonValueKind.Array => element.EnumerateArray().Select(JsonElementToObject).ToArray(),
                _ => element.ToString()!
            };
        }
    }
}