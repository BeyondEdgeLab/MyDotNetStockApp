using StockApp;
using StockApp.Models;
using System.Text.Json;

namespace StockApp.Services
{
    public class YahooFinanceStockService : IStockService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<YahooFinanceStockService> _logger;
        private const double DENOMINATOR_THRESHOLD = 0.0001;
        private const int VOLATILITY_PRECISION = 4;
        private const int SPIKE_FACTOR_PRECISION = 1;
        private const int PRICE_CHANGE_PRECISION = 2;

        public YahooFinanceStockService(HttpClient httpClient, ILogger<YahooFinanceStockService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<IEnumerable<StockPrice>> GetStockPricesAsync(string symbol, DateOnly startDate, DateOnly endDate)
        {
            _logger.LogInformation($"Getting stock prices for {symbol} from {startDate} to {endDate}");

            try
            {
                // Calculate Unix timestamps
                // Yahoo expects seconds since epoch
                long startEpoch = new DateTimeOffset(startDate.ToDateTime(TimeOnly.MinValue)).ToUnixTimeSeconds();
                long endEpoch = new DateTimeOffset(endDate.ToDateTime(TimeOnly.MaxValue)).ToUnixTimeSeconds();

                // Yahoo Finance Chart API URL
                // interval=1d for daily prices
                var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{symbol}?period1={startEpoch}&period2={endEpoch}&interval=1d&events=history";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                // Yahoo requires a User-Agent to avoid 429 Too Many Requests
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();
                var result = new List<StockPrice>();

                using (JsonDocument doc = JsonDocument.Parse(jsonString))
                {
                    var chart = doc.RootElement.GetProperty("chart");
                    var resultElement = chart.GetProperty("result");

                    if (resultElement.ValueKind != JsonValueKind.Null && resultElement.GetArrayLength() > 0)
                    {
                        var data = resultElement[0];

                        // Get Timestamps
                        if (!data.TryGetProperty("timestamp", out var timestampElement))
                        {
                            _logger.LogWarning($"No timestamp data found for {symbol}");
                            return result;
                        }

                        // Get Prices (Quotes)
                        var indicators = data.GetProperty("indicators");
                        var quote = indicators.GetProperty("quote")[0];
                        
                        // We need "close" prices usually, or "adjclose" if preferred
                        if (!quote.TryGetProperty("close", out var closeElement))
                        {
                            _logger.LogWarning($"No close price data found for {symbol}");
                            return result;
                        }

                        var timestamps = timestampElement.EnumerateArray().ToList();
                        var prices = closeElement.EnumerateArray().ToList();

                        for (int i = 0; i < timestamps.Count; i++)
                        {
                            // Some prices might be null in the array
                            if (i < prices.Count && prices[i].ValueKind != JsonValueKind.Null)
                            {
                                var priceVal = prices[i].GetDecimal();
                                var dateVal = DateTimeOffset.FromUnixTimeSeconds(timestamps[i].GetInt64());

                                result.Add(new StockPrice
                                {
                                    Symbol = symbol,
                                    Date = dateVal,
                                    Price = Math.Round(priceVal, 2)
                                });
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching data from Yahoo Finance for {symbol}");
                // Return empty list or rethrow depending on desired behavior. 
                // For now, let's return empty to allow the app to continue running.
                return Enumerable.Empty<StockPrice>();
            }
        }

        public async Task<IEnumerable<StockPrice>> GetRecentStockPricesAsync(string symbol, TimeSpan windowDuration)
        {
            _logger.LogInformation($"Getting recent stock prices for {symbol} for the last {windowDuration.TotalMinutes} minutes");

            try
            {
                // Calculate time range based on current time
                var endTime = DateTime.UtcNow;
                var startTime = endTime.Subtract(windowDuration);

                // Calculate Unix timestamps
                long startEpoch = new DateTimeOffset(startTime).ToUnixTimeSeconds();
                long endEpoch = new DateTimeOffset(endTime).ToUnixTimeSeconds();

                // Yahoo Finance Chart API URL with 1-minute interval for intraday data
                var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{symbol}?period1={startEpoch}&period2={endEpoch}&interval=1m&events=history";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();
                var result = new List<StockPrice>();

                using (JsonDocument doc = JsonDocument.Parse(jsonString))
                {
                    var chart = doc.RootElement.GetProperty("chart");
                    var resultElement = chart.GetProperty("result");

                    if (resultElement.ValueKind != JsonValueKind.Null && resultElement.GetArrayLength() > 0)
                    {
                        var data = resultElement[0];

                        // Get Timestamps
                        if (!data.TryGetProperty("timestamp", out var timestampElement))
                        {
                            _logger.LogWarning($"No timestamp data found for {symbol}");
                            return result;
                        }

                        // Get Prices (Quotes)
                        var indicators = data.GetProperty("indicators");
                        var quote = indicators.GetProperty("quote")[0];
                        
                        if (!quote.TryGetProperty("close", out var closeElement))
                        {
                            _logger.LogWarning($"No close price data found for {symbol}");
                            return result;
                        }

                        var timestamps = timestampElement.EnumerateArray().ToList();
                        var prices = closeElement.EnumerateArray().ToList();

                        for (int i = 0; i < timestamps.Count; i++)
                        {
                            // Some prices might be null in the array
                            if (i < prices.Count && prices[i].ValueKind != JsonValueKind.Null)
                            {
                                var priceVal = prices[i].GetDecimal();
                                var dateVal = DateTimeOffset.FromUnixTimeSeconds(timestamps[i].GetInt64());

                                result.Add(new StockPrice
                                {
                                    Symbol = symbol,
                                    Date = dateVal,
                                    Price = Math.Round(priceVal, 2)
                                });
                            }
                        }
                    }
                }

                _logger.LogInformation($"Retrieved {result.Count} price points for {symbol}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching recent data from Yahoo Finance for {symbol}");
                return Enumerable.Empty<StockPrice>();
            }
        }

        public async Task<IEnumerable<StockPrice>> GetRecentStockPricesForSymbolsAsync(IEnumerable<string> symbols, TimeSpan windowDuration)
        {
            _logger.LogInformation($"Getting recent stock prices for {symbols.Count()} symbols for the last {windowDuration.TotalMinutes} minutes");

            var tasks = symbols.Select(symbol => GetRecentStockPricesAsync(symbol, windowDuration));
            var results = await Task.WhenAll(tasks);
            
            return results.SelectMany(r => r);
        }

        public async Task<VolatilitySpikeResponse> GetVolatilitySpikesAsync(VolatilitySpikeRequest request)
        {
            _logger.LogInformation($"Calculating volatility spikes for {request.Symbols.Count} symbols");

            var asOfUtc = DateTimeOffset.UtcNow;
            var results = new List<VolatilitySpikeResult>();

            // Fetch data for each symbol
            foreach (var symbol in request.Symbols)
            {
                // Get all data for the baseline period
                var baselineDuration = TimeSpan.FromMinutes(request.BaselineMinutes);
                var prices = await GetRecentStockPricesAsync(symbol, baselineDuration);
                var orderedPrices = prices.OrderBy(p => p.Date).ToList();

                // Need at least 2 prices in each window to calculate volatility
                if (orderedPrices.Count < 2)
                {
                    _logger.LogWarning($"Insufficient data for {symbol}");
                    continue;
                }

                // Split data into baseline and short-term windows
                var now = DateTimeOffset.UtcNow;
                var shortTermStart = now.AddMinutes(-request.WindowMinutes);
                var baselineStart = now.AddMinutes(-request.BaselineMinutes);

                var shortTermPrices = orderedPrices.Where(p => p.Date >= shortTermStart).ToList();
                var baselinePrices = orderedPrices.Where(p => p.Date >= baselineStart && p.Date < shortTermStart).ToList();

                // Need at least 2 prices in each window
                if (shortTermPrices.Count < 2 || baselinePrices.Count < 2)
                {
                    _logger.LogWarning($"Insufficient data in windows for {symbol}");
                    continue;
                }

                // Calculate log returns and volatility for short-term window
                var shortTermReturns = CalculateLogReturns(shortTermPrices);
                var shortTermVolatility = CalculateStandardDeviation(shortTermReturns);

                // Calculate log returns and volatility for baseline window
                var baselineReturns = CalculateLogReturns(baselinePrices);
                var baselineVolatility = CalculateStandardDeviation(baselineReturns);

                // Skip if baseline volatility is too close to zero (avoid division by zero)
                if (baselineVolatility < DENOMINATOR_THRESHOLD)
                {
                    _logger.LogWarning($"Baseline volatility is too low for {symbol}");
                    continue;
                }

                // Calculate spike factor
                var spikeFactor = shortTermVolatility / baselineVolatility;

                // Only include if spike factor meets threshold
                if (spikeFactor >= request.SpikeThreshold)
                {
                    // Calculate price change percent (using decimal for financial precision)
                    var startPrice = shortTermPrices.First().Price;
                    var endPrice = shortTermPrices.Last().Price;
                    var priceChangePercent = startPrice != 0
                        ? (decimal)Math.Round(((endPrice - startPrice) / startPrice) * 100, PRICE_CHANGE_PRECISION)
                        : 0;

                    results.Add(new VolatilitySpikeResult
                    {
                        Symbol = symbol,
                        ShortTermVolatility = Math.Round(shortTermVolatility, VOLATILITY_PRECISION),
                        BaselineVolatility = Math.Round(baselineVolatility, VOLATILITY_PRECISION),
                        SpikeFactor = Math.Round(spikeFactor, SPIKE_FACTOR_PRECISION),
                        PriceChangePercent = priceChangePercent
                    });
                }
            }

            // Sort by highest spike factor first
            results = results.OrderByDescending(r => r.SpikeFactor).ToList();

            return new VolatilitySpikeResponse
            {
                WindowMinutes = request.WindowMinutes,
                BaselineMinutes = request.BaselineMinutes,
                AsOfUtc = asOfUtc,
                Results = results
            };
        }

        private List<double> CalculateLogReturns(List<StockPrice> prices)
        {
            var returns = new List<double>();
            for (int i = 1; i < prices.Count; i++)
            {
                var prevPrice = (double)prices[i - 1].Price;
                var currPrice = (double)prices[i].Price;

                if (prevPrice > 0 && currPrice > 0)
                {
                    var logReturn = Math.Log(currPrice / prevPrice);
                    returns.Add(logReturn);
                }
            }
            return returns;
        }

        private double CalculateStandardDeviation(List<double> values)
        {
            if (values.Count < 2)
            {
                return 0;
            }

            var mean = values.Average();
            var sumOfSquares = values.Sum(v => Math.Pow(v - mean, 2));
            var variance = sumOfSquares / (values.Count - 1);
            return Math.Sqrt(variance);
        }

        public async Task<List<StockPriceResponse>> GetRecentStockPricesResponseAsync(MultiSymbolRequest request)
        {
            _logger.LogInformation($"Getting recent stock prices response for {request.Symbols.Count()} symbols");

            var windowDuration = TimeSpan.FromMinutes(request.WindowMinutes);
            var allPrices = await GetRecentStockPricesForSymbolsAsync(request.Symbols, windowDuration);

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

            return response;
        }

        public async Task<StockGrowthResponse> GetStockGrowthAsync(StockGrowthRequest request)
        {
            _logger.LogInformation($"Calculating stock growth for {request.Symbols.Count()} symbols");

            var windowDuration = TimeSpan.FromMinutes(request.WindowMinutes);
            var asOfUtc = DateTimeOffset.UtcNow;
            var allPrices = await GetRecentStockPricesForSymbolsAsync(request.Symbols, windowDuration);

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
                        ? (decimal)Math.Round(((endPrice - startPrice) / startPrice) * 100, 2)
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
                .Select(r => r!)
                .OrderByDescending(r => r.PercentageGrowth)
                .ToList();

            var response = new StockGrowthResponse
            {
                WindowMinutes = request.WindowMinutes,
                AsOfUtc = asOfUtc,
                Results = results
            };

            return response;
        }

        public async Task<StockMomentumResponse> GetStockMomentumAsync(StockMomentumRequest request)
        {
            _logger.LogInformation($"Calculating stock momentum for {request.Symbols.Count()} symbols");

            var asOfUtc = DateTimeOffset.UtcNow;
            var results = new List<MomentumResult>();

            // Process each symbol
            foreach (var symbol in request.Symbols)
            {
                var momentumDict = new Dictionary<string, decimal>();
                var weightedScoreSum = 0m;

                // Process each time window
                for (int i = 0; i < request.WindowsMinutes.Count; i++)
                {
                    var windowMinutes = request.WindowsMinutes[i];
                    var windowDuration = TimeSpan.FromMinutes(windowMinutes);
                    
                    // Get stock prices for this window
                    var prices = await GetRecentStockPricesAsync(symbol, windowDuration);
                    var orderedPrices = prices.OrderBy(p => p.Date).ToList();

                    decimal momentum = 0m;

                    // Calculate momentum if we have at least 2 prices
                    if (orderedPrices.Count >= 2)
                    {
                        var startPrice = orderedPrices.First().Price;
                        var currentPrice = orderedPrices.Last().Price;

                        // Momentum = ((CurrentPrice - PriceAtWindowStart) / PriceAtWindowStart) * 100
                        if (startPrice != 0)
                        {
                            momentum = (decimal)Math.Round(((currentPrice - startPrice) / startPrice) * 100, 2);
                        }
                    }

                    // Store momentum with key format like "5m", "30m", etc.
                    var key = $"{windowMinutes}m";
                    momentumDict[key] = momentum;

                    // Add to weighted score if weights are provided
                    if (request.Weights != null)
                    {
                        weightedScoreSum += momentum * request.Weights[i];
                    }
                }

                // Calculate final score
                var score = request.Weights != null 
                    ? (decimal)Math.Round(weightedScoreSum, 2)
                    : 0m;

                results.Add(new MomentumResult
                {
                    Symbol = symbol,
                    Momentum = momentumDict,
                    Score = score
                });
            }

            // Sort by highest score first
            results = results.OrderByDescending(r => r.Score).ToList();

            var response = new StockMomentumResponse
            {
                AsOfUtc = asOfUtc,
                Results = results
            };

            return response;
        }
    }
}
