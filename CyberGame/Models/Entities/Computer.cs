using System;
using System.Collections.Generic;

namespace CyberGame.Models.Entities;

public partial class Computer
{
    public int ComputerId { get; set; }

    public int ZoneId { get; set; }

    public string ComputerName { get; set; } = null!;

    public string Status { get; set; } = null!;

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual ICollection<Equipment> Equipment { get; set; } = new List<Equipment>();

    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();

    public virtual Zone Zone { get; set; } = null!;
}
