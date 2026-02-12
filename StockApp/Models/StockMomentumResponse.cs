namespace StockApp.Models
{
    public class StockMomentumResponse
    {
        public DateTimeOffset AsOfUtc { get; set; }
        public List<MomentumResult> Results { get; set; } = new();
    }

    public class MomentumResult
    {
        public string Symbol { get; set; } = string.Empty;
        public Dictionary<string, decimal> Momentum { get; set; } = new();
        public decimal Score { get; set; }
    }
}
