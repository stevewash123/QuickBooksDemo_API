namespace QuickBooksDemo.Models.DTOs
{
    public class InvoiceDto
    {
        public string Id { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; } = string.Empty;
        public string JobId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}