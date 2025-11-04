using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuickBooksDemo.DAL.Context;
using QuickBooksDemo.Models.Configuration;
using QuickBooksDemo.Service.Interfaces;

namespace QuickBooksDemo.Service.Services
{
    public class ReseedService : IReseedService
    {
        private readonly QuickBooksDemoContext _context;
        private readonly IQuickBooksApiService _quickBooksApiService;
        private readonly IQuickBooksTokenService _tokenService;
        private readonly QuickBooksConfig _config;

        public ReseedService(
            QuickBooksDemoContext context,
            IQuickBooksApiService quickBooksApiService,
            IQuickBooksTokenService tokenService,
            IOptions<QuickBooksConfig> config)
        {
            _context = context;
            _quickBooksApiService = quickBooksApiService;
            _tokenService = tokenService;
            _config = config.Value;
        }

        public async Task<string> ReseedDatabaseAsync()
        {
            var results = new List<string>();

            try
            {
                // Step 1: Clear existing database data
                results.Add("Clearing existing database data...");

                // Remove all entities in correct order (respecting foreign keys)
                _context.LineItems.RemoveRange(_context.LineItems);
                _context.Jobs.RemoveRange(_context.Jobs);
                _context.Customers.RemoveRange(_context.Customers);
                _context.Technicians.RemoveRange(_context.Technicians);

                await _context.SaveChangesAsync();
                results.Add("✅ Database cleared successfully");

                // Step 2: Recreate database with seed data
                results.Add("Recreating database with seed data...");
                await _context.Database.EnsureDeletedAsync();
                await _context.Database.EnsureCreatedAsync();
                results.Add("✅ Database recreated with seed data");

                // Step 3: QuickBooks demo data setup
                results.Add("Setting up QuickBooks demo data...");
                await SetupQuickBooksDemoDataAsync(results);

                results.Add("🎉 Reseed completed successfully!");
                return string.Join("\n", results);
            }
            catch (Exception ex)
            {
                results.Add($"❌ Error during reseed: {ex.Message}");
                return string.Join("\n", results);
            }
        }

        private async Task ManageQuickBooksInvoicesAsync(List<string> results)
        {
            try
            {
                // Check if QuickBooks connection is available
                var isConnected = await _tokenService.IsConnectionValidAsync();
                if (!isConnected)
                {
                    results.Add("⚠️ QuickBooks not connected - skipping invoice management");
                    return;
                }

                var accessToken = await _tokenService.GetValidAccessTokenAsync();
                if (string.IsNullOrEmpty(accessToken))
                {
                    results.Add("⚠️ Could not obtain QuickBooks access token - skipping invoice management");
                    return;
                }

                // Create QuickBooks invoices for actual seeded jobs
                results.Add("Creating QuickBooks invoices for seeded jobs...");

                // Get the seeded jobs and customers from the database
                var jobs = await _context.Jobs
                    .Include(j => j.Customer)
                    .Include(j => j.LineItems)
                    .ToListAsync();

                int invoiceCount = 0;
                foreach (var job in jobs)
                {
                    try
                    {
                        // Create a QuickBooks job request from actual job data
                        var jobRequest = new Models.DTOs.QuickBooksJobRequest
                        {
                            JobId = job.Id.GetHashCode(), // Convert string ID to int for demo
                            CustomerName = job.Customer?.Name ?? "Unknown Customer",
                            JobType = job.JobType.ToString(),
                            Description = job.Description,
                            TotalAmount = job.QuotedAmount,
                            JobDate = job.CreatedDate,
                            LineItems = job.LineItems?.Select(li => new Models.DTOs.QuickBooksLineItemRequest
                            {
                                Description = li.Description,
                                Quantity = li.LaborHours,
                                UnitCost = li.LaborCost + li.MaterialCost,
                                Total = li.TotalCost,
                                Type = "Service"
                            }).ToList() ?? new List<Models.DTOs.QuickBooksLineItemRequest>
                            {
                                new Models.DTOs.QuickBooksLineItemRequest
                                {
                                    Description = job.Description,
                                    Quantity = 1,
                                    UnitCost = job.QuotedAmount,
                                    Total = job.QuotedAmount,
                                    Type = "Service"
                                }
                            }
                        };

                        var invoiceId = await _quickBooksApiService.CreateInvoiceAsync(
                            accessToken,
                            "demo_realm_id",
                            jobRequest);

                        results.Add($"✅ Created invoice {invoiceId} for {job.Customer?.Name} (Job {job.Id})");
                        invoiceCount++;
                    }
                    catch (Exception ex)
                    {
                        results.Add($"⚠️ Failed to create invoice for {job.Customer?.Name} (Job {job.Id}): {ex.Message}");
                    }
                }

                results.Add($"✅ QuickBooks invoice management completed ({invoiceCount} invoices created)");
            }
            catch (Exception ex)
            {
                results.Add($"⚠️ QuickBooks invoice management failed: {ex.Message}");
            }
        }

