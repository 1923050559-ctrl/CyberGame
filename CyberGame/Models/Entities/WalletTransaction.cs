using System;
using System.Collections.Generic;

namespace CyberGame.Models.Entities;

public partial class WalletTransaction
{
    public int TransactionId { get; set; }

    public int UserId { get; set; }

    public string Type { get; set; } = null!;

    public decimal Amount { get; set; }

    public int? ReferenceId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
