namespace QuickBooksDemo.Service.Interfaces
{
    public interface IReseedService
    {
        Task<string> ReseedDatabaseAsync();
    }
}