namespace StockApp
{
    public class TrendAnalysisRequest
    {
        public List<string> Symbols { get; set; } = new List<string>();
        public int WindowMinutes { get; set; } = 5;
    }
}
