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
    }

    public class CreateBookingDto
    {
        public string CustomerName { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public int NumberOfPeople { get; set; } = 1;
        public int ZoneId { get; set; }
        public int? ComputerId { get; set; }
        public DateTime StartTime { get; set; }
        public int DurationHours { get; set; } = 2;
        public string PaymentMethod { get; set; } = "counter";
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
}
