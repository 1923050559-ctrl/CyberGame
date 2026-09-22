using System;
using System.Collections.Generic;

namespace CyberGame.Models.Entities;

public partial class Booking
{
    public int BookingId { get; set; }

    public int UserId { get; set; }

    public int ComputerId { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public DateTime? HoldExpiresAt { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public string? CancelReason { get; set; }

    public virtual Computer Computer { get; set; } = null!;

    public virtual ICollection<FoodOrder> FoodOrders { get; set; } = new List<FoodOrder>();

    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();

    public virtual User User { get; set; } = null!;
}
