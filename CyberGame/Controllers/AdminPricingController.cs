using System.Globalization;
using System.Threading.Tasks;
using CyberGame.Models;
using CyberGame.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CyberGame.Controllers
{
    // Task #20 - Quản lý bảng giá 
    // Controller riêng, không đụng AdminController.cs / CyberGameDbContext.cs.
    [Authorize(Roles = "admin")]
    public class AdminPricingController : Controller
    {
        // Khớp với cột price_per_hour DECIMAL(10,2) trong CyberGameDB.sql
        // => tối đa 8 chữ số phần nguyên + 2 chữ số thập phân.
        private const decimal MaxPrice = 99999999.99m;

        private readonly CyberGameDbContext _context;

        public AdminPricingController(CyberGameDbContext context)
        {
            _context = context;
        }

        // GET: /AdminPricing
        public async Task<IActionResult> Index()
        {
            var zones = await _context.Zones
                .OrderBy(z => z.ZoneId)
                .Select(z => new AdminPricingRowViewModel
                {
                    ZoneId = z.ZoneId,
                    ZoneName = z.ZoneName,
                    Specs = z.Specs,
                    PricePerHour = z.PricePerHour
                })
                .ToListAsync();

            return View(zones);
        }

        // POST: /AdminPricing/UpdatePrice
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePrice(int zoneId, string price)
        {
            // Parse thủ công theo InvariantCulture để tránh lỗi dấu phẩy/chấm
            // khi máy chủ chạy hệ điều hành có locale vi-VN (dấu thập phân là ",").
            if (!decimal.TryParse(
                    (price ?? string.Empty).Trim(),
                    NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture,
                    out var parsedPrice))
            {
                TempData["PricingError"] = $"Giá không hợp lệ: \"{price}\".";
                return RedirectToAction(nameof(Index));
            }

            if (parsedPrice <= 0)
            {
                TempData["PricingError"] = "Giá phải lớn hơn 0.";
                return RedirectToAction(nameof(Index));
            }

            if (parsedPrice > MaxPrice)
            {
                TempData["PricingError"] = $"Giá vượt quá giới hạn cho phép ({MaxPrice:N0}).";
                return RedirectToAction(nameof(Index));
            }

            var zone = await _context.Zones.FirstOrDefaultAsync(z => z.ZoneId == zoneId);
            if (zone == null)
            {
                TempData["PricingError"] = "Không tìm thấy khu vực (zone).";
                return RedirectToAction(nameof(Index));
            }

            // Làm tròn đúng scale(2) của cột DB trước khi lưu, tránh EF Core làm tròn ngầm khác ý.
            zone.PricePerHour = System.Math.Round(parsedPrice, 2, System.MidpointRounding.AwayFromZero);
            await _context.SaveChangesAsync();

            TempData["PricingSuccess"] = $"Đã cập nhật giá khu vực \"{zone.ZoneName}\" thành {zone.PricePerHour:N0}đ/giờ.";
            return RedirectToAction(nameof(Index));
        }
    }
}