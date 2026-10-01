using System.Linq;
using System.Threading.Tasks;
using CyberGame.Models;
using CyberGame.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CyberGame.Controllers
{
    // Task #17 - Quản lý trạng thái máy 
    // Controller riêng, không đụng AdminController.cs để tránh conflict với thành viên khác.
    [Authorize(Roles = "admin")]
    public class AdminMachineController : Controller
    {
        // Đúng theo CHECK constraint CK_Computers_Status trong CyberGameDB.sql
        private static readonly string[] ValidStatuses = { "available", "in_use", "maintenance", "offline" };

        private readonly CyberGameDbContext _context;

        public AdminMachineController(CyberGameDbContext context)
        {
            _context = context;
        }

        // GET: /AdminMachine
        public async Task<IActionResult> Index()
        {
            var now = DateTime.Now;

            var activeBookings = await _context.Bookings
                .Include(b => b.User)
                .Where(b => (b.Status == "pending" || b.Status == "confirmed")
                         && b.EndTime > now
                         && (b.Status == "confirmed" || b.HoldExpiresAt > now))
                .OrderBy(b => b.StartTime)
                .ToListAsync();

            var nextByComputer = activeBookings
                .GroupBy(b => b.ComputerId)
                .ToDictionary(g => g.Key, g => g.First());

            var computers = await _context.Computers
                .Include(c => c.Zone)
                .OrderBy(c => c.ZoneId)
                .ThenBy(c => c.ComputerName)
                .Select(c => new AdminMachineRowViewModel
                {
                    ComputerId = c.ComputerId,
                    ComputerName = c.ComputerName,
                    ZoneId = c.ZoneId,
                    ZoneName = c.Zone.ZoneName,
                    Status = c.Status
                })
                .ToListAsync();

            foreach (var c in computers)
            {
                if (nextByComputer.TryGetValue(c.ComputerId, out var b))
                {
                    c.BookingId = b.BookingId;
                    c.BookingCustomer = b.User?.Username;
                    c.BookingStatus = b.Status;
                    c.BookingStart = b.StartTime;
                    c.HoldExpiresAt = b.HoldExpiresAt;
                }
            }

            return View(computers);
        }

        // POST: /AdminMachine/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int computerId, string status)
        {
            if (string.IsNullOrWhiteSpace(status) || !ValidStatuses.Contains(status))
            {
                TempData["MachineError"] = $"Trạng thái không hợp lệ: \"{status}\".";
                return RedirectToAction(nameof(Index));
            }

            var computer = await _context.Computers.FirstOrDefaultAsync(c => c.ComputerId == computerId);
            if (computer == null)
            {
                TempData["MachineError"] = "Không tìm thấy máy.";
                return RedirectToAction(nameof(Index));
            }

            var now = DateTime.Now;

            // Đang có phiên chơi chưa kết thúc thì không đổi sang trạng thái nào khác in_use
            if (status != "in_use")
            {
                bool hasActiveSession = await _context.Sessions
                    .AnyAsync(s => s.ComputerId == computerId && s.EndTime == null);
                if (hasActiveSession)
                {
                    TempData["MachineError"] = $"Máy \"{computer.ComputerName}\" đang có phiên chơi chưa kết thúc.";
                    return RedirectToAction(nameof(Index));
                }
            }

            // Booking còn hiệu lực trên máy này
            var liveBookings = _context.Bookings.Where(b => b.ComputerId == computerId
                && (b.Status == "pending" || b.Status == "confirmed")
                && b.EndTime > now
                && (b.Status == "confirmed" || b.HoldExpiresAt > now));

            if (status == "maintenance" || status == "offline")
            {
                if (await liveBookings.AnyAsync())
                {
                    TempData["MachineError"] = $"Máy \"{computer.ComputerName}\" đang có booking, hãy huỷ booking trước khi chuyển sang bảo trì/ngừng hoạt động.";
                    return RedirectToAction(nameof(Index));
                }
            }
            else if (status == "available")
            {
                if (await liveBookings.AnyAsync(b => b.StartTime <= now))
                {
                    TempData["MachineError"] = $"Máy \"{computer.ComputerName}\" đang được giữ cho khách, không thể chuyển sang \"Trống\".";
                    return RedirectToAction(nameof(Index));
                }
            }

            computer.Status = status;
            await _context.SaveChangesAsync();

            TempData["MachineSuccess"] = $"Đã cập nhật trạng thái máy \"{computer.ComputerName}\" thành \"{status}\".";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(int bookingId)
        {
            var b = await _context.Bookings.FirstOrDefaultAsync(x => x.BookingId == bookingId);
            if (b == null || (b.Status != "pending" && b.Status != "confirmed"))
            {
                TempData["MachineError"] = "Không tìm thấy booking hoặc booking đã kết thúc.";
                return RedirectToAction(nameof(Index));
            }

            b.Status = "cancelled";
            b.CancelledAt = DateTime.Now;
            b.CancelReason = "Nhân viên huỷ tại trang quản lý máy";
            await _context.SaveChangesAsync();

            await _context.Computers
                .Where(c => c.ComputerId == b.ComputerId && c.Status == "in_use"
                    && !_context.Sessions.Any(s => s.ComputerId == c.ComputerId && s.EndTime == null))
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.Status, "available"));

            TempData["MachineSuccess"] = $"Đã huỷ booking #{bookingId} và nhả máy.";
            return RedirectToAction(nameof(Index));
        }
    }
}