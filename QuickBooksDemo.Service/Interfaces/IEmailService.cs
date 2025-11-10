namespace QuickBooksDemo.Service.Interfaces
{
    public interface IEmailService
    {
        Task SendQuickBooksTokenErrorAsync(string errorMessage, Exception exception);
    }
}