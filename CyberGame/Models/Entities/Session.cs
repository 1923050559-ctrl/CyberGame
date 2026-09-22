using System;
using System.Collections.Generic;

namespace CyberGame.Models.Entities;

public partial class Session
{
    public int SessionId { get; set; }

    public int? BookingId { get; set; }

    public int UserId { get; set; }

    public int ComputerId { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public int? TotalMinutes { get; set; }

    public decimal? TotalCost { get; set; }

    public virtual Booking? Booking { get; set; }

    public virtual Computer Computer { get; set; } = null!;

    public virtual ICollection<FoodOrder> FoodOrders { get; set; } = new List<FoodOrder>();

    public virtual User User { get; set; } = null!;
}
