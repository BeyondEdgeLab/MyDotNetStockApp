namespace StockApp.Models
{
    public class StockPriceResponse
    {
        public string Symbol { get; set; } = string.Empty;
        public List<PricePoint> Prices { get; set; } = new();
    }

    public class PricePoint
    {
        public decimal Price { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }
}
