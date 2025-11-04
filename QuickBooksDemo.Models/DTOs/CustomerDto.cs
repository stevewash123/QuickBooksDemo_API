using QuickBooksDemo.Models.Enums;

namespace QuickBooksDemo.Models.DTOs;

public class CustomerDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public CustomerType CustomerType { get; set; }
    public string? Notes { get; set; }

    // QuickBooks Integration Fields
    public string? QuickBooksId { get; set; }
    public DateTime? QuickBooksSyncDate { get; set; }
}