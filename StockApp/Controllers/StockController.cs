using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using StockApp.Services;
using StockApp.Models;

namespace StockApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StockController : ControllerBase
    {
        private readonly ILogger<StockController> _logger;
        private readonly HttpClient _httpClient;
        private readonly IStockService _stockService;

        public StockController(ILogger<StockController> logger, HttpClient httpClient, IStockService stockService)
        {
            _logger = logger;
            _httpClient = httpClient;
            _stockService = stockService;
        }

        [HttpGet("{symbol}")]
        public async Task<IEnumerable<StockPrice>> Get(string symbol, [FromQuery] DateOnly? startDate, [FromQuery] DateOnly? endDate)
        {
            var start = startDate ?? DateOnly.FromDateTime(DateTime.Now.AddDays(-30));
            var end = endDate ?? DateOnly.FromDateTime(DateTime.Now);

            return await _stockService.GetStockPricesAsync(symbol, start, end);
        }

        [HttpGet("{symbol}/recent")]
        public async Task<IEnumerable<StockPrice>> GetRecent(string symbol, [FromQuery] int minutes = 5)
        {
            var windowDuration = TimeSpan.FromMinutes(minutes);
            return await _stockService.GetRecentStockPricesAsync(symbol, windowDuration);
        }

        [HttpPost("recent")]
        public async Task<IActionResult> GetRecentForMultipleSymbols([FromBody] MultiSymbolRequest request)
        {
            if (request.Symbols == null || !request.Symbols.Any())
            {
                return BadRequest(new { error = "Symbols list cannot be empty" });
            }

            var windowDuration = TimeSpan.FromMinutes(request.WindowMinutes);
            var allPrices = await _stockService.GetRecentStockPricesForSymbolsAsync(request.Symbols, windowDuration);

            // Group by symbol and sort prices by timestamp (most recent first)
            var response = allPrices
                .GroupBy(p => p.Symbol)
                .Select(g => new StockPriceResponse
                {
                    Symbol = g.Key,
                    Prices = g.Select(p => new PricePoint
                    {
                        Price = p.Price,
                        Timestamp = p.Date
                    })
                    .OrderByDescending(p => p.Timestamp)
                    .ToList()
                })
                .ToList();

            return Ok(response);
        }

        [HttpPost("growth")]
        public async Task<IActionResult> GetStockGrowth([FromBody] StockGrowthRequest request)
        {
            if (request.Symbols == null || !request.Symbols.Any())
            {
                return BadRequest(new { error = "Symbols list cannot be empty" });
            }

            var windowDuration = TimeSpan.FromMinutes(request.WindowMinutes);
            var asOfUtc = DateTime.UtcNow;
            var allPrices = await _stockService.GetRecentStockPricesForSymbolsAsync(request.Symbols, windowDuration);

            // Group by symbol, calculate growth, and sort by percentage growth (highest first)
            var results = allPrices
                .GroupBy(p => p.Symbol)
                .Select(g =>
                {
                    var orderedPrices = g.OrderBy(p => p.Date).ToList();
                    
                    // If we don't have at least 2 prices, we can't calculate growth
                    if (orderedPrices.Count < 2)
                    {
                        return null;
                    }

                    var startPrice = orderedPrices.First().Price;
                    var endPrice = orderedPrices.Last().Price;
                    
                    // Calculate percentage growth: ((end - start) / start) * 100
                    var percentageGrowth = startPrice != 0 
                        ? Math.Round(((endPrice - startPrice) / startPrice) * 100, 2)
                        : 0;

                    return new StockResult
                    {
                        Symbol = g.Key,
                        StartPrice = startPrice,
                        EndPrice = endPrice,
                        PercentageGrowth = percentageGrowth,
                        Prices = orderedPrices.Select(p => new PriceData
                        {
                            Timestamp = p.Date,
                            Price = p.Price
                        })
                        .OrderByDescending(p => p.Timestamp)
                        .ToList()
                    };
                })
                .Where(r => r != null)
                .OrderByDescending(r => r!.PercentageGrowth)
                .ToList();

            var response = new StockGrowthResponse
            {
                WindowMinutes = request.WindowMinutes,
                AsOfUtc = asOfUtc,
                Results = results!
            };

            return Ok(response);
        }
    }
}
