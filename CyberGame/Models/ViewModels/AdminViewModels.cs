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
}
