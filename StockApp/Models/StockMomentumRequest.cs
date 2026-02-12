namespace StockApp.Models
{
    public class StockMomentumRequest
    {
        public List<string> Symbols { get; set; } = new();
        public List<int> WindowsMinutes { get; set; } = new();
        public List<decimal>? Weights { get; set; }
    }
}
