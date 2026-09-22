using System;
using System.Collections.Generic;

namespace CyberGame.Models.Entities;

public partial class Equipment
{
    public int EquipmentId { get; set; }

    public int ComputerId { get; set; }

    public string EquipmentName { get; set; } = null!;

    public string ConditionStatus { get; set; } = null!;

    public virtual Computer Computer { get; set; } = null!;
}
