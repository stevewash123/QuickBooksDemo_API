using QuickBooksDemo.Models.Enums;

namespace QuickBooksDemo.Models.Entities;

public class Job
{
    public string Id { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public JobStatus Status { get; set; }
    public JobType JobType { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal QuotedAmount { get; set; }
    public decimal? ActualAmount { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string? AssignedTechnicianId { get; set; }

    // Navigation properties
    public virtual Customer Customer { get; set; } = null!;
    public virtual Technician? AssignedTechnician { get; set; }
    public virtual ICollection<LineItem> LineItems { get; set; } = new List<LineItem>();

    // Calculated properties
    public decimal TotalLineItemCost => LineItems?.Sum(li => li.TotalCost) ?? 0;
    public int TotalLaborHours => LineItems?.Sum(li => li.LaborHours) ?? 0;
}