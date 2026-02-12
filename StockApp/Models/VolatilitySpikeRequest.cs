namespace StockApp.Models
{
    public class VolatilitySpikeRequest
    {
        public List<string> Symbols { get; set; } = new();
        public int WindowMinutes { get; set; } = 5;
        public int BaselineMinutes { get; set; } = 60;
        public double SpikeThreshold { get; set; } = 2.0;
    }
}
