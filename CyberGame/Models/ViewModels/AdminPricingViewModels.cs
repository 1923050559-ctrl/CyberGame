namespace CyberGame.Models.ViewModels
{
    // Task #20 - Quản lý bảng giá
    public class AdminPricingRowViewModel
    {
        public int ZoneId { get; set; }
        public string ZoneName { get; set; } = null!;
        public string? Specs { get; set; }
        public decimal PricePerHour { get; set; }
    }
}