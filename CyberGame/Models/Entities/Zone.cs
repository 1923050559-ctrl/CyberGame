using System;
using System.Collections.Generic;

namespace CyberGame.Models.Entities;

public partial class Zone
{
    public int ZoneId { get; set; }

    public string ZoneName { get; set; } = null!;

    public string? Specs { get; set; }

    public decimal PricePerHour { get; set; }

    public string Status { get; set; } = null!;

    public virtual ICollection<Computer> Computers { get; set; } = new List<Computer>();
}
