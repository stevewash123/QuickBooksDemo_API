using Microsoft.Extensions.Options;
using QuickBooksDemo.Models.Configuration;
using QuickBooksDemo.Models.DTOs;
using QuickBooksDemo.Service.Interfaces;
using System.Text.Json;

namespace QuickBooksDemo.Service.Services
{
    public class QuickBooksApiService : IQuickBooksApiService
    {
        private readonly QuickBooksConfig _config;
        private readonly HttpClient _httpClient;

        public QuickBooksApiService(IOptions<QuickBooksConfig> config, HttpClient httpClient)
        {
            _config = config.Value;
            _httpClient = httpClient;
        }

        public async Task<string> CreateCustomerAsync(string accessToken, string companyId, string customerName, string email = "", string phone = "")
        {
            try
            {
                var url = $"{_config.BaseUrl}/v3/company/{companyId}/customer";

                // QuickBooks API expects GivenName/FamilyName format
                var nameParts = customerName.Split(' ', 2);
                var customerData = new
                {
                    GivenName = nameParts[0],
                    FamilyName = nameParts.Length > 1 ? nameParts[1] : "",
                    CompanyName = customerName,
                    PrimaryEmailAddr = !string.IsNullOrEmpty(email) ? new { Address = email } : null,
                    PrimaryPhone = !string.IsNullOrEmpty(phone) ? new { FreeFormNumber = phone } : null
                };

                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("Authorization", $"Bearer {accessToken}");
                request.Headers.Add("Accept", "application/json");
                request.Content = new StringContent(
                    JsonSerializer.Serialize(customerData, new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull }),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var qbResponse = JsonSerializer.Deserialize<JsonElement>(content);
                    if (qbResponse.TryGetProperty("QueryResponse", out var queryResponse) &&
                        queryResponse.TryGetProperty("Customer", out var customerArray))
                    {
                        var customer = customerArray.EnumerateArray().FirstOrDefault();
                        return customer.GetProperty("Id").GetString() ?? "";
                    }
                    else if (qbResponse.TryGetProperty("Customer", out var directCustomer))
                    {
                        return directCustomer.GetProperty("Id").GetString() ?? "";
                    }
                    return "CREATED_BUT_NO_ID";
                }
                else
                {
                    throw new Exception($"QuickBooks API error: {content}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create customer: {ex.Message}", ex);
            }
        }

        public async Task<string> CreateInvoiceAsync(string accessToken, string companyId, QuickBooksJobRequest jobRequest)
        {
            try
            {
                var url = $"{_config.BaseUrl}/v3/company/{companyId}/invoice";

                // Create a simple service-based invoice (no items needed)
                var lines = new List<object>();

                if (jobRequest.LineItems?.Any() == true)
                {
                    foreach (var li in jobRequest.LineItems)
                    {
                        lines.Add(new
                        {
                            Amount = li.Total,
                            DetailType = "SalesItemLineDetail",
                            SalesItemLineDetail = new
                            {
                                Qty = 1,
                                UnitPrice = li.Total,
                                ItemRef = new { value = "1" } // Services item (usually exists)
                            }
                        });
                    }
                }
                else
                {
                    // Default line if no line items
                    lines.Add(new
                    {
                        Amount = jobRequest.TotalAmount,
                        DetailType = "SalesItemLineDetail",
                        SalesItemLineDetail = new
                        {
                            Qty = 1,
                            UnitPrice = jobRequest.TotalAmount,
                            ItemRef = new { value = "1" }
                        }
                    });
                }

                var invoiceData = new
                {
                    Line = lines.ToArray(),
                    CustomerRef = new
                    {
                        value = jobRequest.QuickBooksCustomerId
                    }
                };

                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("Authorization", $"Bearer {accessToken}");
                request.Headers.Add("Accept", "application/json");
                request.Content = new StringContent(
                    JsonSerializer.Serialize(invoiceData),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var qbResponse = JsonSerializer.Deserialize<JsonElement>(content);
                    // For CREATE operations, QB returns different structure
                    if (qbResponse.TryGetProperty("QueryResponse", out var queryResponse) &&
                        queryResponse.TryGetProperty("Invoice", out var invoiceArray))
                    {
                        var invoice = invoiceArray.EnumerateArray().FirstOrDefault();
                        return invoice.GetProperty("Id").GetString() ?? "";
                    }
                    else if (qbResponse.TryGetProperty("Invoice", out var directInvoice))
                    {
                        return directInvoice.GetProperty("Id").GetString() ?? "";
                    }
                    return "CREATED_BUT_NO_ID";
                }
                else
                {
                    throw new Exception($"QuickBooks API error: {content}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create invoice: {ex.Message}", ex);
            }
        }

        public async Task<string> CreateEstimateAsync(string accessToken, string companyId, QuickBooksJobRequest jobRequest)
        {
            try
            {
                // Simplified implementation for testing
                await System.Threading.Tasks.Task.Delay(200); // Simulate API call
                return $"ESTIMATE_{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create estimate: {ex.Message}", ex);
            }
        }

