using QuickBooksDemo.Models.DTOs;

namespace QuickBooksDemo.Service.Interfaces;

public interface ICustomerService
{
    Task<IEnumerable<CustomerDto>> GetAllCustomersAsync();
    Task<CustomerDto?> GetCustomerByIdAsync(string id);
    Task<CustomerDto> CreateCustomerAsync(CustomerDto customerDto);
    Task<CustomerDto?> UpdateCustomerAsync(string id, CustomerDto customerDto);
    Task<bool> DeleteCustomerAsync(string id);
    Task<IEnumerable<CustomerDto>> SearchCustomersAsync(string searchTerm);

    // QuickBooks Integration
    Task<string> EnsureCustomerInQuickBooksAsync(string customerId);
}