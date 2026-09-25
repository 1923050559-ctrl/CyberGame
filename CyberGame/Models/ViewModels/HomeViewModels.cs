using System;
using System.Collections.Generic;
using CyberGame.Models.Entities;

namespace CyberGame.Models.ViewModels
{
    public class HomeIndexViewModel
    {
        public int TotalZones { get; set; }
        public int TotalComputers { get; set; }
        public int TotalEquipment { get; set; }
        public List<Zone> Zones { get; set; } = new List<Zone>();
        public List<Food> FeaturedFoods { get; set; } = new List<Food>();
        public List<KnowledgeBase> NewsList { get; set; } = new List<KnowledgeBase>();
    }

    public class BookingPageViewModel
    {
        public List<Zone> Zones { get; set; } = new List<Zone>();
        public List<Computer> AvailableComputers { get; set; } = new List<Computer>();
        public List<PaymentMethod> PaymentMethods { get; set; } = new List<PaymentMethod>();
        public List<Food> Foods { get; set; } = new List<Food>();
    }

    public class CreateBookingDto
    {
        public string CustomerName { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public int NumberOfPeople { get; set; } = 1;
        public int ZoneId { get; set; }
        public int? ComputerId { get; set; }
        public List<int>? ComputerIds { get; set; }
        public List<string>? ComputerNames { get; set; }
        public DateTime StartTime { get; set; }
        public int DurationHours { get; set; } = 2;
        public string PaymentMethod { get; set; } = "counter";
        public List<CartItemDto>? FoodItems { get; set; }
    }

    public class CartItemDto
    {
        public int FoodId { get; set; }
        public string Name { get; set; } = null!;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }

    public class CreateFoodOrderDto
    {
        public string SeatNumber { get; set; } = null!;
        public string PaymentMethod { get; set; } = "cash";
        public List<CartItemDto> Items { get; set; } = new List<CartItemDto>();
    }

    public class SendChatMessageDto
    {
        public string Content { get; set; } = null!;
    }

    public class ChatMessageItemDto
    {
        public int MessageId { get; set; }
        public string Sender { get; set; } = null!; // "user", "staff", "bot"
        public string Content { get; set; } = null!;
        public string Time { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }

    public class ChatHistoryResponseDto
    {
        public bool Success { get; set; }
        public int SessionId { get; set; }
        public string Status { get; set; } = "open";
        public bool HasTicket { get; set; }
        public string? TicketStatus { get; set; }
        public string? WaitingReason { get; set; }
        public List<ChatMessageItemDto> Messages { get; set; } = new List<ChatMessageItemDto>();
    }
}
