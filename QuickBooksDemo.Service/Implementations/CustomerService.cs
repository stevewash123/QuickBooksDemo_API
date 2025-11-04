using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuickBooksDemo.DAL.Context;
using QuickBooksDemo.Models.Configuration;
using QuickBooksDemo.Models.DTOs;
using QuickBooksDemo.Models.Entities;
using QuickBooksDemo.Service.Interfaces;

namespace QuickBooksDemo.Service.Implementations;

public class CustomerService : ICustomerService
{
    private readonly QuickBooksDemoContext _context;
    private readonly IQuickBooksApiService _quickBooksApiService;
    private readonly IQuickBooksTokenService _tokenService;
    private readonly QuickBooksConfig _config;

    public CustomerService(
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

    public async Task<IEnumerable<CustomerDto>> GetAllCustomersAsync()
    {
        var customers = await _context.Customers.ToListAsync();
        return customers.Select(MapToDto);
    }

    public async Task<CustomerDto?> GetCustomerByIdAsync(string id)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == id);
        return customer != null ? MapToDto(customer) : null;
    }

    public async Task<CustomerDto> CreateCustomerAsync(CustomerDto customerDto)
    {
        var customer = MapToEntity(customerDto);
        customer.Id = Guid.NewGuid().ToString("N")[..8]; // Generate short ID

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        return MapToDto(customer);
    }

    public async Task<CustomerDto?> UpdateCustomerAsync(string id, CustomerDto customerDto)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == id);
        if (customer == null) return null;

        customer.Name = customerDto.Name;
        customer.Email = customerDto.Email;
        customer.Phone = customerDto.Phone;
        customer.Address = customerDto.Address;
        customer.CustomerType = customerDto.CustomerType;
        customer.Notes = customerDto.Notes;

        await _context.SaveChangesAsync();
        return MapToDto(customer);
    }

    public async Task<bool> DeleteCustomerAsync(string id)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == id);
        if (customer == null) return false;

        _context.Customers.Remove(customer);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<CustomerDto>> SearchCustomersAsync(string searchTerm)
    {
        var customers = await _context.Customers
            .Where(c => c.Name.Contains(searchTerm) ||
                       c.Email.Contains(searchTerm) ||
                       c.Phone.Contains(searchTerm) ||
                       c.Address.Contains(searchTerm))
            .ToListAsync();

        return customers.Select(MapToDto);
    }

    public async Task<string> EnsureCustomerInQuickBooksAsync(string customerId)
    {
        // 1. Get local customer
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == customerId);
        if (customer == null)
        {
            throw new Exception($"Customer {customerId} not found");
        }

        // 2. Check if already synced to QuickBooks
        if (!string.IsNullOrEmpty(customer.QuickBooksId))
        {
            return customer.QuickBooksId; // Already exists, return QB ID
        }

        // 3. Not synced yet - create in QuickBooks (FAIL-FAST on error)
        try
        {
            var accessToken = await _tokenService.GetValidAccessTokenAsync();
            var qbCustomerId = await _quickBooksApiService.CreateCustomerAsync(
                accessToken, _config.RealmId, customer.Name, customer.Email, customer.Phone);

            // 4. Update local record with QB ID
            customer.QuickBooksId = qbCustomerId;
            customer.QuickBooksSyncDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // 5. Return QB customer ID for invoice creation
            return qbCustomerId;
        }
        catch (Exception ex)
        {
            // FAIL-FAST: If QB customer creation fails, throw exception
            // This will cause the invoice creation to fail as requested
            throw new Exception($"Failed to create customer {customer.Name} in QuickBooks: {ex.Message}", ex);
        }
    }

    private static CustomerDto MapToDto(Customer customer)
    {
        return new CustomerDto
        {
            Id = customer.Id,
            Name = customer.Name,
            Email = customer.Email,
            Phone = customer.Phone,
            Address = customer.Address,
            CustomerType = customer.CustomerType,
            Notes = customer.Notes,
            QuickBooksId = customer.QuickBooksId,
            QuickBooksSyncDate = customer.QuickBooksSyncDate
        };
    }

    private static Customer MapToEntity(CustomerDto dto)
    {
        return new Customer
        {
            Id = dto.Id,
            Name = dto.Name,
            Email = dto.Email,
            Phone = dto.Phone,
            Address = dto.Address,
            CustomerType = dto.CustomerType,
            Notes = dto.Notes,
            QuickBooksId = dto.QuickBooksId,
            QuickBooksSyncDate = dto.QuickBooksSyncDate
        };
    }
}