namespace StockApp.Models
{
    public class VolatilitySpikeResponse
    {
        public int WindowMinutes { get; set; }
        public int BaselineMinutes { get; set; }
        public DateTimeOffset AsOfUtc { get; set; }
        public List<VolatilitySpikeResult> Results { get; set; } = new();
    }

    public class VolatilitySpikeResult
    {
        public string Symbol { get; set; } = string.Empty;
        public double ShortTermVolatility { get; set; }
        public double BaselineVolatility { get; set; }
        public double SpikeFactor { get; set; }
        public decimal PriceChangePercent { get; set; }
    }
}
