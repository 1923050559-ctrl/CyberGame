using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using CyberGame.Models;
using CyberGame.Models.Entities;
using CyberGame.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CyberGame.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly CyberGameDbContext _context;

        public HomeController(ILogger<HomeController> logger, CyberGameDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var vm = new HomeIndexViewModel
            {
                TotalZones = await _context.Zones.CountAsync(),
                TotalComputers = await _context.Computers.CountAsync(),
                TotalEquipment = await _context.Equipment.CountAsync(),
                Zones = await _context.Zones.Include(z => z.Computers).ToListAsync(),
                FeaturedFoods = await _context.Foods.Where(f => f.Status == "available").Take(8).ToListAsync(),
                NewsList = await _context.KnowledgeBases.Where(k => k.Status == "active").OrderByDescending(k => k.CreatedAt).Take(6).ToListAsync()
            };

            return View(vm);
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Games()
        {
            return View();
        }

        public async Task<IActionResult> Cyber()
        {
            var zones = await _context.Zones
                .Include(z => z.Computers)
                .ToListAsync();
            return View(zones);
        }

        public async Task<IActionResult> CyberDetail(int id = 1)
        {
            var zone = await _context.Zones
                .Include(z => z.Computers)
                .FirstOrDefaultAsync(z => z.ZoneId == id);

            if (zone == null)
            {
                zone = await _context.Zones
                    .Include(z => z.Computers)
                    .FirstOrDefaultAsync();
            }

            var computerIds = zone?.Computers.Select(c => c.ComputerId).ToList() ?? new List<int>();
            var equipment = await _context.Equipment
                .Where(e => computerIds.Contains(e.ComputerId))
                .ToListAsync();

            ViewBag.Equipment = equipment;
            return View(zone);
        }

        public async Task<IActionResult> Booking()
        {
            var vm = new BookingPageViewModel
            {
                Zones = await _context.Zones.Include(z => z.Computers).ToListAsync(),
                AvailableComputers = await _context.Computers
                    .Where(c => c.Status == "available")
                    .Include(c => c.Zone)
                    .ToListAsync(),
                PaymentMethods = await _context.PaymentMethods.Where(p => p.IsActive).ToListAsync()
            };
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.CustomerName) || string.IsNullOrWhiteSpace(dto.Phone))
            {
                return Json(new { success = false, message = "Vui lòng nhập đầy đủ họ tên và số điện thoại." });
            }

            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 1;
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    user = await _context.Users.FirstOrDefaultAsync();
                    userId = user?.UserId ?? 1;
                }

                int computerId = dto.ComputerId ?? 0;
                if (computerId <= 0)
                {
                    var availableComp = await _context.Computers
                        .FirstOrDefaultAsync(c => c.ZoneId == dto.ZoneId && c.Status == "available");
                    if (availableComp != null)
                    {
                        computerId = availableComp.ComputerId;
                    }
                    else
                    {
                        var anyComp = await _context.Computers.FirstOrDefaultAsync(c => c.ZoneId == dto.ZoneId);
                        computerId = anyComp?.ComputerId ?? 1;
                    }
                }

                var startTime = dto.StartTime == default ? DateTime.Now : dto.StartTime;
                var endTime = startTime.AddHours(dto.DurationHours > 0 ? dto.DurationHours : 2);

                var booking = new Booking
                {
                    UserId = userId,
                    ComputerId = computerId,
                    StartTime = startTime,
                    EndTime = endTime,
                    Status = "confirmed",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    bookingId = booking.BookingId,
                    computerId = computerId,
                    startTime = startTime.ToString("dd/MM/yyyy HH:mm"),
                    endTime = endTime.ToString("dd/MM/yyyy HH:mm"),
                    message = "Đặt chỗ thành công! Mã đơn của bạn là #" + booking.BookingId
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi lưu đặt chỗ: " + ex.Message });
            }
        }

        public async Task<IActionResult> Menu()
        {
            var foods = await _context.Foods.ToListAsync();
            return View(foods);
        }

        [HttpPost]
        public async Task<IActionResult> CreateFoodOrder([FromBody] CreateFoodOrderDto dto)
        {
            if (dto == null || dto.Items == null || !dto.Items.Any())
            {
                return Json(new { success = false, message = "Giỏ hàng đang trống." });
            }

            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 1;
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    user = await _context.Users.FirstOrDefaultAsync();
                    userId = user?.UserId ?? 1;
                }

                decimal total = dto.Items.Sum(i => i.Price * i.Quantity);

                var order = new FoodOrder
                {
                    UserId = userId,
                    SeatLabel = dto.SeatNumber,
                    PaymentMethod = dto.PaymentMethod,
                    Status = "pending",
                    TotalAmount = total,
                    CreatedAt = DateTime.UtcNow
                };

                _context.FoodOrders.Add(order);
                await _context.SaveChangesAsync();

                foreach (var item in dto.Items)
                {
                    var orderItem = new FoodOrderItem
                    {
                        OrderId = order.OrderId,
                        FoodId = item.FoodId,
                        Quantity = item.Quantity,
                        UnitPrice = item.Price
                    };
                    _context.FoodOrderItems.Add(orderItem);
                }
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    orderId = order.OrderId,
                    total = total,
                    message = "Đặt món thành công! Mã đơn #" + order.OrderId
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi tạo đơn hàng: " + ex.Message });
            }
        }

        public async Task<IActionResult> News()
        {
            var news = await _context.KnowledgeBases
                .Where(k => k.Status == "active")
                .OrderByDescending(k => k.CreatedAt)
                .ToListAsync();
            return View(news);
        }

        public async Task<IActionResult> NewsDetail(int id = 1)
        {
            var article = await _context.KnowledgeBases.FindAsync(id);
            if (article == null)
            {
                article = await _context.KnowledgeBases.FirstOrDefaultAsync();
            }

            ViewBag.RelatedArticles = await _context.KnowledgeBases
                .Where(k => k.KnowledgeId != id && k.Status == "active")
                .Take(4)
                .ToListAsync();

            return View(article);
        }

        [HttpGet]
        public async Task<IActionResult> TestDbConnection()
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                var userCount = await _context.Users.CountAsync();
                var foodCount = await _context.Foods.CountAsync();
                var zoneCount = await _context.Zones.CountAsync();
                var computerCount = await _context.Computers.CountAsync();
                var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Role == "admin");

                return Json(new
                {
                    success = canConnect,
                    database = "CyberGameDB",
                    server = "localhost",
                    user = "sa",
                    counts = new
                    {
                        users = userCount,
                        foods = foodCount,
                        zones = zoneCount,
                        computers = computerCount
                    },
                    adminUser = adminUser == null ? null : new
                    {
                        adminUser.UserId,
                        adminUser.Username,
                        adminUser.Email,
                        adminUser.Role,
                        adminUser.Balance,
                        adminUser.Status
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetFoods()
        {
            var foods = await _context.Foods.ToListAsync();
            return Json(foods);
        }

        [HttpGet]
        public async Task<IActionResult> GetZones()
        {
            var zones = await _context.Zones
                .Select(z => new
                {
                    z.ZoneId,
                    z.ZoneName,
                    z.PricePerHour,
                    z.Specs,
                    z.Status,
                    ComputerCount = z.Computers.Count
                })
                .ToListAsync();
            return Json(zones);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
