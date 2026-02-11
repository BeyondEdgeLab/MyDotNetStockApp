namespace StockApp.Models
{
    public class StockGrowthRequest
    {
        public List<string> Symbols { get; set; } = new();
        public int WindowMinutes { get; set; } = 5;
    }
}
