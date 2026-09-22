using System;
using System.Collections.Generic;

namespace CyberGame.Models.Entities;

public partial class User
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public decimal Balance { get; set; }

    public string Role { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual ICollection<ChatSession> ChatSessions { get; set; } = new List<ChatSession>();

    public virtual ICollection<FoodOrder> FoodOrders { get; set; } = new List<FoodOrder>();

    public virtual ICollection<Recharge> Recharges { get; set; } = new List<Recharge>();

    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();

    public virtual ICollection<SupportTicket> SupportTickets { get; set; } = new List<SupportTicket>();

    public virtual ICollection<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();
}
