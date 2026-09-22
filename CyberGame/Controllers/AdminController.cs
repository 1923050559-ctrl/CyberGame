using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using CyberGame.Models;
using CyberGame.Models.Entities;
using CyberGame.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CyberGame.Controllers
{
    public class AdminController : Controller
    {
        private readonly CyberGameDbContext _context;

        public AdminController(CyberGameDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var sessionRev = await _context.Sessions
                .Where(s => s.TotalCost.HasValue)
                .SumAsync(s => (decimal?)s.TotalCost) ?? 0;

            var confirmedBookings = await _context.Bookings
                .Include(b => b.Computer)
                .ThenInclude(c => c.Zone)
                .Where(b => b.Status == "confirmed" || b.Status == "completed")
                .ToListAsync();

            decimal bookingRevFromBookings = 0;
            foreach (var b in confirmedBookings)
            {
                var price = b.Computer?.Zone?.PricePerHour ?? 20000m;
                var hours = (b.EndTime.HasValue && b.EndTime > b.StartTime)
                    ? (decimal)(b.EndTime.Value - b.StartTime).TotalHours
                    : 2m;
                bookingRevFromBookings += hours * price;
            }
            var totalBookingRev = sessionRev + bookingRevFromBookings;

            var totalFnbRev = await _context.FoodOrders
                .Where(o => o.Status != "cancelled")
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            var totalRechargeRev = await _context.Recharges
                .Where(r => r.Status == "completed")
                .SumAsync(r => (decimal?)r.Amount) ?? 0;

            var vm = new AdminDashboardViewModel
            {
                TotalRevenue = totalBookingRev + totalFnbRev + totalRechargeRev,
                BookingRevenue = totalBookingRev,
                FnbRevenue = totalFnbRev,
                TotalComputers = await _context.Computers.CountAsync(),
                ActiveComputers = await _context.Sessions.CountAsync(s => s.EndTime == null),
                MaintenanceComputers = await _context.Computers.CountAsync(c => c.Status == "maintenance"),
                TotalUsers = await _context.Users.CountAsync(),
                PendingOrdersCount = await _context.FoodOrders.CountAsync(o => o.Status == "pending"),
                Zones = await _context.Zones.Include(z => z.Computers).ToListAsync(),
                RecentOrders = await _context.FoodOrders
                    .Include(o => o.User)
                    .Include(o => o.FoodOrderItems)
                    .ThenInclude(i => i.Food)
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(10)
                    .ToListAsync(),
                TopFoods = await _context.Foods
                    .Take(6)
                    .ToListAsync()
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardStats()
        {
            // 1. Calculate Booking revenue from both Sessions and confirmed Bookings
            var sessionRev = await _context.Sessions
                .Where(s => s.TotalCost.HasValue)
                .SumAsync(s => (decimal?)s.TotalCost) ?? 0;

            var confirmedBookings = await _context.Bookings
                .Include(b => b.Computer)
                .ThenInclude(c => c.Zone)
                .Include(b => b.User)
                .Where(b => b.Status == "confirmed" || b.Status == "completed")
                .ToListAsync();

            decimal bookingRevFromBookings = 0;
            foreach (var b in confirmedBookings)
            {
                var price = b.Computer?.Zone?.PricePerHour ?? 20000m;
                var hours = (b.EndTime.HasValue && b.EndTime > b.StartTime)
                    ? (decimal)(b.EndTime.Value - b.StartTime).TotalHours
                    : 2m;
                bookingRevFromBookings += hours * price;
            }
            var totalBookingRev = sessionRev + bookingRevFromBookings;

            // 2. FnB revenue
            var foodOrders = await _context.FoodOrders
                .Include(o => o.User)
                .Include(o => o.FoodOrderItems)
                .ThenInclude(i => i.Food)
                .Where(o => o.Status != "cancelled")
                .ToListAsync();
            var totalFnbRev = foodOrders.Sum(o => o.TotalAmount);

            // 3. Recharges revenue
            var recharges = await _context.Recharges
                .Include(r => r.User)
                .Where(r => r.Status == "completed")
                .ToListAsync();
            var totalRecharges = recharges.Sum(r => r.Amount);

            var totalRevenue = totalBookingRev + totalFnbRev + totalRecharges;

            // 4. Maintenance / Equipment stats
            var totalComp = await _context.Computers.CountAsync();
            var activeComp = await _context.Sessions.CountAsync(s => s.EndTime == null);
            var maintComp = await _context.Computers.CountAsync(c => c.Status == "maintenance");

            var allEquipment = await _context.Equipment.ToListAsync();
            var brokenEquipment = allEquipment.Where(e => e.ConditionStatus != "good").ToList();
            int totalMaintIssues = maintComp + brokenEquipment.Count;

            decimal totalMaintCost = (maintComp * 200000m) + brokenEquipment.Sum(e =>
                e.EquipmentName.Contains("phím", StringComparison.OrdinalIgnoreCase) ? 350000m :
                e.EquipmentName.Contains("chuột", StringComparison.OrdinalIgnoreCase) ? 250000m :
                e.EquipmentName.Contains("tai nghe", StringComparison.OrdinalIgnoreCase) ? 300000m :
                e.EquipmentName.Contains("màn hình", StringComparison.OrdinalIgnoreCase) ? 500000m : 200000m);

            // 5. Hourly Trend data (Today: 08:00 to 24:00, 9 slots)
            var hourlyLabels = new[] { "08:00", "10:00", "12:00", "14:00", "16:00", "18:00", "20:00", "22:00", "24:00" };
            var todayBookingHourly = new decimal[9];
            var todayFnbHourly = new decimal[9];
            var todayRechargesHourly = new decimal[9];

            int GetHourSlot(int hour)
            {
                if (hour <= 8) return 0;
                if (hour <= 10) return 1;
                if (hour <= 12) return 2;
                if (hour <= 14) return 3;
                if (hour <= 16) return 4;
                if (hour <= 18) return 5;
                if (hour <= 20) return 6;
                if (hour <= 22) return 7;
                return 8;
            }

            foreach (var b in confirmedBookings)
            {
                int slot = GetHourSlot(b.StartTime.Hour);
                var price = b.Computer?.Zone?.PricePerHour ?? 20000m;
                var hours = (b.EndTime.HasValue && b.EndTime > b.StartTime)
                    ? (decimal)(b.EndTime.Value - b.StartTime).TotalHours
                    : 2m;
                todayBookingHourly[slot] += hours * price;
            }

            foreach (var o in foodOrders)
            {
                int slot = GetHourSlot(o.CreatedAt.Hour);
                todayFnbHourly[slot] += o.TotalAmount;
            }

            foreach (var r in recharges)
            {
                int slot = GetHourSlot(r.CreatedAt.Hour);
                todayRechargesHourly[slot] += r.Amount;
            }

            // 6. Weekday Trend data (Thứ 2 to Chủ Nhật, 7 slots)
            var weekdayLabels = new[] { "Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5", "Thứ 6", "Thứ 7", "Chủ Nhật" };
            var weekdayBooking = new decimal[7];
            var weekdayFnb = new decimal[7];
            var weekdayRecharges = new decimal[7];

            int GetWeekdayIndex(DateTime dt)
            {
                return dt.DayOfWeek == DayOfWeek.Sunday ? 6 : ((int)dt.DayOfWeek - 1);
            }

            foreach (var b in confirmedBookings)
            {
                int w = GetWeekdayIndex(b.StartTime);
                var price = b.Computer?.Zone?.PricePerHour ?? 20000m;
                var hours = (b.EndTime.HasValue && b.EndTime > b.StartTime)
                    ? (decimal)(b.EndTime.Value - b.StartTime).TotalHours
                    : 2m;
                weekdayBooking[w] += hours * price;
            }

            foreach (var o in foodOrders)
            {
                int w = GetWeekdayIndex(o.CreatedAt);
                weekdayFnb[w] += o.TotalAmount;
            }

            foreach (var r in recharges)
            {
                int w = GetWeekdayIndex(r.CreatedAt);
                weekdayRecharges[w] += r.Amount;
            }

            // 7. Maintenance Chart Categories
            var maintLabels = new[] { "Chuột gaming", "Bàn phím cơ", "Tai nghe & Mút", "PC & Tản nhiệt", "Màn hình & Cáp", "Ghế Gaming" };
            var maintCostArr = new decimal[6];
            var maintCountArr = new int[6];

            maintCountArr[3] = maintComp;
            maintCostArr[3] = maintComp * 200000m;

            foreach (var e in brokenEquipment)
            {
                var name = e.EquipmentName.ToLower();
                if (name.Contains("chuột")) { maintCountArr[0]++; maintCostArr[0] += 250000m; }
                else if (name.Contains("phím")) { maintCountArr[1]++; maintCostArr[1] += 350000m; }
                else if (name.Contains("tai nghe")) { maintCountArr[2]++; maintCostArr[2] += 300000m; }
                else if (name.Contains("màn")) { maintCountArr[4]++; maintCostArr[4] += 500000m; }
                else if (name.Contains("ghế")) { maintCountArr[5]++; maintCostArr[5] += 200000m; }
                else { maintCountArr[3]++; maintCostArr[3] += 200000m; }
            }

            // 8. Other Periods for Trend Chart (Month & Year)
            var monthLabels = new[] { "Tuần 1", "Tuần 2", "Tuần 3", "Tuần 4" };
            var monthBooking = new decimal[4];
            var monthFnb = new decimal[4];
            var monthRecharges = new decimal[4];
            foreach (var b in confirmedBookings)
            {
                int w = Math.Clamp((b.StartTime.Day - 1) / 7, 0, 3);
                var price = b.Computer?.Zone?.PricePerHour ?? 20000m;
                var hours = (b.EndTime.HasValue && b.EndTime > b.StartTime) ? (decimal)(b.EndTime.Value - b.StartTime).TotalHours : 2m;
                monthBooking[w] += hours * price;
            }
            foreach (var o in foodOrders)
            {
                int w = Math.Clamp((o.CreatedAt.Day - 1) / 7, 0, 3);
                monthFnb[w] += o.TotalAmount;
            }
            foreach (var r in recharges)
            {
                int w = Math.Clamp((r.CreatedAt.Day - 1) / 7, 0, 3);
                monthRecharges[w] += r.Amount;
            }

            var yearLabels = new[] { "T1", "T2", "T3", "T4", "T5", "T6", "T7", "T8", "T9", "T10", "T11", "T12" };
            var yearBooking = new decimal[12];
            var yearFnb = new decimal[12];
            var yearRecharges = new decimal[12];
            foreach (var b in confirmedBookings)
            {
                int m = Math.Clamp(b.StartTime.Month - 1, 0, 11);
                var price = b.Computer?.Zone?.PricePerHour ?? 20000m;
                var hours = (b.EndTime.HasValue && b.EndTime > b.StartTime) ? (decimal)(b.EndTime.Value - b.StartTime).TotalHours : 2m;
                yearBooking[m] += hours * price;
            }
            foreach (var o in foodOrders)
            {
                int m = Math.Clamp(o.CreatedAt.Month - 1, 0, 11);
                yearFnb[m] += o.TotalAmount;
            }
            foreach (var r in recharges)
            {
                int m = Math.Clamp(r.CreatedAt.Month - 1, 0, 11);
                yearRecharges[m] += r.Amount;
            }

            // 9. Build real transactions table list
            var transactions = new List<object>();
            foreach (var b in confirmedBookings)
            {
                var price = b.Computer?.Zone?.PricePerHour ?? 20000m;
                var hours = (b.EndTime.HasValue && b.EndTime > b.StartTime) ? (decimal)(b.EndTime.Value - b.StartTime).TotalHours : 2m;
                var cost = hours * price;
                transactions.Add(new
                {
                    code = "BK-" + b.BookingId.ToString("D4"),
                    customer = b.User?.Username ?? "Khách hàng",
                    phone = b.User?.Phone ?? "0900000000",
                    bookingFee = cost,
                    fnbFee = 0m,
                    gearFee = 0m,
                    total = cost,
                    method = "Ví Cyber",
                    time = b.StartTime.ToString("dd/MM/yyyy HH:mm"),
                    status = b.Status == "confirmed" ? "Đã xác nhận" : "Đã xong"
                });
            }
            foreach (var o in foodOrders)
            {
                transactions.Add(new
                {
                    code = "FB-" + o.OrderId.ToString("D4"),
                    customer = o.User?.Username ?? "Khách hàng",
                    phone = o.User?.Phone ?? "0900000000",
                    bookingFee = 0m,
                    fnbFee = o.TotalAmount,
                    gearFee = 0m,
                    total = o.TotalAmount,
                    method = o.PaymentMethod == "cash" ? "Tiền mặt" : "Ví Cyber",
                    time = o.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                    status = o.Status == "completed" ? "Đã phục vụ" : (o.Status == "cooking" ? "Đang làm" : "Chờ nhận")
                });
            }
            foreach (var r in recharges)
            {
                transactions.Add(new
                {
                    code = "RC-" + r.RechargeId.ToString("D4"),
                    customer = r.User?.Username ?? "Khách hàng",
                    phone = r.User?.Phone ?? "0900000000",
                    bookingFee = 0m,
                    fnbFee = 0m,
                    gearFee = 0m,
                    total = r.Amount,
                    method = "Chuyển khoản",
                    time = r.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                    status = "Nạp ví thành công"
                });
            }

            // 10. Top Food Items
            var topFoodGrouped = await _context.FoodOrderItems
                .Include(i => i.Food)
                .GroupBy(i => new { i.FoodId, FoodName = i.Food.Name, i.Food.Price, i.Food.Category })
                .Select(g => new
                {
                    name = g.Key.FoodName,
                    category = g.Key.Category == "food" ? "Đồ Ăn" : (g.Key.Category == "drink" ? "Đồ Uống" : "Snack"),
                    price = g.Key.Price,
                    qty = g.Sum(x => x.Quantity),
                    revenue = g.Sum(x => x.Subtotal)
                })
                .OrderByDescending(x => x.qty)
                .ToListAsync();

            var topFoodItems = new List<object>();
            int rank = 1;
            decimal totFoodRev = topFoodGrouped.Sum(x => x.revenue);
            foreach (var item in topFoodGrouped)
            {
                var contrib = totFoodRev > 0 ? ((item.revenue / totFoodRev) * 100).ToString("0.0") + "%" : "0%";
                topFoodItems.Add(new
                {
                    rank = rank++,
                    name = item.name,
                    category = item.category,
                    price = item.price,
                    qty = item.qty,
                    revenue = item.revenue,
                    contribution = contrib
                });
            }
            if (!topFoodItems.Any())
            {
                var sampleFoods = await _context.Foods.Take(5).ToListAsync();
                foreach (var sf in sampleFoods)
                {
                    topFoodItems.Add(new
                    {
                        rank = rank++,
                        name = sf.Name,
                        category = sf.Category == "food" ? "Đồ Ăn" : (sf.Category == "drink" ? "Đồ Uống" : "Snack"),
                        price = sf.Price,
                        qty = 0,
                        revenue = 0m,
                        contribution = "0%"
                    });
                }
            }

            // 11. Maintenance logs
            var maintenanceLogs = new List<object>();
            int btCode = 101;
            foreach (var e in brokenEquipment)
            {
                maintenanceLogs.Add(new
                {
                    code = "BT-" + (btCode++),
                    location = "Máy #" + (e.ComputerId > 0 ? e.ComputerId.ToString() : "Kho"),
                    item = e.EquipmentName,
                    detail = "Linh kiện bị báo hỏng (" + e.ConditionStatus + "), cần thay mới",
                    cost = e.EquipmentName.Contains("phím", StringComparison.OrdinalIgnoreCase) ? 350000m : 250000m,
                    tech = "KTV Hoàng Long",
                    date = DateTime.Now.ToString("dd/MM/yyyy"),
                    status = "Đang xử lý"
                });
            }
            var maintComps = await _context.Computers.Where(c => c.Status == "maintenance").ToListAsync();
            foreach (var mc in maintComps)
            {
                maintenanceLogs.Add(new
                {
                    code = "BT-" + (btCode++),
                    location = "Máy " + mc.ComputerName,
                    item = "PC & Hệ thống tản nhiệt",
                    detail = "Bảo dưỡng định kỳ, kiểm tra quạt và thay keo tản nhiệt",
                    cost = 200000m,
                    tech = "KTV Minh Tuấn",
                    date = DateTime.Now.ToString("dd/MM/yyyy"),
                    status = "Hoàn tất"
                });
            }

            // 12. Zones
            var zones = await _context.Zones
                .Select(z => new
                {
                    z.ZoneId,
                    z.ZoneName,
                    z.PricePerHour,
                    TotalComputers = z.Computers.Count,
                    ActiveComputers = z.Computers.Count(c => c.Status == "occupied")
                })
                .ToListAsync();

            return Json(new
            {
                success = true,
                analytics = new
                {
                    totalRevenue = totalRevenue,
                    bookingRevenue = totalBookingRev,
                    fnbRevenue = totalFnbRev,
                    rechargesRevenue = totalRecharges,
                    gearRevenue = 0,
                    maintenanceCost = totalMaintCost,
                    maintenanceCount = totalMaintIssues,
                    trendRev = "+12.5%",
                    trendBooking = "+8.2%",
                    trendFnb = "+15.0%",
                    trendGear = "0%"
                },
                charts = new
                {
                    trend = new
                    {
                        today = new
                        {
                            labels = hourlyLabels,
                            bookingData = todayBookingHourly,
                            fnbData = todayFnbHourly,
                            rechargesData = todayRechargesHourly
                        },
                        week = new
                        {
                            labels = weekdayLabels,
                            bookingData = weekdayBooking,
                            fnbData = weekdayFnb,
                            rechargesData = weekdayRecharges
                        },
                        month = new
                        {
                            labels = monthLabels,
                            bookingData = monthBooking,
                            fnbData = monthFnb,
                            rechargesData = monthRecharges
                        },
                        year = new
                        {
                            labels = yearLabels,
                            bookingData = yearBooking,
                            fnbData = yearFnb,
                            rechargesData = yearRecharges
                        }
                    },
                    category = new
                    {
                        labels = new[] { "Booking máy", "Đồ ăn FnB", "Nạp tiền ví" },
                        data = new[] { totalBookingRev, totalFnbRev, totalRecharges }
                    },
                    weekday = new
                    {
                        labels = weekdayLabels,
                        bookingData = weekdayBooking,
                        fnbData = weekdayFnb,
                        rechargesData = weekdayRecharges
                    },
                    maintenance = new
                    {
                        labels = maintLabels,
                        costData = maintCostArr,
                        countData = maintCountArr
                    }
                },
                transactions = transactions,
                topFoodItems = topFoodItems,
                maintenanceLogs = maintenanceLogs,
                computers = new
                {
                    total = totalComp,
                    active = activeComp,
                    maintenance = maintComp,
                    available = totalComp - activeComp - maintComp
                },
                zones = zones
            });
        }

        public async Task<IActionResult> Customers()
        {
            ViewBag.Users = await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            ViewBag.Orders = await _context.FoodOrders
                .Include(o => o.User)
                .Include(o => o.FoodOrderItems)
                .ThenInclude(i => i.Food)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomersData()
        {
            var users = await _context.Users
                .Select(u => new
                {
                    id = u.UserId,
                    username = u.Username,
                    email = u.Email ?? "",
                    phone = u.Phone ?? "",
                    balance = u.Balance,
                    role = u.Role,
                    status = u.Status,
                    createdAt = u.CreatedAt.ToString("dd/MM/yyyy HH:mm")
                })
                .ToListAsync();

            var orders = await _context.FoodOrders
                .Select(o => new
                {
                    id = o.OrderId,
                    customerName = o.User.Username,
                    seatNumber = o.SeatLabel ?? "Máy",
                    status = o.Status,
                    total = o.TotalAmount,
                    createdAt = o.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                    items = o.FoodOrderItems.Select(i => new
                    {
                        name = i.Food.Name,
                        qty = i.Quantity,
                        price = i.UnitPrice
                    }).ToList()
                })
                .OrderByDescending(o => o.id)
                .ToListAsync();

            return Json(new { success = true, customers = users, orders = orders });
        }

        [HttpPost]
        public async Task<IActionResult> TopUpBalance([FromBody] TopUpDto dto)
        {
            if (dto == null || dto.UserId <= 0 || dto.Amount <= 0)
            {
                return Json(new { success = false, message = "Thông tin nạp tiền không hợp lệ." });
            }

            var user = await _context.Users.FindAsync(dto.UserId);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng." });
            }

            user.Balance += dto.Amount;

            var transaction = new WalletTransaction
            {
                UserId = user.UserId,
                Type = "recharge",
                Amount = dto.Amount,
                CreatedAt = DateTime.UtcNow
            };
            _context.WalletTransactions.Add(transaction);

            var recharge = new Recharge
            {
                UserId = user.UserId,
                MethodId = 1,
                Amount = dto.Amount,
                Status = "completed",
                CreatedAt = DateTime.UtcNow
            };
            _context.Recharges.Add(recharge);

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                newBalance = user.Balance,
                message = $"Đã nạp {dto.Amount:N0} đ thành công cho tài khoản {user.Username}!"
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Username))
            {
                return Json(new { success = false, message = "Tên tài khoản không được để trống." });
            }

            var username = dto.Username.Trim();
            if (await _context.Users.AnyAsync(u => u.Username == username))
            {
                return Json(new { success = false, message = "Tên tài khoản đã tồn tại." });
            }

            var user = new User
            {
                Username = username,
                Email = string.IsNullOrWhiteSpace(dto.Email) ? $"{username}@cybergame.vn" : dto.Email.Trim(),
                Phone = dto.Phone?.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(string.IsNullOrWhiteSpace(dto.Password) ? "123456" : dto.Password),
                Role = "member",
                Status = "active",
                Balance = dto.InitialBalance >= 0 ? dto.InitialBalance : 0,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                user = new
                {
                    id = user.UserId,
                    username = user.Username,
                    email = user.Email,
                    phone = user.Phone,
                    balance = user.Balance,
                    role = user.Role,
                    status = user.Status,
                    createdAt = user.CreatedAt.ToString("dd/MM/yyyy HH:mm")
                },
                message = "Tạo tài khoản thành viên thành công!"
            });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus([FromBody] UpdateOrderStatusDto dto)
        {
            if (dto == null || dto.OrderId <= 0)
            {
                return Json(new { success = false, message = "Mã đơn hàng không hợp lệ." });
            }

            var order = await _context.FoodOrders.FindAsync(dto.OrderId);
            if (order == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng." });
            }

            order.Status = dto.Status;
            await _context.SaveChangesAsync();

            return Json(new { success = true, status = order.Status, message = "Cập nhật trạng thái đơn thành công!" });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng." });
            }

            if (user.Role == "admin")
            {
                return Json(new { success = false, message = "Không thể khóa tài khoản Quản trị viên (admin)!" });
            }

            user.Status = "banned";
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Đã khóa tài khoản {user.Username} thành công." });
        }

        public async Task<IActionResult> Chat()
        {
            ViewBag.Tickets = await _context.SupportTickets
                .Include(t => t.ChatSession)
                .ThenInclude(c => c.User)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            ViewBag.ChatSessions = await _context.ChatSessions
                .Include(c => c.User)
                .Include(c => c.ChatMessages)
                .OrderByDescending(c => c.StartedAt)
                .ToListAsync();

            return View();
        }
    }

    public class UpdateOrderStatusDto
    {
        public int OrderId { get; set; }
        public string Status { get; set; } = "completed";
    }
}
