namespace StockApp.Models
{
    public class StockGrowthResponse
    {
        public int WindowMinutes { get; set; }
        public DateTimeOffset AsOfUtc { get; set; }
        public List<StockResult> Results { get; set; } = new();
    }

    public class StockResult
    {
        public string Symbol { get; set; } = string.Empty;
        public decimal StartPrice { get; set; }
        public decimal EndPrice { get; set; }
        public decimal PercentageGrowth { get; set; }
        public List<PriceData> Prices { get; set; } = new();
    }

    public class PriceData
    {
        public DateTimeOffset Timestamp { get; set; }
        public decimal Price { get; set; }
    }
}