        private async Task SetupQuickBooksDemoDataAsync(List<string> results)
        {
            try
            {
                // Check if QuickBooks connection is available
                var isConnected = await _tokenService.IsConnectionValidAsync();
                if (!isConnected)
                {
                    results.Add("⚠️ QuickBooks not connected - skipping demo data setup");
                    return;
                }

                var accessToken = await _tokenService.GetValidAccessTokenAsync();
                if (string.IsNullOrEmpty(accessToken))
                {
                    results.Add("⚠️ Could not obtain QuickBooks access token - skipping demo data setup");
                    return;
                }

                results.Add("Creating clean demo customers and invoices in QuickBooks...");

                // Demo customers to create
                var demoCustomers = new[]
                {
                    new { Name = "Acme Manufacturing", Email = "billing@acme.com" },
                    new { Name = "Green Valley Resort", Email = "accounts@greenvalley.com" },
                    new { Name = "City Hall Building", Email = "maintenance@cityhall.gov" },
                    new { Name = "Martinez Family Home", Email = "martinez@email.com" },
                    new { Name = "Sunshine Apartments", Email = "manager@sunshine.com" }
                };

                int totalCustomers = 0;
                int totalInvoices = 0;

                // Create customers and invoices
                foreach (var customerData in demoCustomers)
                {
                    try
                    {
                        // Create customer
                        var customerId = await _quickBooksApiService.CreateCustomerAsync(
                            accessToken, _config.RealmId, customerData.Name, customerData.Email);

                        results.Add($"✅ Created customer: {customerData.Name} (ID: {customerId})");
                        totalCustomers++;

                        // Create 1-3 invoices for this customer
                        var random = new Random();
                        var invoiceCount = random.Next(1, 4); // 1-3 invoices
                        for (int i = 0; i < invoiceCount; i++)
                        {
                            var amount = random.Next(25000, 150000) / 100.0m; // $250-$1500
                            var daysAgo = random.Next(1, 90); // 1-90 days ago

                            var jobRequest = new Models.DTOs.QuickBooksJobRequest
                            {
                                JobId = random.Next(1000, 9999),
                                QuickBooksCustomerId = customerId,
                                CustomerName = customerData.Name,
                                JobType = "Electrical",
                                Description = $"Electrical service work for {customerData.Name}",
                                TotalAmount = amount,
                                JobDate = DateTime.Now.AddDays(-daysAgo),
                                LineItems = new List<Models.DTOs.QuickBooksLineItemRequest>
                                {
                                    new Models.DTOs.QuickBooksLineItemRequest
                                    {
                                        Description = "Electrical installation and repair",
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
                            totalInvoices++;
                        }
                    }
                    catch (Exception ex)
                    {
                        results.Add($"❌ Failed to create {customerData.Name}: {ex.Message}");
                    }
                }

                results.Add($"✅ QuickBooks demo data setup completed ({totalCustomers} customers, {totalInvoices} invoices created)");
            }
            catch (Exception ex)
            {
                results.Add($"⚠️ QuickBooks demo data setup failed: {ex.Message}");
            }
        }
    }
}