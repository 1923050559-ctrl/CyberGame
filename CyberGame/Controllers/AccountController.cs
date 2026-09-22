using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using CyberGame.Models;
using CyberGame.Models.Entities;
using CyberGame.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CyberGame.Controllers
{
    public class AccountController : Controller
    {
        private readonly CyberGameDbContext _context;

        public AccountController(CyberGameDbContext context)
        {
            _context = context;
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
            var email = model.Email.Trim();

            if (await _context.Users.AnyAsync(u => u.Username == username))
            {
                ModelState.AddModelError("Username", "Tên tài khoản này đã được sử dụng.");
                return View(model);
            }

            if (await _context.Users.AnyAsync(u => u.Email == email))
            {
                ModelState.AddModelError("Email", "Email này đã được đăng ký.");
                return View(model);
            }

            var newUser = new User
            {
                Username = username,
                Email = email,
                Phone = model.Phone.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Role = "member",
                Status = "active",
                Balance = 0,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            // Tự động đăng nhập sau khi đăng ký
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, newUser.UserId.ToString()),
                new Claim(ClaimTypes.Name, newUser.Username),
                new Claim(ClaimTypes.Role, newUser.Role),
                new Claim("Balance", "0")
            };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity));

            HttpContext.Session.SetInt32("UserId", newUser.UserId);
            HttpContext.Session.SetString("Username", newUser.Username);
            HttpContext.Session.SetString("Role", newUser.Role);

            return RedirectToAction("Index", "Home");
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
