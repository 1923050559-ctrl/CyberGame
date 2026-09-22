## 🚀 HƯỚNG DẪN CÀI ĐẶT & CHẠY DỰ ÁN
### Yêu cầu hệ thống
- Đã cài đặt [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
- Đã cài đặt **SQL Server** (hoặc SQL Server Express) và **SSMS**.
### Bước 1: Khôi phục Cơ sở Dữ liệu
1. Mở **SQL Server Management Studio (SSMS)**.
2. Mở file `CyberGameDB.sql` đính kèm trong thư mục dự án.
3. Bấm **Execute** (F5) để tạo database và chèn dữ liệu mẫu.
### Bước 2: Cấu hình Chuỗi kết nối (Connection String)
Mở file `appsettings.json` trong dự án `CyberGame` và điều chỉnh thông tin đăng nhập SQL Server của bạn nếu cần:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=CyberGameDB;User Id=sa;Password=123456;TrustServerCertificate=True;"
}
