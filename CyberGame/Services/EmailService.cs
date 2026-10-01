using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CyberGame.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<(bool Success, string? ErrorMessage)> SendOtpEmailAsync(string toEmail, string username, string otpCode, int expiryMinutes = 5)
        {
            // Luôn ghi log mã OTP ra console để hỗ trợ dev/test nhanh chóng ngay cả khi chưa gắn mật khẩu Gmail thật
            _logger.LogInformation("==================================================");
            _logger.LogInformation(" [CYBER GAME OTP SYSTEM] EMAIL: {Email} | OTP: {OtpCode}", toEmail, otpCode);
            _logger.LogInformation("==================================================");

            var senderEmail = _settings.SenderEmail?.Trim() ?? string.Empty;
            var senderPassword = _settings.SenderPassword?.Replace(" ", "").Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(senderEmail) || string.IsNullOrWhiteSpace(senderPassword))
            {
                _logger.LogWarning("EmailSettings chưa được cấu hình đầy đủ (SenderEmail / SenderPassword còn trống). Hệ thống đang ở chế độ giả lập gửi email.");
                return (false, "Chưa cấu hình tài khoản gửi email trong appsettings.json. Vui lòng xem mã OTP tại bảng thông báo kiểm thử hoặc console.");
            }

            try
            {
                using var client = new SmtpClient(_settings.SmtpServer, _settings.SmtpPort)
                {
                    EnableSsl = _settings.EnableSsl,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    Timeout = 10000
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, _settings.SenderName ?? "Cyber Game System"),
                    Subject = $"[Cyber Game] Mã xác thực đăng ký tài khoản: {otpCode}",
                    IsBodyHtml = true,
                    Body = GenerateOtpEmailHtml(username, otpCode, expiryMinutes)
                };

                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Đã gửi email xác thực OTP thành công đến {Email}", toEmail);
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi email OTP đến {Email}: {Message}", toEmail, ex.Message);
                return (false, $"Không thể kết nối đến máy chủ email: {ex.Message}");
            }
        }

        private static string GenerateOtpEmailHtml(string username, string otpCode, int expiryMinutes)
        {
            return $@"
<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Xác thực tài khoản Cyber Game</title>
</head>
<body style=""margin:0;padding:0;background-color:#070a0f;font-family:'Segoe UI',Roboto,Helvetica,Arial,sans-serif;color:#e2e8f0;"">
    <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" style=""background-color:#070a0f;padding:40px 10px;"">
        <tr>
            <td align=""center"">
                <table role=""presentation"" width=""100%"" style=""max-width:540px;background:#0d1117;border:1px solid #1f2937;border-radius:14px;overflow:hidden;box-shadow:0 12px 40px rgba(0,0,0,0.7);"" cellspacing=""0"" cellpadding=""0"">
                    <!-- HEADER -->
                    <tr>
                        <td style=""background:linear-gradient(135deg,#0052cc 0%,#00c0f7 100%);padding:28px 30px;text-align:center;"">
                            <div style=""display:inline-block;padding:8px 18px;background:rgba(0,0,0,0.3);border-radius:8px;border:1px solid rgba(255,255,255,0.2);"">
                                <span style=""font-size:22px;font-weight:900;letter-spacing:2px;color:#ffffff;"">⚡ CYBER GAME</span>
                            </div>
                            <p style=""margin:10px 0 0;color:rgba(255,255,255,0.9);font-size:13px;letter-spacing:1px;text-transform:uppercase;font-weight:600;"">HỆ THỐNG XÁC THỰC EMAIL THÀNH VIÊN</p>
                        </td>
                    </tr>
                    
                    <!-- BODY -->
                    <tr>
                        <td style=""padding:36px 32px;"">
                            <h2 style=""margin:0 0 16px;color:#ffffff;font-size:20px;font-weight:700;"">Xin chào <span style=""color:#38bdf8;"">{username}</span>!</h2>
                            <p style=""margin:0 0 20px;color:#94a3b8;font-size:14px;line-height:1.6;"">
                                Cảm ơn bạn đã lựa chọn gia nhập cộng đồng <strong>Cyber Game</strong>. Để xác thực email này là có thật và hoàn tất việc đăng ký tài khoản, bạn vui lòng nhập mã xác thực OTP sau đây:
                            </p>
                            
                            <!-- OTP BOX -->
                            <div style=""margin:28px 0;text-align:center;"">
                                <div style=""display:inline-block;background:#111827;border:2px dashed #0284c7;border-radius:12px;padding:18px 36px;box-shadow:0 0 25px rgba(2,132,199,0.25);"">
                                    <div style=""font-size:11px;color:#38bdf8;font-weight:800;letter-spacing:2px;margin-bottom:6px;text-transform:uppercase;"">MÃ XÁC THỰC OTP CỦA BẠN</div>
                                    <div style=""font-size:38px;font-weight:900;letter-spacing:12px;color:#ffffff;text-shadow:0 0 10px rgba(56,189,248,0.5);font-family:Consolas,monaco,monospace;"">
                                        {otpCode}
                                    </div>
                                </div>
                            </div>

                            <p style=""margin:0 0 16px;color:#f59e0b;font-size:13px;background:rgba(245,158,11,0.08);border-left:3px solid #f59e0b;padding:10px 14px;border-radius:0 6px 6px 0;"">
                                ⏳ Mã OTP này có hiệu lực trong vòng <strong>{expiryMinutes} phút</strong>. Sau thời gian này mã sẽ tự động vô hiệu hóa.
                            </p>

                            <p style=""margin:0 0 8px;color:#64748b;font-size:12px;line-height:1.5;"">
                                🔒 <strong>Lưu ý bảo mật:</strong> Tuyệt đối không chia sẻ mã xác thực này cho bất kỳ ai. Nhân viên Cyber Game sẽ không bao giờ yêu cầu cung cấp mã OTP của bạn.
                            </p>
                            <p style=""margin:0;color:#64748b;font-size:12px;line-height:1.5;"">
                                Nếu bạn không thực hiện yêu cầu đăng ký tại Cyber Game, xin hãy bỏ qua email này.
                            </p>
                        </td>
                    </tr>

                    <!-- FOOTER -->
                    <tr>
                        <td style=""background:#080b11;padding:20px 30px;border-top:1px solid #1f2937;text-align:center;"">
                            <p style=""margin:0 0 6px;color:#64748b;font-size:11px;"">© 2026 CYBER GAME MANAGEMENT SYSTEM. ALL RIGHTS RESERVED.</p>
                            <p style=""margin:0;color:#475569;font-size:11px;"">131 Đường Gaming, Việt Nam | contact@cybergame.vn</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
        }
    }
}
