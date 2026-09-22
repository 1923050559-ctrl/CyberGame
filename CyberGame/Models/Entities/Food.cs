using System;
using System.Collections.Generic;

namespace CyberGame.Models.Entities;

public partial class Food
{
    public int FoodId { get; set; }

    public string Name { get; set; } = null!;

    public string Category { get; set; } = null!;

    public decimal Price { get; set; }

    public string? ImageUrl { get; set; }

    public string Status { get; set; } = null!;

    public virtual ICollection<FoodOrderItem> FoodOrderItems { get; set; } = new List<FoodOrderItem>();
}
