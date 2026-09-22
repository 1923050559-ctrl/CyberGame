using System;
using System.Collections.Generic;

namespace CyberGame.Models.Entities;

public partial class KnowledgeBase
{
    public int KnowledgeId { get; set; }

    public string Question { get; set; } = null!;

    public string Answer { get; set; } = null!;

    public string? Category { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
