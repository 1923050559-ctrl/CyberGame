using System;
using System.Collections.Generic;

namespace CyberGame.Models.Entities;

public partial class SupportTicket
{
    public int TicketId { get; set; }

    public int ChatSessionId { get; set; }

    public int? AssignedStaff { get; set; }

    public string Subject { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual User? AssignedStaffNavigation { get; set; }

    public virtual ChatSession ChatSession { get; set; } = null!;
}
