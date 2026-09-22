using System;
using System.Collections.Generic;

namespace CyberGame.Models.Entities;

public partial class PaymentMethod
{
    public int MethodId { get; set; }

    public string Name { get; set; } = null!;

    public string Code { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Recharge> Recharges { get; set; } = new List<Recharge>();
}
