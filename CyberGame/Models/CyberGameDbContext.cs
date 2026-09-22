using System;
using System.Collections.Generic;
using CyberGame.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace CyberGame.Models;

public partial class CyberGameDbContext : DbContext
{
    public CyberGameDbContext()
    {
    }

    public CyberGameDbContext(DbContextOptions<CyberGameDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Booking> Bookings { get; set; }

    public virtual DbSet<ChatMessage> ChatMessages { get; set; }

    public virtual DbSet<ChatSession> ChatSessions { get; set; }

    public virtual DbSet<Computer> Computers { get; set; }

    public virtual DbSet<Equipment> Equipment { get; set; }

    public virtual DbSet<Food> Foods { get; set; }

    public virtual DbSet<FoodOrder> FoodOrders { get; set; }

    public virtual DbSet<FoodOrderItem> FoodOrderItems { get; set; }

    public virtual DbSet<KnowledgeBase> KnowledgeBases { get; set; }

    public virtual DbSet<PaymentMethod> PaymentMethods { get; set; }

    public virtual DbSet<Recharge> Recharges { get; set; }

    public virtual DbSet<Session> Sessions { get; set; }

    public virtual DbSet<SupportTicket> SupportTickets { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<WalletTransaction> WalletTransactions { get; set; }

    public virtual DbSet<Zone> Zones { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("Server=localhost;Database=CyberGameDB;User Id=sa;Password=123456;TrustServerCertificate=True;MultipleActiveResultSets=true;");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.BookingId).HasName("PK__BOOKINGS__5DE3A5B12FBC0937");

            entity.ToTable("BOOKINGS");

            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.CancelReason)
                .HasMaxLength(255)
                .HasColumnName("cancel_reason");
            entity.Property(e => e.CancelledAt)
                .HasColumnType("datetime")
                .HasColumnName("cancelled_at");
            entity.Property(e => e.ComputerId).HasColumnName("computer_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.EndTime)
                .HasColumnType("datetime")
                .HasColumnName("end_time");
            entity.Property(e => e.HoldExpiresAt)
                .HasColumnType("datetime")
                .HasColumnName("hold_expires_at");
            entity.Property(e => e.StartTime)
                .HasColumnType("datetime")
                .HasColumnName("start_time");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("pending")
                .HasColumnName("status");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Computer).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.ComputerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Bookings_Computers");

            entity.HasOne(d => d.User).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Bookings_Users");
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.MessageId).HasName("PK__CHAT_MES__0BBF6EE6DAAA51FB");

            entity.ToTable("CHAT_MESSAGES");

            entity.HasIndex(e => new { e.ChatSessionId, e.CreatedAt }, "IX_ChatMessages_SessionCreated");

