using Microsoft.Extensions.Options;
using QuickBooksDemo.Models.Configuration;
using QuickBooksDemo.Models.DTOs;
using QuickBooksDemo.Service.Interfaces;

namespace QuickBooksDemo.Service.Services
{
    public class QuickBooksIntegrationService : IQuickBooksIntegrationService
    {
        private readonly IQuickBooksTokenService _tokenService;
        private readonly IQuickBooksApiService _quickBooksApiService;
        private readonly IJobService _jobService;
        private readonly ICustomerService _customerService;
        private readonly QuickBooksConfig _config;

        public QuickBooksIntegrationService(
            IQuickBooksTokenService tokenService,
            IQuickBooksApiService quickBooksApiService,
            IJobService jobService,
            ICustomerService customerService,
            IOptions<QuickBooksConfig> config)
        {
            _tokenService = tokenService;
            _quickBooksApiService = quickBooksApiService;
            _jobService = jobService;
            _customerService = customerService;
            _config = config.Value;
        }

        public async Task<string> SendJobToQuickBooksAsync(string jobId, bool asInvoice = true)
        {
            try
            {
                // Get valid access token
                var accessToken = await _tokenService.GetValidAccessTokenAsync();
                if (string.IsNullOrEmpty(accessToken))
                {
                    throw new Exception("Unable to obtain valid QuickBooks access token");
                }

                var job = await _jobService.GetJobByIdAsync(jobId);
                if (job == null)
                {
                    throw new Exception("Job not found");
                }

                // LAZY SYNC: Ensure customer exists in QuickBooks (FAIL-FAST on error)
                var qbCustomerId = await _customerService.EnsureCustomerInQuickBooksAsync(job.CustomerId);

                var quickBooksJobRequest = new QuickBooksJobRequest
                {
                    JobId = job.Id.GetHashCode(), // Convert string ID to int hash for QuickBooks
                    QuickBooksCustomerId = qbCustomerId, // Use synced QB customer ID
                    CustomerName = job.CustomerName,
                    JobType = job.JobType.ToString(),
                    Description = job.Description,
                    TotalAmount = job.TotalLineItemCost,
                    JobDate = job.ScheduledDate ?? DateTime.Now,
                    LineItems = job.LineItems?.Select(li => new QuickBooksLineItemRequest
                    {
                        Description = li.Description,
                        Quantity = 1m, // Default quantity since LineItemDto doesn't have quantity
                        UnitCost = li.TotalCost, // Use total cost as unit cost for now
                        Total = li.TotalCost,
                        Type = "Service" // Default type
                    }).ToList() ?? new List<QuickBooksLineItemRequest>()
                };

                if (asInvoice)
                {
                    return await _quickBooksApiService.CreateInvoiceAsync(accessToken, _config.RealmId, quickBooksJobRequest);
                }
                else
                {
                    return await _quickBooksApiService.CreateEstimateAsync(accessToken, _config.RealmId, quickBooksJobRequest);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to send job to QuickBooks: {ex.Message}", ex);
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                return await _tokenService.IsConnectionValidAsync();
            }
            catch
            {
                return false;
            }
        }
    }
}