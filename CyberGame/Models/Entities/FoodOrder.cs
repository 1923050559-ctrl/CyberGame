using System;
using System.Collections.Generic;

namespace CyberGame.Models.Entities;

public partial class FoodOrder
{
    public int OrderId { get; set; }

    public int UserId { get; set; }

    public int? BookingId { get; set; }

    public int? SessionId { get; set; }

    public string? SeatLabel { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public string Status { get; set; } = null!;

    public decimal TotalAmount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Booking? Booking { get; set; }

    public virtual ICollection<FoodOrderItem> FoodOrderItems { get; set; } = new List<FoodOrderItem>();

    public virtual Session? Session { get; set; }

    public virtual User User { get; set; } = null!;
}
