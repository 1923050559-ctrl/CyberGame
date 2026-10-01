
namespace CyberGame.Models.ViewModels
{
    // Task #17 - Quản lý trạng thái máy
    public class AdminMachineRowViewModel
    {
        public int ComputerId { get; set; }
        public string ComputerName { get; set; } = null!;
        public int ZoneId { get; set; }
        public string ZoneName { get; set; } = null!;
        public string Status { get; set; } = null!;

        public string StatusLabel => Status switch
        {
            "available" => "Trống",
            "in_use" => "Đang sử dụng",
            "maintenance" => "Bảo trì",
            "offline" => "Ngừng hoạt động",
            _ => Status
        };
        public int? BookingId { get; set; }
        public string? BookingCustomer { get; set; }
        public string? BookingStatus { get; set; }      // pending | confirmed
        public DateTime? BookingStart { get; set; }
        public DateTime? HoldExpiresAt { get; set; }
    }
}