using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using StockApp.Services;

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
    }
}