            entity.Property(e => e.MessageId).HasColumnName("message_id");
            entity.Property(e => e.ChatSessionId).HasColumnName("chat_session_id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Sender)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("sender");

            entity.HasOne(d => d.ChatSession).WithMany(p => p.ChatMessages)
                .HasForeignKey(d => d.ChatSessionId)
                .HasConstraintName("FK_ChatMessages_Sessions");
        });

        modelBuilder.Entity<ChatSession>(entity =>
        {
            entity.HasKey(e => e.ChatSessionId).HasName("PK__CHAT_SES__B39120D47DF7CCF3");

            entity.ToTable("CHAT_SESSIONS");

            entity.Property(e => e.ChatSessionId).HasColumnName("chat_session_id");
            entity.Property(e => e.EndedAt)
                .HasColumnType("datetime")
                .HasColumnName("ended_at");
            entity.Property(e => e.StartedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("started_at");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("open")
                .HasColumnName("status");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.ChatSessions)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_ChatSessions_Users");
        });

        modelBuilder.Entity<Computer>(entity =>
        {
            entity.HasKey(e => e.ComputerId).HasName("PK__COMPUTER__805FE7FFD6224136");

            entity.ToTable("COMPUTERS");

            entity.HasIndex(e => new { e.ZoneId, e.Status }, "IX_Computers_ZoneStatus");

            entity.HasIndex(e => e.ComputerName, "UQ__COMPUTER__AC63C5EA7412DBA5").IsUnique();

            entity.Property(e => e.ComputerId).HasColumnName("computer_id");
            entity.Property(e => e.ComputerName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("computer_name");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("available")
                .HasColumnName("status");
            entity.Property(e => e.ZoneId).HasColumnName("zone_id");

            entity.HasOne(d => d.Zone).WithMany(p => p.Computers)
                .HasForeignKey(d => d.ZoneId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Computers_Zones");
        });

        modelBuilder.Entity<Equipment>(entity =>
        {
            entity.HasKey(e => e.EquipmentId).HasName("PK__EQUIPMEN__197068AFFEF19B44");

            entity.ToTable("EQUIPMENT");

            entity.Property(e => e.EquipmentId).HasColumnName("equipment_id");
            entity.Property(e => e.ComputerId).HasColumnName("computer_id");
            entity.Property(e => e.ConditionStatus)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue("good")
                .HasColumnName("condition_status");
            entity.Property(e => e.EquipmentName)
                .HasMaxLength(100)
                .HasColumnName("equipment_name");

            entity.HasOne(d => d.Computer).WithMany(p => p.Equipment)
                .HasForeignKey(d => d.ComputerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Equipment_Computers");
        });

        modelBuilder.Entity<Food>(entity =>
        {
            entity.HasKey(e => e.FoodId).HasName("PK__FOODS__2F4C4DD840606E4A");

            entity.ToTable("FOODS");

            entity.Property(e => e.FoodId).HasColumnName("food_id");
            entity.Property(e => e.Category)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("category");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("image_url");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("price");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("available")
                .HasColumnName("status");
        });

        modelBuilder.Entity<FoodOrder>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__FOOD_ORD__46596229B29B9F04");

            entity.ToTable("FOOD_ORDERS");

            entity.HasIndex(e => new { e.UserId, e.Status, e.CreatedAt }, "IX_FoodOrders_UserStatusTime");

            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue("cash")
                .HasColumnName("payment_method");
            entity.Property(e => e.SeatLabel)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("seat_label");
            entity.Property(e => e.SessionId).HasColumnName("session_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("pending")
                .HasColumnName("status");
            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("total_amount");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Booking).WithMany(p => p.FoodOrders)
                .HasForeignKey(d => d.BookingId)
                .HasConstraintName("FK_FoodOrders_Bookings");

            entity.HasOne(d => d.Session).WithMany(p => p.FoodOrders)
                .HasForeignKey(d => d.SessionId)
                .HasConstraintName("FK_FoodOrders_Sessions");

            entity.HasOne(d => d.User).WithMany(p => p.FoodOrders)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FoodOrders_Users");
        });

        modelBuilder.Entity<FoodOrderItem>(entity =>
        {
            entity.HasKey(e => e.OrderItemId).HasName("PK__FOOD_ORD__3764B6BC5308AD8B");

            entity.ToTable("FOOD_ORDER_ITEMS");

            entity.Property(e => e.OrderItemId).HasColumnName("order_item_id");
            entity.Property(e => e.FoodId).HasColumnName("food_id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.Quantity)
                .HasDefaultValue(1)
                .HasColumnName("quantity");
            entity.Property(e => e.Subtotal)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("subtotal");
            entity.Property(e => e.UnitPrice)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("unit_price");

            entity.HasOne(d => d.Food).WithMany(p => p.FoodOrderItems)
                .HasForeignKey(d => d.FoodId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FoodOrderItems_Foods");

            entity.HasOne(d => d.Order).WithMany(p => p.FoodOrderItems)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK_FoodOrderItems_Orders");
        });

        modelBuilder.Entity<KnowledgeBase>(entity =>
        {
            entity.HasKey(e => e.KnowledgeId).HasName("PK__KNOWLEDG__3A9CFBAFEBAB2EEE");

            entity.ToTable("KNOWLEDGE_BASE");

            entity.Property(e => e.KnowledgeId).HasColumnName("knowledge_id");
            entity.Property(e => e.Answer).HasColumnName("answer");
            entity.Property(e => e.Category)
                .HasMaxLength(50)
                .HasColumnName("category");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Question).HasColumnName("question");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("active")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<PaymentMethod>(entity =>
        {
            entity.HasKey(e => e.MethodId).HasName("PK__PAYMENT___747727B6C460BBF1");

            entity.ToTable("PAYMENT_METHODS");

            entity.HasIndex(e => e.Code, "UQ__PAYMENT___357D4CF9D636D1C3").IsUnique();

            entity.Property(e => e.MethodId).HasColumnName("method_id");
            entity.Property(e => e.Code)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Recharge>(entity =>
        {
            entity.HasKey(e => e.RechargeId).HasName("PK__RECHARGE__D3CC5F61AFBB424A");

            entity.ToTable("RECHARGES");

            entity.HasIndex(e => new { e.UserId, e.Status, e.CreatedAt }, "IX_Recharges_UserStatusTime");

            entity.Property(e => e.RechargeId).HasColumnName("recharge_id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.MethodId).HasColumnName("method_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("pending")
                .HasColumnName("status");
            entity.Property(e => e.TransactionCode)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("transaction_code");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Method).WithMany(p => p.Recharges)
                .HasForeignKey(d => d.MethodId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Recharges_PaymentMethods");

            entity.HasOne(d => d.User).WithMany(p => p.Recharges)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Recharges_Users");
        });

        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasKey(e => e.SessionId).HasName("PK__SESSIONS__69B13FDC3493D9C0");

            entity.ToTable("SESSIONS");

            entity.HasIndex(e => new { e.UserId, e.ComputerId, e.StartTime, e.EndTime }, "IX_Sessions_UserComputerTime");

            entity.Property(e => e.SessionId).HasColumnName("session_id");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.ComputerId).HasColumnName("computer_id");
            entity.Property(e => e.EndTime)
                .HasColumnType("datetime")
                .HasColumnName("end_time");
            entity.Property(e => e.StartTime)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("start_time");
            entity.Property(e => e.TotalCost)
                .HasDefaultValue(0.00m)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("total_cost");
            entity.Property(e => e.TotalMinutes)
                .HasDefaultValue(0)
                .HasColumnName("total_minutes");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Booking).WithMany(p => p.Sessions)
                .HasForeignKey(d => d.BookingId)
                .HasConstraintName("FK_Sessions_Bookings");

            entity.HasOne(d => d.Computer).WithMany(p => p.Sessions)
                .HasForeignKey(d => d.ComputerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Sessions_Computers");

            entity.HasOne(d => d.User).WithMany(p => p.Sessions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Sessions_Users");
        });

        modelBuilder.Entity<SupportTicket>(entity =>
        {
            entity.HasKey(e => e.TicketId).HasName("PK__SUPPORT___D596F96B742C6AB0");

            entity.ToTable("SUPPORT_TICKETS");

            entity.HasIndex(e => e.ChatSessionId, "UQ__SUPPORT___B39120D519065B89").IsUnique();

            entity.Property(e => e.TicketId).HasColumnName("ticket_id");
            entity.Property(e => e.AssignedStaff).HasColumnName("assigned_staff");
            entity.Property(e => e.ChatSessionId).HasColumnName("chat_session_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("open")
                .HasColumnName("status");
            entity.Property(e => e.Subject)
                .HasMaxLength(255)
                .HasColumnName("subject");

            entity.HasOne(d => d.AssignedStaffNavigation).WithMany(p => p.SupportTickets)
                .HasForeignKey(d => d.AssignedStaff)
                .HasConstraintName("FK_SupportTickets_Staff");

            entity.HasOne(d => d.ChatSession).WithOne(p => p.SupportTicket)
                .HasForeignKey<SupportTicket>(d => d.ChatSessionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SupportTickets_ChatSessions");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__USERS__B9BE370F446AD31C");

            entity.ToTable("USERS");

            entity.HasIndex(e => e.Username, "UQ__USERS__F3DBC572F092FB71").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Balance)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("balance");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("password_hash");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("phone");
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("member")
                .HasColumnName("role");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("active")
                .HasColumnName("status");
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("username");
        });

        modelBuilder.Entity<WalletTransaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId).HasName("PK__WALLET_T__85C600AFF62E14D4");

            entity.ToTable("WALLET_TRANSACTIONS");

            entity.Property(e => e.TransactionId).HasColumnName("transaction_id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.ReferenceId).HasColumnName("reference_id");
            entity.Property(e => e.Type)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("type");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.WalletTransactions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WalletTransactions_Users");
        });

        modelBuilder.Entity<Zone>(entity =>
        {
            entity.HasKey(e => e.ZoneId).HasName("PK__ZONES__80B401DFED5FD16D");

            entity.ToTable("ZONES");

            entity.Property(e => e.ZoneId).HasColumnName("zone_id");
            entity.Property(e => e.PricePerHour)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("price_per_hour");
            entity.Property(e => e.Specs)
                .HasMaxLength(255)
                .HasColumnName("specs");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("active")
                .HasColumnName("status");
            entity.Property(e => e.ZoneName)
                .HasMaxLength(50)
                .HasColumnName("zone_name");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
