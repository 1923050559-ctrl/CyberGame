using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CyberGame.Models.Entities;

namespace CyberGame.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên tài khoản hoặc email")]
        public string Username { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;

        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên tài khoản")]
        public string Username { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string Phone { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;

        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = null!;
    }

    public class UserProfileViewModel
    {
        public User User { get; set; } = null!;
        public List<UserBookingDto> Bookings { get; set; } = new List<UserBookingDto>();
        public List<UserRechargeDto> Recharges { get; set; } = new List<UserRechargeDto>();
        public List<UserTransactionDto> Transactions { get; set; } = new List<UserTransactionDto>();
        public List<PaymentMethod> PaymentMethods { get; set; } = new List<PaymentMethod>();

        public decimal TotalDeposited { get; set; }
        public decimal TotalSpent { get; set; }
        public int TotalBookings { get; set; }
        public string ActiveTab { get; set; } = "info";
    }

    public class UserBookingDto
    {
        public int BookingId { get; set; }
        public int ComputerId { get; set; }
        public string ComputerName { get; set; } = string.Empty;
        public string ZoneName { get; set; } = string.Empty;
        public decimal PricePerHour { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public double DurationHours { get; set; }
        public decimal EstimatedCost { get; set; }
        public string Status { get; set; } = "pending";
        public DateTime CreatedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? CancelReason { get; set; }
        public bool CanCancel { get; set; }
        public List<string> FoodItems { get; set; } = new List<string>();
        public decimal FoodTotal { get; set; }
    }

    public class UserRechargeDto
    {
        public int RechargeId { get; set; }
        public decimal Amount { get; set; }
        public string MethodName { get; set; } = string.Empty;
        public string MethodCode { get; set; } = string.Empty;
        public string Status { get; set; } = "pending";
        public string? TransactionCode { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UserTransactionDto
    {
        public int TransactionId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string TypeDisplay { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public bool IsPositive { get; set; }
        public int? ReferenceId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UpdateProfileDto
    {
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public DateOnly? DateOfBirth { get; set; }
    }

    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại")]
        public string OldPassword { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu mới tối thiểu 6 ký tự")]
        public string NewPassword { get; set; } = null!;

        [Compare("NewPassword", ErrorMessage = "Xác nhận mật khẩu mới không khớp")]
        public string ConfirmNewPassword { get; set; } = null!;
    }

    public class DepositRequestDto
    {
        public decimal Amount { get; set; }
        public int MethodId { get; set; } = 2; // Default VietQR
        public string? Note { get; set; }
    }

    public class CancelBookingDto
    {
        public int BookingId { get; set; }
        public string? Reason { get; set; }
    }
}

