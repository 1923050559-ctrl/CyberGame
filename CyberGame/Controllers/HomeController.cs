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
                Zones = await _context.Zones
                    .Include(z => z.Computers)
                    .Where(z => z.Status == "active")
                    .ToListAsync(),
                AvailableComputers = await _context.Computers
                    .Where(c => c.Status == "available")
                    .Include(c => c.Zone)
                    .ToListAsync(),
                PaymentMethods = await _context.PaymentMethods.Where(p => p.IsActive).ToListAsync(),
                Foods = await _context.Foods.Where(f => f.Status == "available").ToListAsync()
            };
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> GetZoneComputers(int zoneId)
        {
            var computers = await _context.Computers
                .Where(c => c.ZoneId == zoneId)
                .OrderBy(c => c.ComputerName)
                .Select(c => new
                {
                    id = c.ComputerId,
                    name = c.ComputerName,
                    status = c.Status
                })
                .ToListAsync();

            return Json(computers);
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
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0 && User.Identity?.IsAuthenticated == true)
                {
                    var idClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    if (int.TryParse(idClaim, out int parsedId)) userId = parsedId;
                }

                if (userId <= 0)
                {
                    return Json(new { success = false, requireLogin = true, message = "Bạn cần đăng nhập tài khoản để thực hiện đặt máy." });
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, requireLogin = true, message = "Tài khoản không tồn tại hoặc phiên đăng nhập đã hết hạn." });
                }

                // Lấy danh sách ID máy được chọn
                var selectedCompIds = new List<int>();

                if (dto.ComputerIds != null && dto.ComputerIds.Any())
                {
                    selectedCompIds.AddRange(dto.ComputerIds.Where(id => id > 0));
                }
                else if (dto.ComputerNames != null && dto.ComputerNames.Any())
                {
                    var compsByName = await _context.Computers
                        .Where(c => c.ZoneId == dto.ZoneId && dto.ComputerNames.Contains(c.ComputerName))
                        .Select(c => c.ComputerId)
                        .ToListAsync();
                    selectedCompIds.AddRange(compsByName);
                }
                else if (dto.ComputerId.HasValue && dto.ComputerId.Value > 0)
                {
                    selectedCompIds.Add(dto.ComputerId.Value);
                }

                // Nếu chưa có máy chỉ định, tự động gán máy rảnh thuộc khu vực
                int people = dto.NumberOfPeople > 0 ? dto.NumberOfPeople : 1;
                if (!selectedCompIds.Any())
                {
                    var availableComps = await _context.Computers
                        .Where(c => c.ZoneId == dto.ZoneId && c.Status == "available")
                        .Take(people)
                        .Select(c => c.ComputerId)
                        .ToListAsync();

                    if (availableComps.Any())
                    {
                        selectedCompIds.AddRange(availableComps);
                    }
                    else
                    {
                        var anyComp = await _context.Computers.FirstOrDefaultAsync(c => c.ZoneId == dto.ZoneId);
                        if (anyComp != null) selectedCompIds.Add(anyComp.ComputerId);
                    }
                }

                var startTime = dto.StartTime == default ? DateTime.Now : dto.StartTime;
                var endTime = startTime.AddHours(dto.DurationHours > 0 ? dto.DurationHours : 2);

                var createdBookings = new List<Booking>();
                foreach (var cId in selectedCompIds)
                {
                    var booking = new Booking
                    {
                        UserId = userId,
                        ComputerId = cId,
                        StartTime = startTime,
                        EndTime = endTime,
                        Status = "confirmed",
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Bookings.Add(booking);
                    createdBookings.Add(booking);
                }

                await _context.SaveChangesAsync();

                // Lưu đơn đồ ăn/tiện ích đi kèm nếu có
                if (dto.FoodItems != null && dto.FoodItems.Any())
                {
                    var compNames = await _context.Computers
                        .Where(c => selectedCompIds.Contains(c.ComputerId))
                        .Select(c => c.ComputerName)
                        .ToListAsync();

                    decimal foodTotal = dto.FoodItems.Sum(i => i.Price * i.Quantity);
                    var foodOrder = new FoodOrder
                    {
                        UserId = userId,
                        SeatLabel = string.Join(", ", compNames),
                        PaymentMethod = dto.PaymentMethod,
                        Status = "pending",
                        TotalAmount = foodTotal,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.FoodOrders.Add(foodOrder);
                    await _context.SaveChangesAsync();

                    foreach (var fItem in dto.FoodItems)
                    {
                        _context.FoodOrderItems.Add(new FoodOrderItem
                        {
                            OrderId = foodOrder.OrderId,
                            FoodId = fItem.FoodId,
                            Quantity = fItem.Quantity,
                            UnitPrice = fItem.Price
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                var bookedComputers = await _context.Computers
                    .Where(c => selectedCompIds.Contains(c.ComputerId))
                    .Select(c => c.ComputerName)
                    .ToListAsync();

                var primaryBooking = createdBookings.FirstOrDefault();
                string displaySeats = bookedComputers.Any() ? string.Join(", ", bookedComputers) : "#" + (dto.ComputerId ?? 0);

                return Json(new
                {
                    success = true,
                    bookingId = primaryBooking?.BookingId ?? 0,
                    bookingIds = createdBookings.Select(b => b.BookingId).ToList(),
                    computerId = primaryBooking?.ComputerId ?? 0,
                    computerName = displaySeats,
                    startTime = startTime.ToString("dd/MM/yyyy HH:mm"),
                    endTime = endTime.ToString("dd/MM/yyyy HH:mm"),
                    message = $"Đặt chỗ thành công máy: {displaySeats}! Mã đơn: #{primaryBooking?.BookingId}"
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
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0 && User.Identity?.IsAuthenticated == true)
                {
                    var idClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    if (int.TryParse(idClaim, out int parsedId)) userId = parsedId;
                }

                if (userId <= 0)
                {
                    return Json(new { success = false, requireLogin = true, message = "Bạn cần đăng nhập tài khoản để thực hiện đặt món." });
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, requireLogin = true, message = "Tài khoản không tồn tại hoặc phiên đăng nhập đã hết hạn." });
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

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int uid))
            {
                return uid;
            }
            return HttpContext.Session.GetInt32("UserId");
        }

        [HttpGet]
        public async Task<IActionResult> GetChatHistory()
        {
            var currentUserId = GetCurrentUserId();
            ChatSession? session = null;

            if (currentUserId.HasValue)
            {
                session = await _context.ChatSessions
                    .Include(s => s.ChatMessages)
                    .Include(s => s.SupportTicket)
                    .Where(s => s.UserId == currentUserId.Value && s.Status == "open")
                    .OrderByDescending(s => s.StartedAt)
                    .FirstOrDefaultAsync();

                if (session == null)
                {
                    session = await _context.ChatSessions
                        .Include(s => s.ChatMessages)
                        .Include(s => s.SupportTicket)
                        .Where(s => s.UserId == currentUserId.Value)
                        .OrderByDescending(s => s.StartedAt)
                        .FirstOrDefaultAsync();

                    if (session == null)
                    {
                        session = new ChatSession
                        {
                            UserId = currentUserId.Value,
                            StartedAt = DateTime.UtcNow,
                            Status = "open"
                        };
                        _context.ChatSessions.Add(session);
                        await _context.SaveChangesAsync();
                    }
                    else if (session.Status != "open")
                    {
                        session.Status = "open";
                        await _context.SaveChangesAsync();
                    }
                }
            }
            else
            {
                int? guestSessionId = HttpContext.Session.GetInt32("GuestChatSessionId");
                if (guestSessionId.HasValue)
                {
                    session = await _context.ChatSessions
                        .Include(s => s.ChatMessages)
                        .Include(s => s.SupportTicket)
                        .FirstOrDefaultAsync(s => s.ChatSessionId == guestSessionId.Value);
                }

                if (session == null)
                {
                    session = new ChatSession
                    {
                        UserId = null,
                        StartedAt = DateTime.UtcNow,
                        Status = "open"
                    };
                    _context.ChatSessions.Add(session);
                    await _context.SaveChangesAsync();
                    HttpContext.Session.SetInt32("GuestChatSessionId", session.ChatSessionId);
                }
            }

            var messages = await _context.ChatMessages
                .Where(m => m.ChatSessionId == session.ChatSessionId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            var ticket = await _context.SupportTickets
                .FirstOrDefaultAsync(t => t.ChatSessionId == session.ChatSessionId);

            return Json(new ChatHistoryResponseDto
            {
                Success = true,
                SessionId = session.ChatSessionId,
                Status = session.Status,
                HasTicket = ticket != null,
                TicketStatus = ticket?.Status,
                WaitingReason = (ticket != null && ticket.Status == "open") ? ticket.Subject : null,
                Messages = messages.Select(m => new ChatMessageItemDto
                {
                    MessageId = m.MessageId,
                    Sender = m.Sender,
                    Content = m.Content,
                    Time = m.CreatedAt.ToLocalTime().ToString("HH:mm"),
                    CreatedAt = m.CreatedAt
                }).ToList()
            });
        }

        [HttpPost]
        public async Task<IActionResult> SendChatMessage([FromBody] SendChatMessageDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Content))
            {
                return Json(new { success = false, message = "Nội dung tin nhắn không được để trống." });
            }

            var clean = dto.Content.Trim();
            var currentUserId = GetCurrentUserId();
            ChatSession? session = null;

            if (currentUserId.HasValue)
            {
                session = await _context.ChatSessions
                    .Include(s => s.SupportTicket)
                    .Where(s => s.UserId == currentUserId.Value && s.Status == "open")
                    .OrderByDescending(s => s.StartedAt)
                    .FirstOrDefaultAsync();

                if (session == null)
                {
                    session = new ChatSession
                    {
                        UserId = currentUserId.Value,
                        StartedAt = DateTime.UtcNow,
                        Status = "open"
                    };
                    _context.ChatSessions.Add(session);
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                int? guestSessionId = HttpContext.Session.GetInt32("GuestChatSessionId");
                if (guestSessionId.HasValue)
                {
                    session = await _context.ChatSessions
                        .Include(s => s.SupportTicket)
                        .FirstOrDefaultAsync(s => s.ChatSessionId == guestSessionId.Value);
                }

                if (session == null)
                {
                    session = new ChatSession
                    {
                        UserId = null,
                        StartedAt = DateTime.UtcNow,
                        Status = "open"
                    };
                    _context.ChatSessions.Add(session);
                    await _context.SaveChangesAsync();
                    HttpContext.Session.SetInt32("GuestChatSessionId", session.ChatSessionId);
                }
            }

            // 1. Lưu tin nhắn của khách vào DB
            var userMsg = new ChatMessage
            {
                ChatSessionId = session.ChatSessionId,
                Sender = "user",
                Content = clean,
                CreatedAt = DateTime.UtcNow
            };
            _context.ChatMessages.Add(userMsg);
            await _context.SaveChangesAsync();

            // 2. Logic kiểm tra Bot FAQ hoặc Handover Ticket sang Admin
            var lower = clean.ToLower();
            string? botReply = null;
            bool isHandover = false;

            bool asksForHuman = lower.Contains("gặp admin") || lower.Contains("gặp nhân viên") ||
                                lower.Contains("admin ơi") || lower.Contains("hỗ trợ kỹ thuật") ||
                                lower.Contains("lỗi máy") || lower.Contains("máy hỏng") ||
                                lower.Contains("mất mạng") || lower.Contains("kẹt tiền") ||
                                lower.Contains("ticket") || lower.Contains("cần hỗ trợ") ||
                                lower.Contains("gọi người");

            if (asksForHuman)
            {
                isHandover = true;
                botReply = "🤖 CyberGame Bot đã ghi nhận yêu cầu và tạo Phiếu Hỗ Trợ (Ticket) chuyển tiếp tới Quản Trị Viên! Nhân viên phụ trách sẽ phản hồi trực tiếp cho bạn tại đây ngay nhé.";
            }
            else
            {
                var kb = await _context.KnowledgeBases
                    .Where(k => k.Status == "active")
                    .FirstOrDefaultAsync(k => lower.Contains(k.Question.ToLower()) || k.Question.ToLower().Contains(lower));

                if (kb != null)
                {
                    botReply = kb.Answer;
                }
                else if (lower.Contains("giá") || lower.Contains("bao nhiêu") || lower.Contains("tiền") || lower.Contains("bảng giá"))
                {
                    botReply = "Bảng giá phòng máy: Standard Zone 10.000đ/h, VIP Zone 15.000đ/h, Pro Stage 20.000đ/h và Stream Room 35.000đ/h bạn nhé!";
                }
                else if (lower.Contains("đặt") || lower.Contains("booking") || lower.Contains("giữ chỗ"))
                {
                    botReply = "Bạn có thể vào mục ĐẶT MÁY trên thanh điều hướng để chọn khu vực, sơ đồ máy và thời gian chơi mong muốn nhé.";
                }
                else if (lower.Contains("cấu hình") || lower.Contains("máy") || lower.Contains("gear") || lower.Contains("màn hình"))
                {
                    botReply = "Toàn bộ dàn máy trang bị GPU RTX 3060 - RTX 4080, màn hình 144Hz - 240Hz cực mượt và gear Razer/Logitech cao cấp.";
                }
                else if (lower.Contains("nạp tiền") || lower.Contains("nạp ví") || lower.Contains("chuyển khoản"))
                {
                    botReply = "Bạn có thể nạp tiền tài khoản trực tiếp tại quầy thu ngân hoặc báo với Admin qua tin nhắn để được hỗ trợ nạp qua mã QR nhé.";
                }
                else if (lower.Contains("món") || lower.Contains("đồ ăn") || lower.Contains("nước") || lower.Contains("menu"))
                {
                    botReply = "Menu Bếp F&B phục vụ đầy đủ cơm rang, mì xào, bò né, trà sữa, sting,... Bạn vào tab BẾP F&B để gọi món trực tiếp lên máy nhé!";
                }
                else if (lower.Contains("chào") || lower.Contains("hello") || lower.Contains("hi") || lower.Contains("alo"))
                {
                    botReply = "CyberGame xin chào bạn! Mình là Trợ Lý Tự Động. Bạn đang cần hỗ trợ về giá máy, đặt chỗ, menu F&B hay cần gặp Quản Trị Viên ạ?";
                }
                else
                {
                    // Fallback tự động gửi Ticket cho Admin
                    isHandover = true;
                    botReply = "🤖 Câu hỏi của bạn chưa có trong danh mục tự động. Tôi đã chuyển đoạn chat này thành Ticket hỗ trợ gửi trực tiếp tới Quản Trị Viên. Nhân viên sẽ hỗ trợ bạn tại đây trong giây lát!";
                }
            }

            bool requireLogin = isHandover && !currentUserId.HasValue;

            if (requireLogin)
            {
                // Chưa đăng nhập thì không tạo ticket ẩn danh, yêu cầu đăng nhập
                isHandover = false;
                botReply = "🤖 Yêu cầu của bạn cần chuyển tiếp Phiếu Hỗ Trợ (Ticket) cho Quản Trị Viên. Vui lòng đăng nhập tài khoản CyberGame để gửi ticket và nhận phản hồi riêng tư từ Admin.";
            }

            var botMsg = new ChatMessage
            {
                ChatSessionId = session.ChatSessionId,
                Sender = "bot",
                Content = botReply,
                CreatedAt = DateTime.UtcNow.AddMilliseconds(500)
            };
            _context.ChatMessages.Add(botMsg);

            if (isHandover)
            {
                var ticket = await _context.SupportTickets.FirstOrDefaultAsync(t => t.ChatSessionId == session.ChatSessionId);
                if (ticket == null)
                {
                    ticket = new SupportTicket
                    {
                        ChatSessionId = session.ChatSessionId,
                        Subject = clean.Length > 200 ? clean.Substring(0, 197) + "..." : clean,
                        Status = "open",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.SupportTickets.Add(ticket);
                }
                else
                {
                    ticket.Status = "open";
                    ticket.Subject = clean.Length > 200 ? clean.Substring(0, 197) + "..." : clean;
                    ticket.CreatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                requireLogin = requireLogin,
                userMessage = new ChatMessageItemDto
                {
                    MessageId = userMsg.MessageId,
                    Sender = userMsg.Sender,
                    Content = userMsg.Content,
                    Time = userMsg.CreatedAt.ToLocalTime().ToString("HH:mm"),
                    CreatedAt = userMsg.CreatedAt
                },
                botMessage = new ChatMessageItemDto
                {
                    MessageId = botMsg.MessageId,
                    Sender = botMsg.Sender,
                    Content = botMsg.Content,
                    Time = botMsg.CreatedAt.ToLocalTime().ToString("HH:mm"),
                    CreatedAt = botMsg.CreatedAt
                },
                isHandover = isHandover
            });
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
