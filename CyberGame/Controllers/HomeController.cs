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
        private const int HoldMinutes = 30;      // giữ chỗ 30 phút sau giờ hẹn
        private const int MaxAdvanceHours = 3;   // chỉ được đặt trước tối đa 3 tiếng
        private IQueryable<Booking> OverlappingBookings(DateTime start, DateTime end, DateTime now) =>
    _context.Bookings.Where(b =>
        (b.Status == "pending" || b.Status == "confirmed")
        && (b.Status == "confirmed" || b.HoldExpiresAt == null || b.HoldExpiresAt > now)
        && b.StartTime < end
        && (b.EndTime == null || b.EndTime > start));

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
            int? currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId == null && User.Identity?.IsAuthenticated == true)
            {
                var idClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(idClaim, out int parsedId)) currentUserId = parsedId;
            }

            decimal userBalance = 0;
            bool isUserLoggedIn = false;
            string currentUsername = string.Empty;
            if (currentUserId.HasValue)
            {
                var currentUser = await _context.Users.FindAsync(currentUserId.Value);
                if (currentUser != null)
                {
                    userBalance = currentUser.Balance;
                    isUserLoggedIn = true;
                    currentUsername = currentUser.Username;
                }
            }

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
                Foods = await _context.Foods.Where(f => f.Status == "available").ToListAsync(),
                CurrentUserBalance = userBalance,
                IsUserLoggedIn = isUserLoggedIn,
                CurrentUsername = currentUsername
            };
            var nowB = DateTime.Now;
            ViewBag.ActiveBookings = await _context.Bookings
                .Where(b => (b.Status == "pending" || b.Status == "confirmed")
                         && b.EndTime > nowB
                         && (b.Status == "confirmed" || b.HoldExpiresAt > nowB))
                .Select(b => new { b.ComputerId, b.StartTime, b.EndTime })
                .ToListAsync();
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> GetZoneComputers(int zoneId)
        {
            var now = DateTime.Now;
            var rows = await _context.Computers
                .Where(c => c.ZoneId == zoneId)
                .OrderBy(c => c.ComputerName)
                .Select(c => new
                {
                    id = c.ComputerId,
                    name = c.ComputerName,
                    status = c.Status,
                    nextBookingAt = c.Bookings
                        .Where(b => (b.Status == "pending" || b.Status == "confirmed")
                                 && b.EndTime > now
                                 && (b.Status == "confirmed" || b.HoldExpiresAt > now))
                        .OrderBy(b => b.StartTime)
                        .Select(b => (DateTime?)b.StartTime)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Json(rows.Select(r => new
            {
                r.id,
                r.name,
                r.status,
                nextBookingAt = r.nextBookingAt?.ToString("yyyy-MM-ddTHH:mm:ss"),
                nextBookingLabel = r.nextBookingAt?.ToString("HH:mm")   // vd "11:00"
            }));
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
                var now = DateTime.Now;
                var startTime = dto.StartTime == default
                ? now
                : (dto.StartTime.Kind == DateTimeKind.Utc ? dto.StartTime.ToLocalTime() : dto.StartTime);
                if (startTime < now.AddMinutes(-5))
                    return Json(new { success = false, message = "Giờ đặt không được ở trong quá khứ." });
                if (startTime > now.AddHours(MaxAdvanceHours))
                    return Json(new { success = false, message = $"Chỉ được đặt trước tối đa {MaxAdvanceHours} tiếng." });

                var endTime = startTime.AddHours(dto.DurationHours > 0 ? dto.DurationHours : 2);
                bool startsNow = startTime <= now.AddMinutes(5);
                // Nếu chưa có máy chỉ định, tự động gán máy rảnh thuộc khu vực
                int people = dto.NumberOfPeople > 0 ? dto.NumberOfPeople : 1;
                if (!selectedCompIds.Any())
                {
                    var busyIds = await OverlappingBookings(startTime, endTime, now)
                        .Select(b => b.ComputerId).Distinct().ToListAsync();

                    var freeComps = await _context.Computers
                        .Where(c => c.ZoneId == dto.ZoneId
                                 && c.Status != "maintenance" && c.Status != "offline"
                                 && (!startsNow || c.Status == "available")
                                 && !busyIds.Contains(c.ComputerId))
                        .Take(people)
                        .Select(c => c.ComputerId)
                        .ToListAsync();

                    if (freeComps.Count < people)
                        return Json(new { success = false, message = "Khu vực này không đủ máy trống trong khung giờ bạn chọn." });

                    selectedCompIds.AddRange(freeComps);
                }


                selectedCompIds = selectedCompIds.Distinct().ToList();
                if (!selectedCompIds.Any())
                    return Json(new { success = false, message = "Vui lòng chọn máy." });

                await using var tx = await _context.Database.BeginTransactionAsync();

                // "Khoá" các dòng máy: request đến sau phải chờ request trước commit xong.
                // Đồng thời loại máy bảo trì/offline, và máy đang bận nếu đặt sát giờ.
                int locked = await _context.Computers
                    .Where(c => selectedCompIds.Contains(c.ComputerId)
                             && c.Status != "maintenance" && c.Status != "offline"
                             && (!startsNow || c.Status == "available"))
                    .ExecuteUpdateAsync(s => s.SetProperty(c => c.Status, c => c.Status));

                if (locked != selectedCompIds.Count)
                    return Json(new { success = false, message = "Có máy đang bảo trì hoặc đang được sử dụng, vui lòng chọn máy khác." });

                bool conflict = await OverlappingBookings(startTime, endTime, now)
                    .AnyAsync(b => selectedCompIds.Contains(b.ComputerId));
                if (conflict)
                    return Json(new { success = false, message = "Máy đã có người đặt trong khung giờ này, vui lòng chọn máy hoặc giờ khác."
                    
                    });
                // Tính toán tiền máy & tiền cọc tối thiểu 30 phút
                decimal zonePrice = 10000;
                var targetZone = await _context.Zones.FindAsync(dto.ZoneId);
                if (targetZone == null && selectedCompIds.Any())
                {
                    var comp = await _context.Computers.Include(c => c.Zone).FirstOrDefaultAsync(c => c.ComputerId == selectedCompIds.First());
                    if (comp?.Zone != null)
                    {
                        targetZone = comp.Zone;
                    }
                }
                if (targetZone != null) zonePrice = targetZone.PricePerHour;

                int compCount = selectedCompIds.Count > 0 ? selectedCompIds.Count : 1;
                decimal durationHours = dto.DurationHours > 0 ? dto.DurationHours : 2;
                decimal bookingFee = durationHours * zonePrice * compCount;
                decimal foodFee = (dto.FoodItems != null && dto.FoodItems.Any()) ? dto.FoodItems.Sum(i => i.Price * i.Quantity) : 0;
                decimal totalRequired = bookingFee + foodFee;

                // Quy định giữ chỗ: Cọc ít nhất 30 phút (0.5 giờ) tiền máy + tiền đồ ăn (nếu có)
                decimal minDepositRequired = (0.5m * zonePrice * compCount) + foodFee;

                var payMethod = dto.PaymentMethod?.Trim().ToUpper() ?? "WALLET";
                if (payMethod == "COUNTER") payMethod = "CASH";
                if (payMethod == "TRANSFER") payMethod = "BANK_QR";

                decimal deductedAmount = 0;
                bool isFullPayment = false;

                // Kiểm tra số dư tài khoản của người dùng:
                // Nếu số dư của người đó ĐỦ tiền cọc tối thiểu 30 phút -> Trừ vào số dư ví
                if (user.Balance >= minDepositRequired)
                {
                    if (!dto.PayDepositOnly && user.Balance >= totalRequired && payMethod == "WALLET")
                    {
                        // Thanh toán 100% trọn gói
                        deductedAmount = totalRequired;
                        isFullPayment = true;
                    }
                    else
                    {
                        // Cọc tối thiểu 30 phút giữ máy
                        deductedAmount = minDepositRequired;
                        isFullPayment = false;
                    }

                    user.Balance -= deductedAmount;
                    HttpContext.Session.SetString("Balance", user.Balance.ToString());
                }
                else
                {
                    // Nếu số dư không đủ tiền cọc tối thiểu 30 phút:
                    if (payMethod == "WALLET")
                    {
                        return Json(new
                        {
                            success = false,
                            insufficientBalance = true,
                            minDepositRequired = minDepositRequired,
                            currentBalance = user.Balance,
                            message = $"Số dư ví của bạn ({user.Balance:N0} đ) không đủ để cọc tối thiểu 30 phút ({minDepositRequired:N0} đ). Vui lòng nạp thêm tiền vào ví hoặc quét mã VietQR để cọc."
                        });
                    }
                    else if (payMethod == "CASH")
                    {
                        return Json(new
                        {
                            success = false,
                            requireDeposit = true,
                            minDepositRequired = minDepositRequired,
                            currentBalance = user.Balance,
                            message = $"Quy định giữ chỗ yêu cầu cọc trước ít nhất 30 phút ({minDepositRequired:N0} đ). Số dư ví của bạn không đủ ({user.Balance:N0} đ). Vui lòng chọn Chuyển khoản VietQR để cọc giữ chỗ hoặc nạp thêm ví."
                        });
                    }
                    else if (payMethod == "BANK_QR")
                    {
                        // Khách thanh toán cọc qua chuyển khoản VietQR
                        deductedAmount = 0;
                    }
                }

                var createdBookings = new List<Booking>();
                foreach (var cId in selectedCompIds)
                {
                    var booking = new Booking
                    {
                        UserId = userId,
                        ComputerId = cId,
                        StartTime = startTime,
                        EndTime = endTime,
                        Status = deductedAmount > 0 ? "confirmed" : "pending",
                        HoldExpiresAt = startTime.AddMinutes(HoldMinutes),
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Bookings.Add(booking);
                    createdBookings.Add(booking);
                }

                await _context.SaveChangesAsync();

                var primaryBooking = createdBookings.FirstOrDefault();

                // Ghi nhận biến động số dư ví với loại session_fee nếu có trừ tiền ví
                if (deductedAmount > 0)
                {
                    var walletTrans = new WalletTransaction
                    {
                        UserId = user.UserId,
                        Type = "session_fee",
                        Amount = deductedAmount,
                        ReferenceId = primaryBooking?.BookingId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.WalletTransactions.Add(walletTrans);
                    await _context.SaveChangesAsync();
                }

                // Lưu đơn đồ ăn/tiện ích đi kèm nếu có
                if (dto.FoodItems != null && dto.FoodItems.Any())
                {
                    var compNames = await _context.Computers
                        .Where(c => selectedCompIds.Contains(c.ComputerId))
                        .Select(c => c.ComputerName)
                        .ToListAsync();

                    var foodOrder = new FoodOrder
                    {
                        UserId = userId,
                        BookingId = primaryBooking?.BookingId,
                        SeatLabel = string.Join(", ", compNames),
                        PaymentMethod = payMethod,
                        Status = "pending",
                        TotalAmount = foodFee,
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
                await tx.CommitAsync();
                var bookedComputers = await _context.Computers
                    .Where(c => selectedCompIds.Contains(c.ComputerId))
                    .Select(c => c.ComputerName)
                    .ToListAsync();

                if (primaryBooking == null) primaryBooking = createdBookings.FirstOrDefault();
                string displaySeats = bookedComputers.Any() ? string.Join(", ", bookedComputers) : "#" + (dto.ComputerId ?? 0);

                string statusMsg;
                if (deductedAmount > 0)
                {
                    if (isFullPayment)
                    {
                        statusMsg = $"Đặt máy thành công! Đã thanh toán 100% trọn gói ({deductedAmount:N0} đ) từ Ví CyberGame. Số dư còn lại: {user.Balance:N0} đ. Máy giữ: {displaySeats}.";
                    }
                    else
                    {
                        decimal remaining = totalRequired - deductedAmount;
                        statusMsg = $"Đặt máy thành công! Đã trừ cọc 30 phút ({deductedAmount:N0} đ) từ Ví CyberGame. Số dư còn lại: {user.Balance:N0} đ. Tiền giờ còn lại ({remaining:N0} đ) thanh toán khi trả máy. Máy giữ: {displaySeats}.";
                    }
                }
                else
                {
                    statusMsg = $"Đặt máy thành công! Đã ghi nhận cọc 30 phút ({minDepositRequired:N0} đ) qua Chuyển khoản VietQR. Mã đơn: #{primaryBooking?.BookingId}. Máy giữ: {displaySeats}.";
                }

                return Json(new
                {
                    success = true,
                    bookingId = primaryBooking?.BookingId ?? 0,
                    bookingIds = createdBookings.Select(b => b.BookingId).ToList(),
                    computerId = primaryBooking?.ComputerId ?? 0,
                    computerName = displaySeats,
                    startTime = startTime.ToString("dd/MM/yyyy HH:mm"),
                    endTime = endTime.ToString("dd/MM/yyyy HH:mm"),
                    minDeposit = minDepositRequired,
                    depositAmount = minDepositRequired,
                    deposit = minDepositRequired,
                    depositFee = minDepositRequired,
                    pricePerHour = zonePrice,
                    zonePrice = zonePrice,
                    totalAmount = totalRequired,
                    deductedAmount = deductedAmount,
                    isFullPayment = isFullPayment,
                    newBalance = user.Balance,
                    newBalanceFormatted = user.Balance.ToString("N0") + " đ",
                    message = statusMsg
                });
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException != null ? (" (" + ex.InnerException.Message + ")") : "";
                return Json(new { success = false, message = "Lỗi khi lưu đặt chỗ: " + ex.Message + innerMsg });
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

                var payMethod = dto.PaymentMethod?.Trim().ToUpper() ?? "CASH";
                if (payMethod == "COUNTER") payMethod = "CASH";
                if (payMethod == "TRANSFER") payMethod = "BANK_QR";
                bool isWallet = payMethod == "WALLET";

                if (isWallet)
                {
                    if (user.Balance < total)
                    {
                        return Json(new
                        {
                            success = false,
                            insufficientBalance = true,
                            requiredAmount = total,
                            currentBalance = user.Balance,
                            message = $"Số dư ví của bạn ({user.Balance:N0} đ) không đủ để thanh toán ({total:N0} đ). Vui lòng nạp thêm tiền vào ví."
                        });
                    }

                    user.Balance -= total;
                }

                var seatLabel = !string.IsNullOrWhiteSpace(dto.SeatLabel) ? dto.SeatLabel.Trim() : (!string.IsNullOrWhiteSpace(dto.SeatNumber) ? dto.SeatNumber.Trim() : null);

                var order = new FoodOrder
                {
                    UserId = userId,
                    SeatLabel = seatLabel,
                    PaymentMethod = payMethod,
                    Status = "pending",
                    TotalAmount = total,
                    CreatedAt = DateTime.UtcNow
                };

                _context.FoodOrders.Add(order);
                await _context.SaveChangesAsync();

                if (isWallet)
                {
                    var trans = new WalletTransaction
                    {
                        UserId = user.UserId,
                        Type = "food_order",
                        Amount = total,
                        ReferenceId = order.OrderId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.WalletTransactions.Add(trans);
                    await _context.SaveChangesAsync();
                }

                foreach (var item in dto.Items)
                {
                    int foodId = item.FoodId;
                    if (foodId <= 0 && !string.IsNullOrWhiteSpace(item.Name))
                    {
                        var foundFood = await _context.Foods.FirstOrDefaultAsync(f => f.Name.ToLower() == item.Name.ToLower());
                        if (foundFood != null) foodId = foundFood.FoodId;
                    }
                    if (foodId <= 0)
                    {
                        var firstFood = await _context.Foods.FirstOrDefaultAsync();
                        if (firstFood != null) foodId = firstFood.FoodId;
                    }

                    int qty = item.Quantity > 0 ? item.Quantity : 1;
                    var orderItem = new FoodOrderItem
                    {
                        OrderId = order.OrderId,
                        FoodId = foodId,
                        Quantity = qty,
                        UnitPrice = item.Price,
                        Subtotal = item.Price * qty
                    };
                    _context.FoodOrderItems.Add(orderItem);
                }
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    orderId = order.OrderId,
                    total = total,
                    totalAmount = total,
                    seatLabel = order.SeatLabel,
                    seatNumber = order.SeatLabel,
                    status = order.Status,
                    paymentMethod = order.PaymentMethod,
                    newBalance = user.Balance,
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
