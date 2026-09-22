using System;
using System.Collections.Generic;

namespace CyberGame.Models.Entities;

public partial class ChatMessage
{
    public int MessageId { get; set; }

    public int ChatSessionId { get; set; }

    public string Sender { get; set; } = null!;

    public string Content { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ChatSession ChatSession { get; set; } = null!;
}
