using System.Threading.Tasks;

namespace CyberGame.Services
{
    public interface IEmailService
    {
        Task<(bool Success, string? ErrorMessage)> SendOtpEmailAsync(string toEmail, string username, string otpCode, int expiryMinutes = 5);
    }
}
