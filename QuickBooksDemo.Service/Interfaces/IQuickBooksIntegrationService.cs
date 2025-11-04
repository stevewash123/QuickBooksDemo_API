using QuickBooksDemo.Models.DTOs;

namespace QuickBooksDemo.Service.Interfaces
{
    public interface IQuickBooksIntegrationService
    {
        Task<string> SendJobToQuickBooksAsync(string jobId, bool asInvoice = true);
        Task<bool> TestConnectionAsync();
    }
}