        public async Task<bool> TestConnectionAsync(string accessToken, string companyId)
        {
            try
            {
                // Simplified implementation for testing
                await System.Threading.Tasks.Task.Delay(50); // Simulate API call
                return !string.IsNullOrEmpty(accessToken) && !string.IsNullOrEmpty(companyId);
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<InvoiceDto>> GetInvoicesAsync(string accessToken, string companyId)
        {
            try
            {
                // For demo tokens, return mock data to simulate QB invoices
                if (accessToken.Contains("demo") || companyId.Contains("demo"))
                {
                    await Task.Delay(200); // Simulate API call
                    return new List<InvoiceDto>
                    {
                        new InvoiceDto
                        {
                            Id = "INVOICE_QB001234",
                            CustomerName = "Demo Customer from QB",
                            Amount = 1250.00m,
                            Date = DateTime.Now.AddDays(-5),
                            Status = "Paid",
                            Description = "Demo invoice retrieved from QuickBooks"
                        },
                        new InvoiceDto
                        {
                            Id = "INVOICE_QB005678",
                            CustomerName = "Another QB Customer",
                            Amount = 750.50m,
                            Date = DateTime.Now.AddDays(-2),
                            Status = "Sent",
                            Description = "Another demo invoice from QuickBooks"
                        }
                    };
                }

                // Real QuickBooks API call - limit to recent invoices for demo
                var thirtyDaysAgo = DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd");
                var query = Uri.EscapeDataString($"SELECT * FROM Invoice WHERE TxnDate >= '{thirtyDaysAgo}' ORDER BY TxnDate DESC");
                var url = $"{_config.BaseUrl}/v3/company/{companyId}/query?query={query}";
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", $"Bearer {accessToken}");
                request.Headers.Add("Accept", "application/json");

                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var qbResponse = JsonSerializer.Deserialize<JsonElement>(content);
                    var invoices = new List<InvoiceDto>();

                    if (qbResponse.TryGetProperty("QueryResponse", out var queryResponse) &&
                        queryResponse.TryGetProperty("Invoice", out var invoiceArray))
                    {
                        foreach (var invoice in invoiceArray.EnumerateArray())
                        {
                            var rawCustomerName = invoice.TryGetProperty("CustomerRef", out var customerRef)
                                ? customerRef.GetProperty("name").GetString() ?? ""
                                : "";

                            // Clean up customer name (remove addresses, extra spaces, etc.)
                            var cleanCustomerName = CleanCustomerName(rawCustomerName);

                            var invoiceDto = new InvoiceDto
                            {
                                Id = invoice.GetProperty("Id").GetString() ?? "",
                                CustomerName = cleanCustomerName,
                                Amount = invoice.TryGetProperty("TotalAmt", out var totalAmt)
                                    ? totalAmt.GetDecimal()
                                    : 0,
                                Date = invoice.TryGetProperty("TxnDate", out var txnDate)
                                    ? DateTime.Parse(txnDate.GetString() ?? DateTime.Now.ToString())
                                    : DateTime.Now,
                                Status = invoice.TryGetProperty("EmailStatus", out var emailStatus)
                                    ? emailStatus.GetString() ?? "Unknown"
                                    : "Unknown",
                                Description = invoice.TryGetProperty("PrivateNote", out var privateNote)
                                    ? privateNote.GetString() ?? ""
                                    : ""
                            };
                            invoices.Add(invoiceDto);
                        }
                    }

                    // Sort by date descending (newest first)
                    return invoices.OrderByDescending(i => i.Date).ToList();
                }
                else
                {
                    throw new Exception($"QuickBooks API error: {content}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get invoices from QuickBooks: {ex.Message}", ex);
            }
        }

        private static string CleanCustomerName(string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName))
                return "Unknown Customer";

            // Remove common address patterns and clean up
            var cleaned = rawName.Trim();

            // Remove street addresses (numbers followed by street names)
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\d+\s+[\w\s]+(Street|St|Avenue|Ave|Road|Rd|Drive|Dr|Lane|Ln|Boulevard|Blvd|Way|Circle|Cir)\b", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Remove state/zip patterns
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\b[A-Z]{2}\s+\d{5}(-\d{4})?\b", "");

            // Remove extra whitespace and common separators
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"[,\n\r]+", " ");
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s+", " ");

            // Trim and return, limit length for display
            cleaned = cleaned.Trim();
            if (cleaned.Length > 50)
                cleaned = cleaned.Substring(0, 47) + "...";

            return string.IsNullOrWhiteSpace(cleaned) ? "Unknown Customer" : cleaned;
        }

    }
}