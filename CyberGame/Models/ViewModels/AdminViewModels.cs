using System;
using System.Collections.Generic;
using CyberGame.Models.Entities;

namespace CyberGame.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public decimal TotalRevenue { get; set; }
        public decimal BookingRevenue { get; set; }
        public decimal FnbRevenue { get; set; }
        public int TotalComputers { get; set; }
        public int ActiveComputers { get; set; }
        public int MaintenanceComputers { get; set; }
        public int TotalUsers { get; set; }
        public int PendingOrdersCount { get; set; }
        public List<Zone> Zones { get; set; } = new List<Zone>();
        public List<FoodOrder> RecentOrders { get; set; } = new List<FoodOrder>();
        public List<Food> TopFoods { get; set; } = new List<Food>();
    }

    public class TopUpDto
    {
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public string? Note { get; set; }
    }

    public class CreateCustomerDto
    {
        public string Username { get; set; } = null!;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string Password { get; set; } = "123456";
        public decimal InitialBalance { get; set; } = 0;
    }

    public class AdminReplyDto
    {
        public int ChatSessionId { get; set; }
        public string Content { get; set; } = null!;
    }

    public class ToggleTicketStatusDto
    {
        public int ChatSessionId { get; set; }
        public string? Status { get; set; }
    }

    public class CreateAdminChatDto
    {
        public string Zone { get; set; } = "vip";
        public string Seat { get; set; } = null!;
        public string Content { get; set; } = null!;
    }

    public class AdminConversationDto
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string CustomerName { get; set; } = null!;
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Seat { get; set; } = "Khách Web";
        public decimal Balance { get; set; }
        public string AvatarText { get; set; } = "KH";
        public bool IsOnline { get; set; }
        public string Status { get; set; } = "bot_active"; // waiting_admin | resolved | bot_active
        public string? TicketStatus { get; set; }
        public string? WaitingReason { get; set; }
        public string UpdatedAt { get; set; } = "";
        public List<AdminMessageDto> Messages { get; set; } = new List<AdminMessageDto>();
    }

    public class AdminMessageDto
    {
        public int Id { get; set; }
        public string Sender { get; set; } = null!; // customer | admin | bot
        public string Text { get; set; } = null!;
        public string Time { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
