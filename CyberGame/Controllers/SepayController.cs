using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CyberGame.Models;
using CyberGame.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CyberGame.Controllers
{
    /// <summary>
    /// Payload SePay gửi tới webhook.
    /// </summary>
    public class SepayWebhookDto
    {
        public long Id { get; set; }

        public string? Gateway { get; set; }

        public string? TransactionDate { get; set; }

        /// <summary>
        /// Tài khoản ngân hàng chính.
        /// Test ví dụ: 0000000002
        /// </summary>
        public string? AccountNumber { get; set; }

        /// <summary>
        /// Tài khoản ảo (VA) được khớp với giao dịch.
        /// Test ví dụ: SBSEPAYIN4NSU89YGZ0
        /// </summary>
        public string? SubAccount { get; set; }

        /// <summary>
        /// Mã thanh toán SePay nhận diện được.
        /// Ví dụ: CG000006
        /// </summary>
        public string? Code { get; set; }

        /// <summary>
        /// Nội dung chuyển khoản thực tế.
        /// </summary>
        public string? Content { get; set; }

        /// <summary>
        /// "in" hoặc "out"
        /// </summary>
        public string? TransferType { get; set; }

        /// <summary>
        /// Mô tả giao dịch.
        /// Không bắt buộc phải dùng.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Số tiền giao dịch, đơn vị VND.
        /// </summary>
        public decimal TransferAmount { get; set; }

        public decimal? Accumulated { get; set; }

        public string? ReferenceCode { get; set; }
    }

    [ApiController]
    [Route("api/sepay")]
    public class SepayController : ControllerBase
    {
        private readonly CyberGameDbContext _context;
        private readonly IConfiguration _config;
        private readonly ILogger<SepayController> _logger;

        public SepayController(
            CyberGameDbContext context,
            IConfiguration config,
            ILogger<SepayController> logger)
        {
            _context = context;
            _config = config;
            _logger = logger;
        }

        // =============================================================
        // POST /api/sepay/webhook
        // =============================================================
        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook()
        {
            // =========================================================
            // 1. ĐỌC RAW BODY
            //
            // HMAC của SePay được tính trên RAW BODY.
            // Không được deserialize JSON trước rồi serialize lại.
            // =========================================================

            Request.EnableBuffering();

            string rawBody;

            using (var reader = new StreamReader(
                Request.Body,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                leaveOpen: true))
            {
                rawBody = await reader.ReadToEndAsync();
            }

            Request.Body.Position = 0;

            if (string.IsNullOrWhiteSpace(rawBody))
            {
                _logger.LogWarning("SePay webhook: body rỗng.");

                return BadRequest(new
                {
                    success = false,
                    message = "Empty body"
                });
            }

            // =========================================================
            // 2. LẤY SECRET HMAC
            // =========================================================

            var secret = _config["SePay:WebhookSecret"];

            if (string.IsNullOrWhiteSpace(secret))
            {
                _logger.LogError(
                    "SePay:WebhookSecret chưa được cấu hình.");

                return StatusCode(500, new
                {
                    success = false,
                    message = "Webhook secret not configured"
                });
            }

            // =========================================================
            // 3. LẤY HEADER HMAC
            // =========================================================

            string signature =
                Request.Headers["X-SePay-Signature"].ToString().Trim();

            string timestampHeader =
                Request.Headers["X-SePay-Timestamp"].ToString().Trim();

            if (string.IsNullOrWhiteSpace(signature) ||
                string.IsNullOrWhiteSpace(timestampHeader))
            {
                _logger.LogWarning(
                    "SePay webhook thiếu X-SePay-Signature hoặc X-SePay-Timestamp.");

                return Unauthorized(new
                {
                    success = false,
                    message = "Missing HMAC headers"
                });
            }

            if (!long.TryParse(timestampHeader, out long timestamp))
            {
                _logger.LogWarning(
                    "SePay webhook có timestamp không hợp lệ: {Timestamp}",
                    timestampHeader);

                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid timestamp"
                });
            }

            // =========================================================
            // 4. CHỐNG REPLAY
            //
            // SePay khuyến nghị timestamp không lệch quá 5 phút.
            // =========================================================

            long nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (timestamp < nowUnix - 300 ||
                timestamp > nowUnix + 300)
            {
                _logger.LogWarning(
                    "SePay webhook hết hạn. Timestamp={Timestamp}, Now={Now}",
                    timestamp,
                    nowUnix);

                return Unauthorized(new
                {
                    success = false,
                    message = "Request expired"
                });
            }

            // =========================================================
            // 5. TÍNH HMAC
            //
            // SePay:
            //
            // signed_data = timestamp + "." + raw_body
            //
            // signature = sha256=<hex>
            // =========================================================

            string signedPayload =
                $"{timestamp}.{rawBody}";

            using var hmac =
                new HMACSHA256(
                    Encoding.UTF8.GetBytes(secret));

            byte[] hash =
                hmac.ComputeHash(
                    Encoding.UTF8.GetBytes(signedPayload));

            string expectedSignature =
                "sha256=" +
                Convert.ToHexString(hash)
                    .ToLowerInvariant();

            // So sánh constant-time
            byte[] expectedBytes =
                Encoding.UTF8.GetBytes(expectedSignature);

            byte[] receivedBytes =
                Encoding.UTF8.GetBytes(
                    signature.ToLowerInvariant());

            bool signatureValid =
                expectedBytes.Length == receivedBytes.Length &&
                CryptographicOperations.FixedTimeEquals(
                    expectedBytes,
                    receivedBytes);

            if (!signatureValid)
            {
                _logger.LogWarning(
                    "SePay webhook: HMAC không hợp lệ.");

                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid signature"
                });
            }

            // =========================================================
            // 6. DESERIALIZE JSON SAU KHI ĐÃ XÁC THỰC HMAC
            // =========================================================

            SepayWebhookDto? dto;

            try
            {
                dto = JsonSerializer.Deserialize<SepayWebhookDto>(
                    rawBody,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(
                    ex,
                    "SePay webhook: JSON không hợp lệ.");

                return BadRequest(new
                {
                    success = false,
                    message = "Invalid JSON"
                });
            }

            if (dto == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid payload"
                });
            }

            // =========================================================
            // 7. KIỂM TRA SEPAY TRANSACTION ID
            // =========================================================

            if (dto.Id <= 0)
            {
                _logger.LogWarning(
                    "SePay webhook: transaction ID không hợp lệ.");

                return BadRequest(new
                {
                    success = false,
                    message = "Invalid transaction ID"
                });
            }

            // =========================================================
            // 8. CHỈ NHẬN TIỀN VÀO
            // =========================================================

            if (!string.Equals(
                    dto.TransferType,
                    "in",
                    StringComparison.OrdinalIgnoreCase))
            {
                return Ok(new
                {
                    success = true
                });
            }

            if (dto.TransferAmount <= 0)
            {
                return Ok(new
                {
                    success = true
                });
            }

            // =========================================================
            // 9. KIỂM TRA TÀI KHOẢN NGÂN HÀNG
            //
            // Test:
            // AccountNumber = 0000000002
            //
            // Live:
            // AccountNumber = 1029037260
            //
            // appsettings:
            // "AccountNumber": "0000000002"
            // =========================================================

            var expectedAccount =
                _config["SePay:AccountNumber"];

            if (!string.IsNullOrWhiteSpace(expectedAccount))
            {
                bool accountMatched =
                    string.Equals(
                        dto.AccountNumber?.Trim(),
                        expectedAccount.Trim(),
                        StringComparison.OrdinalIgnoreCase);

                if (!accountMatched)
                {
                    _logger.LogWarning(
                        "SePay: tài khoản ngân hàng không khớp. " +
                        "Expected={Expected}, Got={Got}",
                        expectedAccount,
                        dto.AccountNumber);

                    // Không phải giao dịch của tài khoản CyberGame
                    return Ok(new
                    {
                        success = true
                    });
                }
            }

            // =========================================================
            // 10. KIỂM TRA VA
            //
            // Test:
            //
            // "VirtualAccount":
            // "SBSEPAYIN4NSU89YGZ0"
            //
            // Live không dùng VA:
            // để trống hoặc không cấu hình VirtualAccount.
            // =========================================================

            var expectedVa =
                _config["SePay:VirtualAccount"];

            if (!string.IsNullOrWhiteSpace(expectedVa))
            {
                bool vaMatched =
                    string.Equals(
                        dto.SubAccount?.Trim(),
                        expectedVa.Trim(),
                        StringComparison.OrdinalIgnoreCase);

                if (!vaMatched)
                {
                    _logger.LogWarning(
                        "SePay: VA không khớp. " +
                        "Expected={Expected}, Got={Got}",
                        expectedVa,
                        dto.SubAccount);

                    return Ok(new
                    {
                        success = true
                    });
                }
            }

            // =========================================================
            // 11. LẤY MÃ GIAO DỊCH CYBERGAME
            //
            // Ví dụ:
            // CG000006
            // =========================================================

            string prefix =
                (_config["SePay:CodePrefix"] ?? "CG").Trim();

            string? transactionCode = null;

            // ---------------------------------------------------------
            // Ưu tiên Code do SePay đã nhận diện
            // ---------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(dto.Code) &&
                dto.Code.StartsWith(
                    prefix,
                    StringComparison.OrdinalIgnoreCase))
            {
                transactionCode = dto.Code.Trim();
            }

            // ---------------------------------------------------------
            // Nếu Code không có thì tìm trong Content
            // ---------------------------------------------------------

            if (string.IsNullOrWhiteSpace(transactionCode) &&
                !string.IsNullOrWhiteSpace(dto.Content))
            {
                string pattern =
                    $@"\b{Regex.Escape(prefix)}\d{{6,20}}\b";

                Match match =
                    Regex.Match(
                        dto.Content,
                        pattern,
                        RegexOptions.IgnoreCase);

                if (match.Success)
                {
                    transactionCode =
                        match.Value;
                }
            }

            // Không phải giao dịch CyberGame
            if (string.IsNullOrWhiteSpace(transactionCode))
            {
                _logger.LogInformation(
                    "SePay: giao dịch {Id} không có mã CyberGame.",
                    dto.Id);

                return Ok(new
                {
                    success = true
                });
            }

            transactionCode =
                transactionCode.Trim();

            // =========================================================
            // 12. CHỐNG DUPLICATE THEO SEPAY TRANSACTION ID
            // =========================================================

            bool alreadyProcessed =
                await _context.Recharges.AnyAsync(
                    r => r.SepayTransactionId == dto.Id);

            if (alreadyProcessed)
            {
                _logger.LogInformation(
                    "SePay: transaction {Id} đã được xử lý trước đó.",
                    dto.Id);

                return Ok(new
                {
                    success = true
                });
            }

            // =========================================================
            // 13. TÌM RECHARGE
            // =========================================================

            var recharge =
                await _context.Recharges.FirstOrDefaultAsync(
                    r => r.TransactionCode == transactionCode);

            if (recharge == null)
            {
                _logger.LogWarning(
                    "SePay: không tìm thấy Recharge cho {Code}. " +
                    "SePayId={Id}",
                    transactionCode,
                    dto.Id);

                return Ok(new
                {
                    success = true
                });
            }

            // =========================================================
            // 14. CHỈ XỬ LÝ RECHARGE ĐANG PENDING
            // =========================================================

            if (!string.Equals(
                    recharge.Status,
                    "pending",
                    StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation(
                    "Recharge {RechargeId} đã có Status={Status}. Bỏ qua.",
                    recharge.RechargeId,
                    recharge.Status);

                return Ok(new
                {
                    success = true
                });
            }

            // =========================================================
            // 15. KIỂM TRA SỐ TIỀN TUYỆT ĐỐI
            //
            // 100000 == 100000  -> OK
            //  99000 != 100000  -> KHÔNG
            // 101000 != 100000  -> KHÔNG
            // =========================================================

            if (dto.TransferAmount != recharge.Amount)
            {
                _logger.LogWarning(
                    "SePay: {Code} sai số tiền. " +
                    "Expected={Expected}, Received={Received}, SePayId={Id}",
                    transactionCode,
                    recharge.Amount,
                    dto.TransferAmount,
                    dto.Id);

                // Không cộng tiền
                // Không completed
                // Không tạo WalletTransaction

                return Ok(new
                {
                    success = true,
                    message = "Amount mismatch"
                });
            }

            // Luôn dùng số tiền đã lưu trong Recharge.
            decimal amount = recharge.Amount;

            // =========================================================
            // 16. GIỚI HẠN DỮ LIỆU LƯU
            // =========================================================

            string? content =
                dto.Content != null &&
                dto.Content.Length > 500
                    ? dto.Content[..500]
                    : dto.Content;

            string? referenceCode =
                dto.ReferenceCode != null &&
                dto.ReferenceCode.Length > 100
                    ? dto.ReferenceCode[..100]
                    : dto.ReferenceCode;

            DateTime now =
                DateTime.UtcNow;

            // =========================================================
            // 17. DATABASE TRANSACTION
            // =========================================================

            await using var dbTransaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // =====================================================
                // 18. pending -> completed CHỈ 1 LẦN
                // =====================================================

                int rows =
                    await _context.Recharges
                        .Where(r =>
                            r.RechargeId == recharge.RechargeId &&
                            r.Status == "pending")
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(
                                r => r.Status,
                                "completed")

                            .SetProperty(
                                r => r.SepayTransactionId,
                                dto.Id)

                            .SetProperty(
                                r => r.ReferenceCode,
                                referenceCode)

                            .SetProperty(
                                r => r.Content,
                                content)

                            .SetProperty(
                                r => r.PaidAt,
                                now)

                            .SetProperty(
                                r => r.UpdatedAt,
                                now));

                // Một webhook khác đã xử lý trước
                if (rows == 0)
                {
                    await dbTransaction.RollbackAsync();

                    return Ok(new
                    {
                        success = true
                    });
                }

                // =====================================================
                // 19. CỘNG BALANCE CHO USER
                // =====================================================

                int userRows =
                    await _context.Users
                        .Where(u =>
                            u.UserId == recharge.UserId)
                        .ExecuteUpdateAsync(s =>
                            s.SetProperty(
                                u => u.Balance,
                                u => u.Balance + amount));

                if (userRows == 0)
                {
                    throw new InvalidOperationException(
                        $"Không tìm thấy UserId={recharge.UserId}.");
                }

                // =====================================================
                // 20. GHI WALLET TRANSACTION
                // =====================================================

                _context.WalletTransactions.Add(
                    new WalletTransaction
                    {
                        UserId = recharge.UserId,
                        Type = "recharge",
                        Amount = amount,
                        ReferenceId = recharge.RechargeId,
                        CreatedAt = now
                    });

                await _context.SaveChangesAsync();

                // =====================================================
                // 21. COMMIT
                // =====================================================

                await dbTransaction.CommitAsync();

                _logger.LogInformation(
                    "SePay: thanh toán thành công. " +
                    "RechargeId={RechargeId}, " +
                    "Code={Code}, " +
                    "Amount={Amount}, " +
                    "SePayId={SePayId}, " +
                    "UserId={UserId}",
                    recharge.RechargeId,
                    transactionCode,
                    amount,
                    dto.Id,
                    recharge.UserId);

                return Ok(new
                {
                    success = true
                });
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();

                _logger.LogError(
                    ex,
                    "SePay: lỗi xử lý webhook. " +
                    "Code={Code}, SePayId={SePayId}",
                    transactionCode,
                    dto.Id);

                return StatusCode(500, new
                {
                    success = false,
                    message = "Webhook processing failed"
                });
            }
        }
    }
}