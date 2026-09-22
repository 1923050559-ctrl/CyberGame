using System;
using System.Collections.Generic;

namespace CyberGame.Models.Entities;

public partial class ChatSession
{
    public int ChatSessionId { get; set; }

    public int? UserId { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? EndedAt { get; set; }

    public string Status { get; set; } = null!;

    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();

    public virtual SupportTicket? SupportTicket { get; set; }

    public virtual User? User { get; set; }
}
