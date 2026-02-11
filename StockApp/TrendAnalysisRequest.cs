using System.ComponentModel.DataAnnotations;

namespace StockApp
{
    public class TrendAnalysisRequest
    {
        [Required]
        public List<string> Symbols { get; set; } = new List<string>();
        
        [Range(1, 1440)] // 1 minute to 24 hours
        public int WindowMinutes { get; set; } = 5;
    }
}
