using QuickBooksDemo.Models.DTOs;

namespace QuickBooksDemo.Service.Interfaces
{
    public interface IQuickBooksApiService
    {
        Task<string> CreateCustomerAsync(string accessToken, string companyId, string customerName, string email = "", string phone = "");
        Task<string> CreateInvoiceAsync(string accessToken, string companyId, QuickBooksJobRequest jobRequest);
        Task<string> CreateEstimateAsync(string accessToken, string companyId, QuickBooksJobRequest jobRequest);
        Task<bool> TestConnectionAsync(string accessToken, string companyId);
        Task<List<InvoiceDto>> GetInvoicesAsync(string accessToken, string companyId);
    }
}