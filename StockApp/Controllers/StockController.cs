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
        private readonly IStockService _stockService;

        public StockController(ILogger<StockController> logger, IStockService stockService)
        {
            _logger = logger;
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

            var response = await _stockService.GetRecentStockPricesResponseAsync(request);
            return Ok(response);
        }

        [HttpPost("growth")]
        public async Task<IActionResult> GetStockGrowth([FromBody] StockGrowthRequest request)
        {
            if (request.Symbols == null || !request.Symbols.Any())
            {
                return BadRequest(new { error = "Symbols list cannot be empty" });
            }

            var response = await _stockService.GetStockGrowthAsync(request);
            return Ok(response);
        }

        [HttpPost("momentum")]
        public async Task<IActionResult> GetStockMomentum([FromBody] StockMomentumRequest request)
        {
            if (request.Symbols == null || !request.Symbols.Any())
            {
                return BadRequest(new { error = "Symbols list cannot be empty" });
            }

            if (request.WindowsMinutes == null || !request.WindowsMinutes.Any())
            {
                return BadRequest(new { error = "WindowsMinutes list cannot be empty" });
            }

            if (request.Weights != null && request.Weights.Count != request.WindowsMinutes.Count)
            {
                return BadRequest(new { error = "Weights count must match WindowsMinutes count" });
            }

            var response = await _stockService.GetStockMomentumAsync(request);
            return Ok(response);
        }

        [HttpPost("volatility/spikes")]
        public async Task<IActionResult> GetVolatilitySpikes([FromBody] VolatilitySpikeRequest request)
        {
            if (request.Symbols == null || !request.Symbols.Any())
            {
                return BadRequest(new { error = "Symbols list cannot be empty" });
            }

            if (request.WindowMinutes <= 0)
            {
                return BadRequest(new { error = "WindowMinutes must be greater than 0" });
            }

            if (request.BaselineMinutes <= request.WindowMinutes)
            {
                return BadRequest(new { error = "BaselineMinutes must be greater than WindowMinutes" });
            }

            if (request.SpikeThreshold <= 0)
            {
                return BadRequest(new { error = "SpikeThreshold must be greater than 0" });
            }

            var response = await _stockService.GetVolatilitySpikesAsync(request);
            return Ok(response);
        }
    }
}
