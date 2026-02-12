using StockApp.Models;

namespace StockApp.Services
{
    public interface IStockService
    {
        Task<IEnumerable<StockPrice>> GetStockPricesAsync(string symbol, DateOnly startDate, DateOnly endDate);
        Task<IEnumerable<StockPrice>> GetRecentStockPricesAsync(string symbol, TimeSpan windowDuration);
        Task<IEnumerable<StockPrice>> GetRecentStockPricesForSymbolsAsync(IEnumerable<string> symbols, TimeSpan windowDuration);
        Task<VolatilitySpikeResponse> GetVolatilitySpikesAsync(VolatilitySpikeRequest request);
    }
}
