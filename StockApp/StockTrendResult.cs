namespace StockApp
{
    public class StockTrendResult
    {
        public string Symbol { get; set; } = string.Empty;
        public List<StockPrice> Prices { get; set; } = new List<StockPrice>();
        public double Slope { get; set; }
    }
}
