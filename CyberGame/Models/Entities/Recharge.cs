using System;
using System.Collections.Generic;

namespace CyberGame.Models.Entities;

public partial class Recharge
{
    public int RechargeId { get; set; }

    public int UserId { get; set; }

    public decimal Amount { get; set; }

    public int MethodId { get; set; }

    public string Status { get; set; } = null!;

    public string? TransactionCode { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual PaymentMethod Method { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
