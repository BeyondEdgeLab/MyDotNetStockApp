namespace StockApp.Services
{
    public interface IStockService
    {
        Task<IEnumerable<StockPrice>> GetStockPricesAsync(string symbol, DateOnly startDate, DateOnly endDate);
    }
}
