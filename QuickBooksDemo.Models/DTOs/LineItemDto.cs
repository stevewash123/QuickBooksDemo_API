namespace QuickBooksDemo.Models.DTOs;

public class LineItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal MaterialCost { get; set; }
    public int LaborHours { get; set; }
    public decimal LaborCost { get; set; }
    public decimal TotalCost { get; set; }
}