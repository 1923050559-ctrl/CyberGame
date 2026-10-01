using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using CyberGame.Models;
using CyberGame.Models.Entities;
using CyberGame.Models.ViewModels;
using CyberGame.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CyberGame.Controllers
{
    public class AccountController : Controller
    {
        private readonly CyberGameDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            CyberGameDbContext context,
            IEmailService emailService,
            IWebHostEnvironment env,
            ILogger<AccountController> logger)
        {
            _context = context;
            _emailService = emailService;
            _env = env;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var identifier = model.Username.Trim();
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Username == identifier || u.Email == identifier || u.Phone == identifier);

            if (user == null || !VerifyPassword(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "Tài khoản hoặc mật khẩu không chính xác.");
                return View(model);
            }

            if (user.Status != "active")
            {
                ModelState.AddModelError(string.Empty, "Tài khoản của bạn đã bị khóa hoặc tạm ngưng.");
                return View(model);
            }

            // Tạo vé xác thực Authentication Cookie
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("Balance", user.Balance.ToString())
            };
            if (!string.IsNullOrEmpty(user.Email))
            {
                claims.Add(new Claim(ClaimTypes.Email, user.Email));
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity), authProperties);

            // Lưu phiên làm việc Session
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Role", user.Role);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            if (user.Role == "admin")
            {
                return RedirectToAction("Index", "Admin");
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> LoginAjax([FromBody] LoginViewModel model)
        {
            if (string.IsNullOrEmpty(model.Username) || string.IsNullOrEmpty(model.Password))
            {
                return Json(new { success = false, message = "Vui lòng nhập đầy đủ thông tin đăng nhập." });
            }

            var identifier = model.Username.Trim();
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Username == identifier || u.Email == identifier || u.Phone == identifier);

            if (user == null || !VerifyPassword(model.Password, user.PasswordHash))
            {
                return Json(new { success = false, message = "Tài khoản hoặc mật khẩu không đúng." });
            }

            if (user.Status != "active")
            {
                return Json(new { success = false, message = "Tài khoản của bạn đã bị khóa." });
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("Balance", user.Balance.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity));

            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Role", user.Role);

            return Json(new
            {
                success = true,
                username = user.Username,
                role = user.Role,
                balance = user.Balance,
                redirectUrl = user.Role == "admin" ? Url.Action("Index", "Admin") : Url.Action("Index", "Home")
            });
        }

        [HttpGet]
        public IActionResult Register()
        {
            var pendingJson = HttpContext.Session.GetString("PendingRegistration");
            if (!string.IsNullOrEmpty(pendingJson))
            {
                try
                {
                    var pending = JsonSerializer.Deserialize<PendingRegistration>(pendingJson);
                    if (pending != null)
                    {
                        return View(new RegisterViewModel
                        {
                            Username = pending.Username,
                            Email = pending.Email,
                            Phone = pending.Phone
                        });
                    }
                }
                catch { }
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var username = model.Username.Trim();
            var email = model.Email.Trim().ToLowerInvariant();
            var phone = model.Phone?.Trim() ?? string.Empty;

            if (await _context.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower()))
            {
                ModelState.AddModelError("Username", "Tên tài khoản này đã được sử dụng.");
                return View(model);
            }

            if (await _context.Users.AnyAsync(u => u.Email != null && u.Email.ToLower() == email))
            {
                ModelState.AddModelError("Email", "Địa chỉ email này đã được sử dụng.");
                return View(model);
            }

            // Sinh mã OTP ngẫu nhiên 6 chữ số
            var otp = Random.Shared.Next(100000, 999999).ToString();

            // Lưu thông tin đăng ký tạm thời vào Session để xác thực OTP trước khi ghi vào Database
            var pending = new PendingRegistration
            {
                Username = username,
                Email = email,
                Phone = phone,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                OtpCode = otp,
                OtpExpiry = DateTime.UtcNow.AddMinutes(5),
                LastSentAt = DateTime.UtcNow,
                AttemptCount = 0
            };

            HttpContext.Session.SetString("PendingRegistration", JsonSerializer.Serialize(pending));

            // Gửi email chứa mã OTP
            var (emailSent, errorMsg) = await _emailService.SendOtpEmailAsync(email, username, otp, 5);

            if (!emailSent)
            {
                TempData["EmailWarning"] = errorMsg ?? "Không thể gửi email OTP tự động. Vui lòng kiểm tra lại cấu hình email.";
            }

            return RedirectToAction("VerifyOtp");
        }

        [HttpGet]
        public IActionResult VerifyOtp()
        {
            var pendingJson = HttpContext.Session.GetString("PendingRegistration");
            if (string.IsNullOrEmpty(pendingJson))
            {
                TempData["ErrorMessage"] = "Không tìm thấy phiên đăng ký hoặc phiên đã kết thúc. Vui lòng đăng ký lại.";
                return RedirectToAction("Register");
            }

            PendingRegistration? pending;
            try
            {
                pending = JsonSerializer.Deserialize<PendingRegistration>(pendingJson);
            }
            catch
            {
                return RedirectToAction("Register");
            }

            if (pending == null)
            {
                return RedirectToAction("Register");
            }

            var remainingSeconds = (int)(pending.OtpExpiry - DateTime.UtcNow).TotalSeconds;
            var elapsedSinceLastSend = (int)(DateTime.UtcNow - pending.LastSentAt).TotalSeconds;

            var viewModel = new VerifyOtpViewModel
            {
                Email = pending.Email,
                RemainingSeconds = Math.Max(0, remainingSeconds),
                ResendCooldownSeconds = Math.Max(0, 60 - elapsedSinceLastSend)
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp(VerifyOtpViewModel model)
        {
            var pendingJson = HttpContext.Session.GetString("PendingRegistration");
            if (string.IsNullOrEmpty(pendingJson))
            {
                ModelState.AddModelError(string.Empty, "Phiên đăng ký đã hết hạn. Vui lòng thực hiện lại.");
                return View(model);
            }

            PendingRegistration? pending;
            try
            {
                pending = JsonSerializer.Deserialize<PendingRegistration>(pendingJson);
            }
            catch
            {
                return RedirectToAction("Register");
            }

            if (pending == null)
            {
                return RedirectToAction("Register");
            }

            model.Email = pending.Email;
            var remainingSeconds = (int)(pending.OtpExpiry - DateTime.UtcNow).TotalSeconds;
            model.RemainingSeconds = Math.Max(0, remainingSeconds);
            var elapsed = (int)(DateTime.UtcNow - pending.LastSentAt).TotalSeconds;
            model.ResendCooldownSeconds = Math.Max(0, 60 - elapsed);

            if (DateTime.UtcNow > pending.OtpExpiry)
            {
                ModelState.AddModelError("OtpCode", "Mã OTP đã hết hiệu lực (quá 5 phút). Vui lòng nhấn 'Gửi lại mã OTP'.");
                return View(model);
            }

            pending.AttemptCount++;
            if (pending.AttemptCount > 5)
            {
                HttpContext.Session.Remove("PendingRegistration");
                TempData["ErrorMessage"] = "Bạn đã nhập sai mã OTP quá 5 lần. Vui lòng thực hiện đăng ký lại từ đầu để bảo vệ an toàn.";
                return RedirectToAction("Register");
            }

            var inputOtp = model.OtpCode?.Trim() ?? string.Empty;
            if (inputOtp != pending.OtpCode)
            {
                HttpContext.Session.SetString("PendingRegistration", JsonSerializer.Serialize(pending));
                ModelState.AddModelError("OtpCode", $"Mã OTP không chính xác. Bạn còn {5 - pending.AttemptCount} lần thử.");
                return View(model);
            }

            // Kiểm tra trùng lặp lần cuối trước khi tạo User chính thức
            if (await _context.Users.AnyAsync(u => u.Username.ToLower() == pending.Username.ToLower()))
            {
                HttpContext.Session.Remove("PendingRegistration");
                TempData["ErrorMessage"] = "Tên tài khoản này vừa có người đăng ký. Vui lòng chọn tên khác.";
                return RedirectToAction("Register");
            }

            if (await _context.Users.AnyAsync(u => u.Email != null && u.Email.ToLower() == pending.Email.ToLower()))
            {
                HttpContext.Session.Remove("PendingRegistration");
                TempData["ErrorMessage"] = "Email này đã được sử dụng. Vui lòng kiểm tra lại.";
                return RedirectToAction("Register");
            }

            var newUser = new User
            {
                Username = pending.Username,
                Email = pending.Email,
                Phone = pending.Phone,
                PasswordHash = pending.PasswordHash,
                Role = "member",
                Status = "active",
                Balance = 0,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            // Xóa session đăng ký tạm thời
            HttpContext.Session.Remove("PendingRegistration");

            // Tự động đăng nhập sau khi xác thực thành công
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, newUser.UserId.ToString()),
                new Claim(ClaimTypes.Name, newUser.Username),
                new Claim(ClaimTypes.Role, newUser.Role),
                new Claim("Balance", "0")
            };
            if (!string.IsNullOrEmpty(newUser.Email))
            {
                claims.Add(new Claim(ClaimTypes.Email, newUser.Email));
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity));

            HttpContext.Session.SetInt32("UserId", newUser.UserId);
            HttpContext.Session.SetString("Username", newUser.Username);
            HttpContext.Session.SetString("Role", newUser.Role);

            TempData["SuccessMessage"] = $"Xác thực email thành công! Chào mừng game thủ {newUser.Username} đã gia nhập hệ thống Cyber Game.";
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> ResendOtp()
        {
            var pendingJson = HttpContext.Session.GetString("PendingRegistration");
            if (string.IsNullOrEmpty(pendingJson))
            {
                return Json(new { success = false, message = "Phiên đăng ký đã kết thúc. Vui lòng đăng ký lại." });
            }

            PendingRegistration? pending;
            try
            {
                pending = JsonSerializer.Deserialize<PendingRegistration>(pendingJson);
            }
            catch
            {
                return Json(new { success = false, message = "Không tìm thấy dữ liệu đăng ký hợp lệ." });
            }

            if (pending == null)
            {
                return Json(new { success = false, message = "Không tìm thấy dữ liệu đăng ký." });
            }

            var elapsedSinceLastSend = (DateTime.UtcNow - pending.LastSentAt).TotalSeconds;
            if (elapsedSinceLastSend < 60)
            {
                var waitSec = 60 - (int)elapsedSinceLastSend;
                return Json(new { success = false, message = $"Vui lòng đợi thêm {waitSec} giây trước khi gửi lại mã mới." });
            }

            var newOtp = Random.Shared.Next(100000, 999999).ToString();
            pending.OtpCode = newOtp;
            pending.OtpExpiry = DateTime.UtcNow.AddMinutes(5);
            pending.LastSentAt = DateTime.UtcNow;
            pending.AttemptCount = 0;

            HttpContext.Session.SetString("PendingRegistration", JsonSerializer.Serialize(pending));

            var (sent, errorMsg) = await _emailService.SendOtpEmailAsync(pending.Email, pending.Username, newOtp, 5);

            return Json(new
            {
                success = true,
                message = sent ? "Mã OTP mới đã được gửi tới email của bạn." : (errorMsg ?? "Đã tạo mã OTP mới thành công."),
                cooldown = 60,
                expirySeconds = 300
            });
        }

        [HttpGet]
        public IActionResult CancelRegistration()
        {
            // Cho phép người dùng quay lại sửa thông tin email hoặc số điện thoại
            return RedirectToAction("Register");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> CurrentUser()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null && User.Identity?.IsAuthenticated == true)
            {
                var claimId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(claimId, out int id))
                {
                    userId = id;
                }
            }

            if (userId == null)
            {
                return Json(new { isAuthenticated = false });
            }

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
            {
                return Json(new { isAuthenticated = false });
            }

            return Json(new
            {
                isAuthenticated = true,
                userId = user.UserId,
                username = user.Username,
                email = user.Email,
                phone = user.Phone,
                role = user.Role,
                balance = user.Balance
            });
        }

        [HttpGet]
        public async Task<IActionResult> Profile(string? tab = "info")
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", new { returnUrl = Url.Action("Profile", "Account", new { tab }) });
            }

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var bookings = await _context.Bookings
                .Include(b => b.Computer)
                    .ThenInclude(c => c.Zone)
                .Where(b => b.UserId == user.UserId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            var foodOrders = await _context.FoodOrders
                .Include(fo => fo.FoodOrderItems)
                    .ThenInclude(foi => foi.Food)
                .Where(fo => fo.UserId == user.UserId)
                .OrderByDescending(fo => fo.CreatedAt)
                .ToListAsync();

            var recharges = await _context.Recharges
                .Include(r => r.Method)
                .Where(r => r.UserId == user.UserId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var transactions = await _context.WalletTransactions
                .Where(w => w.UserId == user.UserId)
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();

            var paymentMethods = await _context.PaymentMethods
                .Where(p => p.IsActive)
                .ToListAsync();

            var bookingDtos = bookings.Select(b =>
            {
                var duration = (b.EndTime.HasValue && b.EndTime.Value > b.StartTime)
                    ? (b.EndTime.Value - b.StartTime).TotalHours
                    : 2.0;
                var zonePrice = b.Computer?.Zone?.PricePerHour ?? 10000;
                var estCost = (decimal)duration * zonePrice;

                var relFoods = foodOrders.Where(fo => fo.BookingId == b.BookingId).ToList();
                var foodItemList = new List<string>();
                decimal foodTotal = 0;
                foreach (var order in relFoods)
                {
                    foodTotal += order.TotalAmount;
                    foreach (var item in order.FoodOrderItems)
                    {
                        foodItemList.Add($"{item.Food.Name} (x{item.Quantity})");
                    }
                }

                bool canCancel = (b.Status == "confirmed" || b.Status == "pending") && b.StartTime > DateTime.Now;

                return new UserBookingDto
                {
                    BookingId = b.BookingId,
                    ComputerId = b.ComputerId,
                    ComputerName = b.Computer?.ComputerName ?? $"#{b.ComputerId}",
                    ZoneName = b.Computer?.Zone?.ZoneName ?? "Standard",
                    PricePerHour = zonePrice,
                    StartTime = b.StartTime,
                    EndTime = b.EndTime,
                    DurationHours = Math.Round(duration, 1),
                    EstimatedCost = estCost,
                    Status = b.Status,
                    CreatedAt = b.CreatedAt,
                    CancelledAt = b.CancelledAt,
                    CancelReason = b.CancelReason,
                    CanCancel = canCancel,
                    FoodItems = foodItemList,
                    FoodTotal = foodTotal
                };
            }).ToList();

            var rechargeDtos = recharges.Select(r => new UserRechargeDto
            {
                RechargeId = r.RechargeId,
                Amount = r.Amount,
                MethodName = r.Method?.Name ?? "Chuyển khoản QR",
                MethodCode = r.Method?.Code ?? "BANK_QR",
                Status = r.Status,
                TransactionCode = r.TransactionCode ?? ("RC-" + r.RechargeId.ToString("D4")),
                CreatedAt = r.CreatedAt
            }).ToList();

            var transDtos = transactions.Select(t =>
            {
                string typeDisplay = t.Type switch
                {
                    "recharge" => "Nạp tiền ví",
                    "booking" => "Thanh toán đặt máy",
                    "session_fee" => "Phí giờ chơi",
                    "food_order" => "Gọi món F&B",
                    "refund" => "Hoàn tiền",
                    _ => t.Type
                };
                bool isPositive = t.Type == "recharge" || t.Type == "refund";

                return new UserTransactionDto
                {
                    TransactionId = t.TransactionId,
                    Type = t.Type,
                    TypeDisplay = typeDisplay,
                    Amount = t.Amount,
                    IsPositive = isPositive,
                    ReferenceId = t.ReferenceId,
                    CreatedAt = t.CreatedAt
                };
            }).ToList();

            var vm = new UserProfileViewModel
            {
                User = user,
                Bookings = bookingDtos,
                Recharges = rechargeDtos,
                Transactions = transDtos,
                PaymentMethods = paymentMethods,
                TotalDeposited = recharges.Where(r => r.Status == "completed").Sum(r => r.Amount),
                TotalSpent = transactions.Where(t => t.Type != "recharge" && t.Type != "refund").Sum(t => t.Amount),
                TotalBookings = bookingDtos.Count,
                ActiveTab = string.IsNullOrEmpty(tab) ? "info" : tab.ToLower()
            };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập." });
            }

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
            {
                return Json(new { success = false, message = "Người dùng không tồn tại." });
            }

            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                var cleanEmail = dto.Email.Trim();
                var emailExists = await _context.Users.AnyAsync(u => u.Email == cleanEmail && u.UserId != user.UserId);
                if (emailExists)
                {
                    return Json(new { success = false, message = "Email này đã được tài khoản khác sử dụng." });
                }
                user.Email = cleanEmail;
            }

            if (!string.IsNullOrWhiteSpace(dto.Phone))
            {
                var cleanPhone = dto.Phone.Trim();
                var phoneExists = await _context.Users.AnyAsync(u => u.Phone == cleanPhone && u.UserId != user.UserId);
                if (phoneExists)
                {
                    return Json(new { success = false, message = "Số điện thoại này đã được tài khoản khác sử dụng." });
                }
                user.Phone = cleanPhone;
            }

            if (dto.DateOfBirth.HasValue)
            {
                user.DateOfBirth = dto.DateOfBirth.Value;
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Cập nhật thông tin thành công!" });
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập." });
            }

            if (string.IsNullOrWhiteSpace(dto.OldPassword) || string.IsNullOrWhiteSpace(dto.NewPassword))
            {
                return Json(new { success = false, message = "Vui lòng nhập đầy đủ thông tin mật khẩu." });
            }

            if (dto.NewPassword.Length < 6)
            {
                return Json(new { success = false, message = "Mật khẩu mới phải có ít nhất 6 ký tự." });
            }

            if (dto.NewPassword != dto.ConfirmNewPassword)
            {
                return Json(new { success = false, message = "Xác nhận mật khẩu mới không khớp." });
            }

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
            {
                return Json(new { success = false, message = "Người dùng không tồn tại." });
            }

            if (!VerifyPassword(dto.OldPassword, user.PasswordHash))
            {
                return Json(new { success = false, message = "Mật khẩu hiện tại không chính xác." });
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đổi mật khẩu thành công! Hãy lưu nhớ mật khẩu mới của bạn." });
        }

        [HttpPost]
        public async Task<IActionResult> Deposit([FromBody] DepositRequestDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để nạp tiền." });
            }

            if (dto.Amount < 10000)
            {
                return Json(new { success = false, message = "Số tiền nạp tối thiểu là 10.000 VNĐ." });
            }

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
            {
                return Json(new { success = false, message = "Tài khoản không tồn tại." });
            }

            var method = await _context.PaymentMethods.FindAsync(dto.MethodId)
                         ?? await _context.PaymentMethods.FirstOrDefaultAsync(p => p.Code == "BANK_QR")
                         ?? await _context.PaymentMethods.FirstOrDefaultAsync();

            int methodId = method?.MethodId ?? 1;
            string transCode = "CG" + DateTime.UtcNow.ToString("yyMMddHHmmss") + "_" + user.UserId;

            user.Balance += dto.Amount;

            var recharge = new Recharge
            {
                UserId = user.UserId,
                MethodId = methodId,
                Amount = dto.Amount,
                Status = "completed",
                TransactionCode = transCode,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Recharges.Add(recharge);
            await _context.SaveChangesAsync();

            var walletTrans = new WalletTransaction
            {
                UserId = user.UserId,
                Type = "recharge",
                Amount = dto.Amount,
                ReferenceId = recharge.RechargeId,
                CreatedAt = DateTime.UtcNow
            };
            _context.WalletTransactions.Add(walletTrans);
            await _context.SaveChangesAsync();

            HttpContext.Session.SetString("Balance", user.Balance.ToString());

            return Json(new
            {
                success = true,
                newBalance = user.Balance,
                newBalanceFormatted = user.Balance.ToString("N0") + " đ",
                amount = dto.Amount,
                transactionCode = transCode,
                methodName = method?.Name ?? "Chuyển khoản QR",
                createdAt = recharge.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                message = $"Nạp thành công {dto.Amount:N0} đ vào tài khoản! Số dư hiện tại: {user.Balance:N0} đ"
            });
        }

        [HttpPost]
        public async Task<IActionResult> CancelBooking([FromBody] CancelBookingDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập." });
            }

            var booking = await _context.Bookings
                .Include(b => b.Computer)
                .FirstOrDefaultAsync(b => b.BookingId == dto.BookingId && b.UserId == userId.Value);

            if (booking == null)
            {
                return Json(new { success = false, message = "Không tìm thấy thông tin đặt chỗ." });
            }

            if (booking.Status == "cancelled")
            {
                return Json(new { success = false, message = "Đơn đặt này đã được hủy trước đó." });
            }

            if (booking.Status == "completed")
            {
                return Json(new { success = false, message = "Không thể hủy phiên đặt đã hoàn thành." });
            }

            booking.Status = "cancelled";
            booking.CancelledAt = DateTime.UtcNow;
            booking.CancelReason = string.IsNullOrWhiteSpace(dto.Reason) ? "Khách hàng hủy đặt trên website" : dto.Reason.Trim();

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                bookingId = booking.BookingId,
                message = $"Đã hủy thành công đặt chỗ máy {booking.Computer?.ComputerName ?? ""} (Mã #{booking.BookingId})!"
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetWalletInfo()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Json(new { isAuthenticated = false });
            }

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
            {
                return Json(new { isAuthenticated = false });
            }

            var recentTrans = await _context.WalletTransactions
                .Where(w => w.UserId == user.UserId)
                .OrderByDescending(w => w.CreatedAt)
                .Take(5)
                .Select(t => new
                {
                    t.TransactionId,
                    t.Type,
                    t.Amount,
                    time = t.CreatedAt.ToLocalTime().ToString("dd/MM HH:mm")
                })
                .ToListAsync();

            return Json(new
            {
                isAuthenticated = true,
                userId = user.UserId,
                username = user.Username,
                balance = user.Balance,
                balanceFormatted = user.Balance.ToString("N0") + " đ",
                recentTransactions = recentTrans
            });
        }

        private int? GetCurrentUserId()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null && User.Identity?.IsAuthenticated == true)
            {
                var claimId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(claimId, out int id))
                {
                    userId = id;
                }
            }
            return userId;
        }

        private bool VerifyPassword(string inputPassword, string storedHash)
        {
            if (string.IsNullOrEmpty(inputPassword) || string.IsNullOrEmpty(storedHash))
                return false;

            // Xác thực chuẩn BCrypt đối chiếu với hash lưu trong CSDL
            try
            {
                if (BCrypt.Net.BCrypt.Verify(inputPassword, storedHash))
                    return true;
            }
            catch {}

            // So khớp trực tiếp (nếu mật khẩu lưu dạng plaintext)
            return inputPassword == storedHash;
        }
    }
}
