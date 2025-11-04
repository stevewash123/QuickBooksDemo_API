using QuickBooksDemo.Models.Enums;

namespace QuickBooksDemo.Models.DTOs;

public class JobDto
{
    public string Id { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public JobStatus Status { get; set; }
    public JobType JobType { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal QuotedAmount { get; set; }
    public decimal? ActualAmount { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string? AssignedTechnicianId { get; set; }
    public string? AssignedTechnicianName { get; set; }
    public List<LineItemDto> LineItems { get; set; } = new List<LineItemDto>();
    public decimal TotalLineItemCost { get; set; }
    public int TotalLaborHours { get; set; }
}