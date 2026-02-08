namespace StockApp
{
    public class StockPrice
    {
        public DateTime Date { get; set; }
        public decimal Price { get; set; }
        public string Symbol { get; set; } = string.Empty;
    }
}
