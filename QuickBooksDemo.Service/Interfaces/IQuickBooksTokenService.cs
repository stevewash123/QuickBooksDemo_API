namespace QuickBooksDemo.Service.Interfaces
{
    public interface IQuickBooksTokenService
    {
        Task<string> GetValidAccessTokenAsync();
        Task<bool> IsConnectionValidAsync();
    }
}