using StockApp;
using System.Text.Json;

namespace StockApp.Services
{
    public class YahooFinanceStockService : IStockService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<YahooFinanceStockService> _logger;

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
                                var dateVal = DateTimeOffset.FromUnixTimeSeconds(timestamps[i].GetInt64()).LocalDateTime;

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
                                var dateVal = DateTimeOffset.FromUnixTimeSeconds(timestamps[i].GetInt64()).UtcDateTime;

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

        public async Task<IEnumerable<StockTrendResult>> AnalyzeTrendsAsync(IEnumerable<string> symbols, TimeSpan windowDuration)
        {
            _logger.LogInformation($"Analyzing trends for {symbols.Count()} symbols over {windowDuration.TotalMinutes} minutes");

            var tasks = symbols.Select(async symbol =>
            {
                var prices = await GetRecentStockPricesAsync(symbol, windowDuration);
                var priceList = prices.ToList();
                
                var slope = CalculateLinearRegressionSlope(priceList);

                return new StockTrendResult
                {
                    Symbol = symbol,
                    Prices = priceList,
                    Slope = slope
                };
            });

            var results = await Task.WhenAll(tasks);

            // Sort by slope in descending order (most positive first)
            return results.OrderByDescending(r => r.Slope);
        }

        private double CalculateLinearRegressionSlope(List<StockPrice> prices)
        {
            if (prices.Count < 2)
            {
                return 0.0;
            }

            // Use index as X (time) and price as Y
            // Linear regression: slope = Σ((x - x̄)(y - ȳ)) / Σ((x - x̄)²)
            
            int n = prices.Count;
            double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;

            for (int i = 0; i < n; i++)
            {
                double x = i; // Use index as time representation
                double y = (double)prices[i].Price;

                sumX += x;
                sumY += y;
                sumXY += x * y;
                sumX2 += x * x;
            }

            double xMean = sumX / n;
            double yMean = sumY / n;

            double numerator = sumXY - n * xMean * yMean;
            double denominator = sumX2 - n * xMean * xMean;

            if (Math.Abs(denominator) < 0.0001)
            {
                return 0.0;
            }

            return numerator / denominator;
        }
    }
}
