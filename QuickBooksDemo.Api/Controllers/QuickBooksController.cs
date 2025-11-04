using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using QuickBooksDemo.Models.Configuration;
using QuickBooksDemo.Models.DTOs;
using QuickBooksDemo.Service.Interfaces;

namespace QuickBooksDemo.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuickBooksController : ControllerBase
    {
        private readonly IQuickBooksIntegrationService _quickBooksService;
        private readonly IJobService _jobService;
        private readonly IQuickBooksApiService _quickBooksApiService;
        private readonly IQuickBooksTokenService _tokenService;
        private readonly QuickBooksConfig _config;

        public QuickBooksController(
            IQuickBooksIntegrationService quickBooksService,
            IJobService jobService,
            IQuickBooksApiService quickBooksApiService,
            IQuickBooksTokenService tokenService,
            IOptions<QuickBooksConfig> config)
        {
            _quickBooksService = quickBooksService;
            _jobService = jobService;
            _quickBooksApiService = quickBooksApiService;
            _tokenService = tokenService;
            _config = config.Value;
        }

        [HttpPost("send-job/{jobId}")]
        public async Task<IActionResult> SendJobToQuickBooks(string jobId, [FromBody] SendJobRequest request)
        {
            try
            {
                var quickBooksId = await _quickBooksService.SendJobToQuickBooksAsync(jobId, request.AsInvoice);
                return Ok(new { quickBooksId, type = request.AsInvoice ? "invoice" : "estimate" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("test-connection")]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                var isConnected = await _quickBooksService.TestConnectionAsync();
                return Ok(new { isConnected });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("debug-token")]
        public async Task<IActionResult> DebugToken()
        {
            try
            {
                var accessToken = await _tokenService.GetValidAccessTokenAsync();
                var isValid = !string.IsNullOrEmpty(accessToken);

                return Ok(new
                {
                    hasToken = isValid,
                    tokenLength = accessToken?.Length ?? 0,
                    realmId = _config.RealmId,
                    currentTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    configExpiry = _config.AccessTokenExpiry.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    isExpired = DateTime.UtcNow >= _config.AccessTokenExpiry.AddMinutes(-5)
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("debug-raw-invoices")]
        public async Task<IActionResult> DebugRawInvoices()
        {
            try
            {
                var accessToken = await _tokenService.GetValidAccessTokenAsync();
                if (string.IsNullOrEmpty(accessToken))
                {
                    return BadRequest(new { error = "No access token" });
                }

                // Make raw QB API call to see what's actually returned
                var query = Uri.EscapeDataString("SELECT * FROM Invoice ORDER BY TxnDate DESC");
                var url = $"{_config.BaseUrl}/v3/company/{_config.RealmId}/query?query={query}";

                var httpClient = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", $"Bearer {accessToken}");
                request.Headers.Add("Accept", "application/json");

                var response = await httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                return Ok(new
                {
                    success = response.IsSuccessStatusCode,
                    statusCode = (int)response.StatusCode,
                    url = url,
                    rawResponse = content,
                    contentLength = content.Length
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }


        [HttpGet("connect")]
        public IActionResult Connect()
        {
            try
            {
                var state = Guid.NewGuid().ToString();
                var authUrl = $"https://appcenter.intuit.com/connect/oauth2?" +
                    $"client_id={_config.ClientId}" +
                    $"&scope={Uri.EscapeDataString(_config.Scope)}" +
                    $"&redirect_uri={Uri.EscapeDataString(_config.RedirectUri)}" +
                    $"&response_type=code" +
                    $"&access_type=offline" +
                    $"&state={state}";

                return Redirect(authUrl);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("callback")]
        public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string realmId, [FromQuery] string state)
        {
            try
            {
                if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(realmId))
                {
                    return BadRequest(new { error = "Missing authorization code or realm ID" });
                }

                // Exchange code for tokens
                var tokenUrl = "https://oauth.platform.intuit.com/oauth2/v1/tokens/bearer";
                var formData = new List<KeyValuePair<string, string>>
                {
                    new("grant_type", "authorization_code"),
                    new("code", code),
                    new("redirect_uri", _config.RedirectUri)
                };

                var httpClient = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl)
                {
                    Content = new FormUrlEncodedContent(formData)
                };

                var authValue = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{_config.ClientId}:{_config.ClientSecret}"));
                request.Headers.Add("Authorization", $"Basic {authValue}");
                request.Headers.Add("Accept", "application/json");

                var response = await httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var tokenResponse = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(responseContent);

                    var accessToken = tokenResponse.GetProperty("access_token").GetString();
                    var refreshToken = tokenResponse.GetProperty("refresh_token").GetString();
                    var expiresIn = tokenResponse.GetProperty("expires_in").GetInt32();

                    var result = new
                    {
                        success = true,
                        message = "Successfully connected to QuickBooks!",
                        tokens = new
                        {
                            AccessToken = accessToken,
                            RefreshToken = refreshToken,
                            RealmId = realmId,
                            ExpiresIn = expiresIn,
                            ExpiryDate = DateTime.UtcNow.AddSeconds(expiresIn).ToString("yyyy-MM-ddTHH:mm:ssZ")
                        },
                        instructions = "Copy these tokens into your appsettings.Development.json file to replace the demo tokens"
                    };

                    return Ok(result);
                }
                else
                {
                    return BadRequest(new { error = $"Token exchange failed: {responseContent}" });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("invoices")]
        public async Task<IActionResult> GetInvoices()
        {
            try
            {
                // Get valid access token for QuickBooks API
                var accessToken = await _tokenService.GetValidAccessTokenAsync();
                if (string.IsNullOrEmpty(accessToken))
                {
                    return BadRequest(new { error = "Unable to obtain valid QuickBooks access token. Try /api/quickbooks/connect first." });
                }

                // Fetch invoices from QuickBooks API
                var invoices = await _quickBooksApiService.GetInvoicesAsync(accessToken, _config.RealmId);

                return Ok(invoices);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("setup-demo-data")]
        public async Task<IActionResult> SetupDemoData()
        {
            try
            {
                var accessToken = await _tokenService.GetValidAccessTokenAsync();
                if (string.IsNullOrEmpty(accessToken))
                {
                    return BadRequest(new { error = "Unable to obtain valid QuickBooks access token" });
                }

                var results = new List<string>();

                // Demo customers to create
                var demoCustomers = new[]
                {
                    new { Name = "Acme Manufacturing", Email = "billing@acme.com" },
                    new { Name = "Green Valley Resort", Email = "accounts@greenvalley.com" },
                    new { Name = "City Hall Building", Email = "maintenance@cityhall.gov" },
                    new { Name = "Martinez Family Home", Email = "martinez@email.com" },
                    new { Name = "Sunshine Apartments", Email = "manager@sunshine.com" }
                };

                // Create customers and invoices
                foreach (var customerData in demoCustomers)
                {
                    try
                    {
                        // Create customer
                        var customerId = await _quickBooksApiService.CreateCustomerAsync(
                            accessToken, _config.RealmId, customerData.Name, customerData.Email);

                        results.Add($"✅ Created customer: {customerData.Name} (ID: {customerId})");

                        // Create 1-3 invoices for this customer
                        var invoiceCount = new Random().Next(1, 4); // 1-3 invoices
                        for (int i = 0; i < invoiceCount; i++)
                        {
                            var amount = new Random().Next(25000, 150000) / 100.0m; // $250-$1500
                            var daysAgo = new Random().Next(1, 90); // 1-90 days ago

                            var jobRequest = new QuickBooksJobRequest
                            {
                                JobId = new Random().Next(1000, 9999),
                                QuickBooksCustomerId = customerId,
                                CustomerName = customerData.Name,
                                JobType = "Electrical",
                                Description = $"Demo invoice #{i + 1} for {customerData.Name}",
                                TotalAmount = amount,
                                JobDate = DateTime.Now.AddDays(-daysAgo),
                                LineItems = new List<QuickBooksLineItemRequest>
                                {
                                    new QuickBooksLineItemRequest
                                    {
                                        Description = "Electrical work",
                                        Quantity = 1,
                                        UnitCost = amount,
                                        Total = amount,
                                        Type = "Service"
                                    }
                                }
                            };

                            var invoiceId = await _quickBooksApiService.CreateInvoiceAsync(
                                accessToken, _config.RealmId, jobRequest);

                            results.Add($"  ✅ Created invoice: ${amount:F2} (ID: {invoiceId})");
                        }
                    }
                    catch (Exception ex)
                    {
                        results.Add($"❌ Failed to create {customerData.Name}: {ex.Message}");
                    }
                }

                return Ok(new
                {
                    success = true,
                    message = "Demo data setup completed",
                    results = results
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }

    public class SendJobRequest
    {
        public bool AsInvoice { get; set; } = true;
    }
}