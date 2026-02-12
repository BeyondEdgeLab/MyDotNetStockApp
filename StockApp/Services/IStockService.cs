using StockApp.Models;

namespace StockApp.Services
{
    public interface IStockService
    {
        Task<IEnumerable<StockPrice>> GetStockPricesAsync(string symbol, DateOnly startDate, DateOnly endDate);
        Task<IEnumerable<StockPrice>> GetRecentStockPricesAsync(string symbol, TimeSpan windowDuration);
        Task<IEnumerable<StockPrice>> GetRecentStockPricesForSymbolsAsync(IEnumerable<string> symbols, TimeSpan windowDuration);
        
        // New business logic methods
        Task<List<StockPriceResponse>> GetRecentStockPricesResponseAsync(MultiSymbolRequest request);
        Task<StockGrowthResponse> GetStockGrowthAsync(StockGrowthRequest request);
        Task<StockMomentumResponse> GetStockMomentumAsync(StockMomentumRequest request);
        Task<VolatilitySpikeResponse> GetVolatilitySpikesAsync(VolatilitySpikeRequest request);
    }
}
