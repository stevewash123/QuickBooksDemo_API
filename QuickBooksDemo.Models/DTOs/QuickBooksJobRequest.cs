namespace QuickBooksDemo.Models.DTOs
{
    public class QuickBooksJobRequest
    {
        public int JobId { get; set; }
        public string QuickBooksCustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string JobType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public DateTime JobDate { get; set; }
        public List<QuickBooksLineItemRequest> LineItems { get; set; } = new();
    }

    public class QuickBooksLineItemRequest
    {
        public string Description { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal Total { get; set; }
        public string Type { get; set; } = string.Empty;
    }
}