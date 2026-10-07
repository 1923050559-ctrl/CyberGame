using CyberGame.Models;
using Microsoft.EntityFrameworkCore;

namespace CyberGame.Services
{
    public class BookingStatusService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BookingStatusService> _logger;

        public BookingStatusService(IServiceScopeFactory scopeFactory, ILogger<BookingStatusService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try { await RunOnce(ct); }
                catch (Exception ex) { _logger.LogError(ex, "BookingStatusService lỗi"); }

                try { await Task.Delay(TimeSpan.FromSeconds(30), ct); }
                catch (OperationCanceledException) { break; }
            }
        }

        private async Task RunOnce(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CyberGameDbContext>();
            var now = DateTime.Now;

            // 1) Quá hạn giữ chỗ -> expired, nhả máy (không đè máy đang bảo trì / đang có phiên chơi)
            // Quét cả booking "pending" và "confirmed" bị No-show (quá giờ giữ chỗ HoldExpiresAt mà khách chưa nhận máy)
            var expired = await db.Bookings
                .Where(b => (b.Status == "pending" || b.Status == "confirmed")
                         && b.HoldExpiresAt != null && b.HoldExpiresAt <= now
                         && !db.Sessions.Any(s => s.BookingId == b.BookingId && s.EndTime == null))
                .ToListAsync(ct);

            if (expired.Count > 0)
            {
                foreach (var b in expired) b.Status = "expired";
                await db.SaveChangesAsync(ct);

                var ids = expired.Select(b => b.ComputerId).Distinct().ToList();
                await db.Computers
                    .Where(c => ids.Contains(c.ComputerId) && c.Status == "in_use"
                        && !db.Bookings.Any(x => x.ComputerId == c.ComputerId && (x.Status == "confirmed" || x.Status == "pending") && x.HoldExpiresAt > now && x.EndTime > now)
                        && !db.Sessions.Any(s => s.ComputerId == c.ComputerId && s.EndTime == null))
                    .ExecuteUpdateAsync(s => s.SetProperty(c => c.Status, "available"), ct);
            }

            // 2) Đến giờ hẹn và còn trong thời gian giữ chỗ -> máy chuyển in_use
            var startedIds = await db.Bookings
                .Where(b => (b.Status == "pending" || b.Status == "confirmed")
                         && b.StartTime <= now
                         && b.EndTime > now
                         && (b.Status == "confirmed" || b.HoldExpiresAt > now))
                .Select(b => b.ComputerId).Distinct()
                .ToListAsync(ct);

            if (startedIds.Count > 0)
            {
                await db.Computers
                    .Where(c => startedIds.Contains(c.ComputerId) && c.Status == "available")
                    .ExecuteUpdateAsync(s => s.SetProperty(c => c.Status, "in_use"), ct);
            }
        }
    }
}