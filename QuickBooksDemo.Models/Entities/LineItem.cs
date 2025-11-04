namespace QuickBooksDemo.Models.Entities;

public class LineItem
{
    public string Id { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal MaterialCost { get; set; }
    public int LaborHours { get; set; }
    public decimal LaborCost { get; set; }

    // Foreign key
    public string JobId { get; set; } = string.Empty;

    // Navigation property
    public virtual Job Job { get; set; } = null!;

    // Calculated property
    public decimal TotalCost => MaterialCost + LaborCost;
}