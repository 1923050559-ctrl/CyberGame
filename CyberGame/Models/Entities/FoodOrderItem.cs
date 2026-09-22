using System;
using System.Collections.Generic;

namespace CyberGame.Models.Entities;

public partial class FoodOrderItem
{
    public int OrderItemId { get; set; }

    public int OrderId { get; set; }

    public int FoodId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Subtotal { get; set; }

    public virtual Food Food { get; set; } = null!;

    public virtual FoodOrder Order { get; set; } = null!;
}
