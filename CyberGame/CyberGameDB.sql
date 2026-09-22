-- =============================================================================
-- CƠ SỞ DỮ LIỆU QUẢN LÝ TIỆM NET (CYBER GAME MANAGEMENT SYSTEM)
-- Hệ quản trị CSDL: Microsoft SQL Server (T-SQL)
-- Phiên bản: 2.0 (Hỗ trợ Unicode NVARCHAR & Tích hợp đầy đủ dữ liệu mẫu)
-- =============================================================================

USE master;
GO

-- 1. TẠO HOẶC LÀM MỚI DATABASE
IF DB_ID('CyberGameDB') IS NOT NULL
BEGIN
    ALTER DATABASE CyberGameDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE CyberGameDB;
END
GO

CREATE DATABASE CyberGameDB;
GO

USE CyberGameDB;
GO

-- =============================================================================
-- 2. TẠO CÁC BẢNG (TABLES) THEO CLASS DIAGRAM
-- =============================================================================

-- BẢNG 1: USERS (Tài khoản người dùng & Quản trị viên)
-- Lưu ý: Chỉ khởi tạo duy nhất tài khoản admin theo yêu cầu
CREATE TABLE USERS (
    user_id INT IDENTITY(1,1) PRIMARY KEY,
    username VARCHAR(50) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    phone VARCHAR(20) NULL,
    email VARCHAR(100) NULL,
    date_of_birth DATE NULL,
    balance DECIMAL(12,2) NOT NULL CONSTRAINT DF_Users_Balance DEFAULT 0.00,
    role VARCHAR(20) NOT NULL CONSTRAINT DF_Users_Role DEFAULT 'member',
    status VARCHAR(20) NOT NULL CONSTRAINT DF_Users_Status DEFAULT 'active',
    created_at DATETIME NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT CK_Users_Role CHECK (role IN ('admin', 'staff', 'member')),
    CONSTRAINT CK_Users_Status CHECK (status IN ('active', 'banned', 'inactive')),
    CONSTRAINT CK_Users_Balance CHECK (balance >= 0)
);

-- BẢNG 2: PAYMENT_METHODS (Phương thức thanh toán)
CREATE TABLE PAYMENT_METHODS (
    method_id INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(100) NOT NULL,
    code VARCHAR(30) NOT NULL UNIQUE,
    is_active BIT NOT NULL CONSTRAINT DF_PaymentMethods_IsActive DEFAULT 1,
    created_at DATETIME NOT NULL CONSTRAINT DF_PaymentMethods_CreatedAt DEFAULT CURRENT_TIMESTAMP
);

-- BẢNG 3: RECHARGES (Lịch sử nạp tiền vào tài khoản)
CREATE TABLE RECHARGES (
    recharge_id INT IDENTITY(1,1) PRIMARY KEY,
    user_id INT NOT NULL,
    amount DECIMAL(12,2) NOT NULL,
    method_id INT NOT NULL,
    status VARCHAR(20) NOT NULL CONSTRAINT DF_Recharges_Status DEFAULT 'pending',
    transaction_code VARCHAR(100) NULL,
    created_at DATETIME NOT NULL CONSTRAINT DF_Recharges_CreatedAt DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NULL,
    CONSTRAINT FK_Recharges_Users FOREIGN KEY (user_id) REFERENCES USERS(user_id),
    CONSTRAINT FK_Recharges_PaymentMethods FOREIGN KEY (method_id) REFERENCES PAYMENT_METHODS(method_id),
    CONSTRAINT CK_Recharges_Amount CHECK (amount > 0),
    CONSTRAINT CK_Recharges_Status CHECK (status IN ('pending', 'completed', 'failed', 'cancelled'))
);

-- BẢNG 4: WALLET_TRANSACTIONS (Biến động số dư ví)
CREATE TABLE WALLET_TRANSACTIONS (
    transaction_id INT IDENTITY(1,1) PRIMARY KEY,
    user_id INT NOT NULL,
    type VARCHAR(30) NOT NULL,
    amount DECIMAL(12,2) NOT NULL,
    reference_id INT NULL,
    created_at DATETIME NOT NULL CONSTRAINT DF_WalletTransactions_CreatedAt DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_WalletTransactions_Users FOREIGN KEY (user_id) REFERENCES USERS(user_id),
    CONSTRAINT CK_WalletTransactions_Type CHECK (type IN ('recharge', 'session_fee', 'food_order', 'refund'))
);

-- BẢNG 5: ZONES (Các khu vực phòng máy)
CREATE TABLE ZONES (
    zone_id INT IDENTITY(1,1) PRIMARY KEY,
    zone_name NVARCHAR(50) NOT NULL,
    specs NVARCHAR(255) NULL,
    price_per_hour DECIMAL(10,2) NOT NULL,
    status VARCHAR(20) NOT NULL CONSTRAINT DF_Zones_Status DEFAULT 'active',
    CONSTRAINT CK_Zones_Price CHECK (price_per_hour >= 0),
    CONSTRAINT CK_Zones_Status CHECK (status IN ('active', 'maintenance', 'inactive'))
);

-- BẢNG 6: COMPUTERS (Danh sách máy trạm)
CREATE TABLE COMPUTERS (
    computer_id INT IDENTITY(1,1) PRIMARY KEY,
    zone_id INT NOT NULL,
    computer_name VARCHAR(50) NOT NULL UNIQUE,
    status VARCHAR(20) NOT NULL CONSTRAINT DF_Computers_Status DEFAULT 'available',
    CONSTRAINT FK_Computers_Zones FOREIGN KEY (zone_id) REFERENCES ZONES(zone_id),
    CONSTRAINT CK_Computers_Status CHECK (status IN ('available', 'in_use', 'maintenance', 'offline'))
);

-- BẢNG 7: EQUIPMENT (Thiết bị ngoại vi & linh kiện gắn với máy)
CREATE TABLE EQUIPMENT (
    equipment_id INT IDENTITY(1,1) PRIMARY KEY,
    computer_id INT NOT NULL,
    equipment_name NVARCHAR(100) NOT NULL,
    condition_status VARCHAR(30) NOT NULL CONSTRAINT DF_Equipment_Status DEFAULT 'good',
    CONSTRAINT FK_Equipment_Computers FOREIGN KEY (computer_id) REFERENCES COMPUTERS(computer_id),
    CONSTRAINT CK_Equipment_Status CHECK (condition_status IN ('good', 'repaired', 'broken'))
);

-- BẢNG 8: BOOKINGS (Lịch đặt máy trước)
CREATE TABLE BOOKINGS (
    booking_id INT IDENTITY(1,1) PRIMARY KEY,
    user_id INT NOT NULL,
    computer_id INT NOT NULL,
    start_time DATETIME NOT NULL,
    end_time DATETIME NULL,
    hold_expires_at DATETIME NULL,
    status VARCHAR(20) NOT NULL CONSTRAINT DF_Bookings_Status DEFAULT 'pending',
    created_at DATETIME NOT NULL CONSTRAINT DF_Bookings_CreatedAt DEFAULT CURRENT_TIMESTAMP,
    cancelled_at DATETIME NULL,
    cancel_reason NVARCHAR(255) NULL,
    CONSTRAINT FK_Bookings_Users FOREIGN KEY (user_id) REFERENCES USERS(user_id),
    CONSTRAINT FK_Bookings_Computers FOREIGN KEY (computer_id) REFERENCES COMPUTERS(computer_id),
    CONSTRAINT CK_Bookings_Status CHECK (status IN ('pending', 'confirmed', 'completed', 'cancelled', 'expired'))
);

-- BẢNG 9: SESSIONS (Phiên chơi thực tế tại máy)
CREATE TABLE SESSIONS (
    session_id INT IDENTITY(1,1) PRIMARY KEY,
    booking_id INT NULL,
    user_id INT NOT NULL,
    computer_id INT NOT NULL,
    start_time DATETIME NOT NULL CONSTRAINT DF_Sessions_StartTime DEFAULT CURRENT_TIMESTAMP,
    end_time DATETIME NULL,
    total_minutes INT NULL CONSTRAINT DF_Sessions_Minutes DEFAULT 0,
    total_cost DECIMAL(10,2) NULL CONSTRAINT DF_Sessions_Cost DEFAULT 0.00,
    CONSTRAINT FK_Sessions_Bookings FOREIGN KEY (booking_id) REFERENCES BOOKINGS(booking_id),
    CONSTRAINT FK_Sessions_Users FOREIGN KEY (user_id) REFERENCES USERS(user_id),
    CONSTRAINT FK_Sessions_Computers FOREIGN KEY (computer_id) REFERENCES COMPUTERS(computer_id),
    CONSTRAINT CK_Sessions_Cost CHECK (total_cost >= 0),
    CONSTRAINT CK_Sessions_Minutes CHECK (total_minutes >= 0)
);

-- BẢNG 10: FOODS (Thực đơn món ăn, thức uống, tiện ích)
CREATE TABLE FOODS (
    food_id INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(100) NOT NULL,
    category VARCHAR(50) NOT NULL,
    price DECIMAL(10,2) NOT NULL,
    image_url VARCHAR(255) NULL,
    status VARCHAR(20) NOT NULL CONSTRAINT DF_Foods_Status DEFAULT 'available',
    CONSTRAINT CK_Foods_Price CHECK (price >= 0),
    CONSTRAINT CK_Foods_Status CHECK (status IN ('available', 'out_of_stock', 'hidden')),
    CONSTRAINT CK_Foods_Category CHECK (category IN ('food', 'drink', 'snack', 'utility'))
);

-- BẢNG 11: FOOD_ORDERS (Đơn đặt món F&B)
CREATE TABLE FOOD_ORDERS (
    order_id INT IDENTITY(1,1) PRIMARY KEY,
    user_id INT NOT NULL,
    booking_id INT NULL,
    session_id INT NULL,
    seat_label VARCHAR(20) NULL,
    payment_method VARCHAR(30) NOT NULL CONSTRAINT DF_FoodOrders_Payment DEFAULT 'cash',
    status VARCHAR(20) NOT NULL CONSTRAINT DF_FoodOrders_Status DEFAULT 'pending',
    total_amount DECIMAL(12,2) NOT NULL CONSTRAINT DF_FoodOrders_Total DEFAULT 0.00,
    created_at DATETIME NOT NULL CONSTRAINT DF_FoodOrders_CreatedAt DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NULL,
    CONSTRAINT FK_FoodOrders_Users FOREIGN KEY (user_id) REFERENCES USERS(user_id),
    CONSTRAINT FK_FoodOrders_Bookings FOREIGN KEY (booking_id) REFERENCES BOOKINGS(booking_id),
    CONSTRAINT FK_FoodOrders_Sessions FOREIGN KEY (session_id) REFERENCES SESSIONS(session_id),
    CONSTRAINT CK_FoodOrders_Total CHECK (total_amount >= 0),
    CONSTRAINT CK_FoodOrders_Status CHECK (status IN ('pending', 'preparing', 'delivered', 'completed', 'cancelled'))
);

-- BẢNG 12: FOOD_ORDER_ITEMS (Chi tiết từng món trong đơn F&B)
CREATE TABLE FOOD_ORDER_ITEMS (
    order_item_id INT IDENTITY(1,1) PRIMARY KEY,
    order_id INT NOT NULL,
    food_id INT NOT NULL,
    quantity INT NOT NULL CONSTRAINT DF_FoodOrderItems_Qty DEFAULT 1,
    unit_price DECIMAL(10,2) NOT NULL,
    subtotal DECIMAL(10,2) NOT NULL,
    CONSTRAINT FK_FoodOrderItems_Orders FOREIGN KEY (order_id) REFERENCES FOOD_ORDERS(order_id) ON DELETE CASCADE,
    CONSTRAINT FK_FoodOrderItems_Foods FOREIGN KEY (food_id) REFERENCES FOODS(food_id),
    CONSTRAINT CK_FoodOrderItems_Qty CHECK (quantity > 0),
    CONSTRAINT CK_FoodOrderItems_UnitPrice CHECK (unit_price >= 0),
    CONSTRAINT CK_FoodOrderItems_Subtotal CHECK (subtotal >= 0)
);

-- BẢNG 13: CHAT_SESSIONS (Phiên trao đổi CSKH trực tuyến)
CREATE TABLE CHAT_SESSIONS (
    chat_session_id INT IDENTITY(1,1) PRIMARY KEY,
    user_id INT NULL,
    started_at DATETIME NOT NULL CONSTRAINT DF_ChatSessions_StartedAt DEFAULT CURRENT_TIMESTAMP,
    ended_at DATETIME NULL,
    status VARCHAR(20) NOT NULL CONSTRAINT DF_ChatSessions_Status DEFAULT 'open',
    CONSTRAINT FK_ChatSessions_Users FOREIGN KEY (user_id) REFERENCES USERS(user_id),
    CONSTRAINT CK_ChatSessions_Status CHECK (status IN ('open', 'closed'))
);

-- BẢNG 14: CHAT_MESSAGES (Tin nhắn trao đổi)
CREATE TABLE CHAT_MESSAGES (
    message_id INT IDENTITY(1,1) PRIMARY KEY,
    chat_session_id INT NOT NULL,
    sender VARCHAR(20) NOT NULL,
    content NVARCHAR(MAX) NOT NULL,
    created_at DATETIME NOT NULL CONSTRAINT DF_ChatMessages_CreatedAt DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_ChatMessages_Sessions FOREIGN KEY (chat_session_id) REFERENCES CHAT_SESSIONS(chat_session_id) ON DELETE CASCADE,
    CONSTRAINT CK_ChatMessages_Sender CHECK (sender IN ('user', 'staff', 'bot'))
);

-- BẢNG 15: SUPPORT_TICKETS (Phiếu yêu cầu hỗ trợ kỹ thuật)
CREATE TABLE SUPPORT_TICKETS (
    ticket_id INT IDENTITY(1,1) PRIMARY KEY,
    chat_session_id INT NOT NULL UNIQUE,
    assigned_staff INT NULL,
    subject NVARCHAR(255) NOT NULL,
    status VARCHAR(20) NOT NULL CONSTRAINT DF_SupportTickets_Status DEFAULT 'open',
    created_at DATETIME NOT NULL CONSTRAINT DF_SupportTickets_CreatedAt DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_SupportTickets_ChatSessions FOREIGN KEY (chat_session_id) REFERENCES CHAT_SESSIONS(chat_session_id),
    CONSTRAINT FK_SupportTickets_Staff FOREIGN KEY (assigned_staff) REFERENCES USERS(user_id),
    CONSTRAINT CK_SupportTickets_Status CHECK (status IN ('open', 'in_progress', 'resolved', 'closed'))
);

-- BẢNG 16: KNOWLEDGE_BASE (Cơ sở kiến thức hỏi đáp tự động)
CREATE TABLE KNOWLEDGE_BASE (
    knowledge_id INT IDENTITY(1,1) PRIMARY KEY,
    question NVARCHAR(MAX) NOT NULL,
    answer NVARCHAR(MAX) NOT NULL,
    category NVARCHAR(50) NULL,
    status VARCHAR(20) NOT NULL CONSTRAINT DF_KnowledgeBase_Status DEFAULT 'active',
    created_at DATETIME NOT NULL CONSTRAINT DF_KnowledgeBase_CreatedAt DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NULL,
    CONSTRAINT CK_KnowledgeBase_Status CHECK (status IN ('active', 'inactive'))
);
GO

-- =============================================================================
-- 3. TẠO INDEXES TỐI ƯU TRUY VẤN
-- =============================================================================
CREATE INDEX IX_Computers_ZoneStatus ON COMPUTERS(zone_id, status);
CREATE INDEX IX_Sessions_UserComputerTime ON SESSIONS(user_id, computer_id, start_time, end_time);
CREATE INDEX IX_FoodOrders_UserStatusTime ON FOOD_ORDERS(user_id, status, created_at);
CREATE INDEX IX_Recharges_UserStatusTime ON RECHARGES(user_id, status, created_at);
CREATE INDEX IX_ChatMessages_SessionCreated ON CHAT_MESSAGES(chat_session_id, created_at);
GO

-- =============================================================================
-- 4. INSERT DỮ LIỆU
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 4.1. BẢNG USERS: CHỈ TẠO DUY NHẤT TÀI KHOẢN ADMIN (KHÔNG CÓ DỮ LIỆU MẪU MEMBER)
-- Mật khẩu mặc định: Admin@123
-- Hash Bcrypt: $2a$11$4J8sFp.juYYta0hu4BJdKuubIHdGBqVACfJFgvJ4MAwkebBp8eqZS
-- -----------------------------------------------------------------------------
INSERT INTO USERS (username, password_hash, phone, email, date_of_birth, balance, role, status, created_at)
VALUES (
    'admin',
    '$2a$11$4J8sFp.juYYta0hu4BJdKuubIHdGBqVACfJFgvJ4MAwkebBp8eqZS',
    '0988888888',
    'admin@cybergame.vn',
    '1995-01-01',
    5000000.00,
    'admin',
    'active',
    CURRENT_TIMESTAMP
);
GO

-- -----------------------------------------------------------------------------
-- 4.2. BẢNG PAYMENT_METHODS: CÁC PHƯƠNG THỨC THANH TOÁN
-- -----------------------------------------------------------------------------
INSERT INTO PAYMENT_METHODS (name, code, is_active) VALUES
(N'Tiền mặt', 'CASH', 1),
(N'Chuyển khoản VietQR', 'BANK_QR', 1),
(N'Ví điện tử MoMo', 'MOMO', 1),
(N'Thẻ ATM / Thẻ quốc tế', 'CARD', 1);
GO

-- -----------------------------------------------------------------------------
-- 4.3. BẢNG ZONES: KHU VỰC PHÒNG MÁY (KHỚP HOÀN TOÀN FRONTEND)
-- -----------------------------------------------------------------------------
INSERT INTO ZONES (zone_name, specs, price_per_hour, status) VALUES
(N'Standard Zone', N'Core i5-12400F, 16GB RAM, RTX 3060 12GB, Màn hình 24" IPS 165Hz', 10000.00, 'active'),
(N'VIP Zone', N'Core i5-13400F, 32GB RAM, RTX 4060 8GB, Màn hình 27" 2K 240Hz', 15000.00, 'active'),
(N'Pro Stage', N'Core i7-13700K, 32GB RAM, RTX 4070 Ti, Màn hình BenQ ZOWIE 240Hz DyAc+', 20000.00, 'active'),
(N'Stream Room', N'Core i9-13900K, 64GB RAM, RTX 4080, Dual Screen 240Hz, Mic Rode & Elgato Cam Link', 35000.00, 'active');
GO

-- -----------------------------------------------------------------------------
-- 4.4. BẢNG COMPUTERS: TOÀN BỘ 75 MÁY TRẠM THEO SƠ ĐỒ VÀ MÃ GHẾ
-- Zone 1 (Standard): S01 -> S40 (40 máy)
-- Zone 2 (VIP): V01 -> V20 (20 máy)
-- Zone 3 (Pro Stage): P01 -> P10 (10 máy)
-- Zone 4 (Stream Room): R01 -> R05 (5 máy)
-- -----------------------------------------------------------------------------
-- 40 Máy Standard (S01 - S40)
INSERT INTO COMPUTERS (zone_id, computer_name, status) VALUES
(1, 'S01', 'available'), (1, 'S02', 'in_use'),   (1, 'S03', 'in_use'),   (1, 'S04', 'available'),
(1, 'S05', 'available'), (1, 'S06', 'in_use'),   (1, 'S07', 'available'), (1, 'S08', 'available'),
(1, 'S09', 'maintenance'), (1, 'S10', 'available'), (1, 'S11', 'in_use'),   (1, 'S12', 'in_use'),
(1, 'S13', 'available'), (1, 'S14', 'available'), (1, 'S15', 'in_use'),   (1, 'S16', 'available'),
(1, 'S17', 'available'), (1, 'S18', 'available'), (1, 'S19', 'in_use'),   (1, 'S20', 'available'),
(1, 'S21', 'available'), (1, 'S22', 'in_use'),   (1, 'S23', 'available'), (1, 'S24', 'available'),
(1, 'S25', 'available'), (1, 'S26', 'available'), (1, 'S27', 'in_use'),   (1, 'S28', 'available'),
(1, 'S29', 'available'), (1, 'S30', 'maintenance'), (1, 'S31', 'available'), (1, 'S32', 'available'),
(1, 'S33', 'in_use'),    (1, 'S34', 'available'), (1, 'S35', 'available'), (1, 'S36', 'available'),
(1, 'S37', 'in_use'),    (1, 'S38', 'available'), (1, 'S39', 'available'), (1, 'S40', 'available');

-- 20 Máy VIP (V01 - V20)
INSERT INTO COMPUTERS (zone_id, computer_name, status) VALUES
(2, 'V01', 'available'), (2, 'V02', 'in_use'),   (2, 'V03', 'in_use'),   (2, 'V04', 'available'),
(2, 'V05', 'in_use'),    (2, 'V06', 'available'), (2, 'V07', 'available'), (2, 'V08', 'available'),
(2, 'V09', 'available'), (2, 'V10', 'available'), (2, 'V11', 'in_use'),   (2, 'V12', 'available'),
(2, 'V13', 'available'), (2, 'V14', 'available'), (2, 'V15', 'available'), (2, 'V16', 'in_use'),
(2, 'V17', 'available'), (2, 'V18', 'available'), (2, 'V19', 'maintenance'), (2, 'V20', 'available');

-- 10 Máy Pro Stage (P01 - P10)
INSERT INTO COMPUTERS (zone_id, computer_name, status) VALUES
(3, 'P01', 'in_use'),   (3, 'P02', 'in_use'),   (3, 'P03', 'in_use'),   (3, 'P04', 'in_use'),
(3, 'P05', 'in_use'),   (3, 'P06', 'available'), (3, 'P07', 'available'), (3, 'P08', 'available'),
(3, 'P09', 'available'), (3, 'P10', 'available');

-- 5 Phòng Stream (R01 - R05)
INSERT INTO COMPUTERS (zone_id, computer_name, status) VALUES
(4, 'R01', 'in_use'),   (4, 'R02', 'available'), (4, 'R03', 'available'), (4, 'R04', 'available'), (4, 'R05', 'maintenance');
GO

-- -----------------------------------------------------------------------------
-- 4.5. BẢNG EQUIPMENT: THIẾT BỊ NGOẠI VI GẮN KÈM CÁC MÁY
-- -----------------------------------------------------------------------------
INSERT INTO EQUIPMENT (computer_id, equipment_name, condition_status) VALUES
-- Trang bị máy S01
(1, N'Bàn phím cơ DareU EK87 Red Switch', 'good'),
(1, N'Chuột Gaming Logitech G102 Lightsync', 'good'),
(1, N'Tai nghe DareU EH416 RGB 7.1', 'good'),
(1, N'Màn hình ViewSonic 24" 165Hz IPS', 'good'),
-- Trang bị máy S09 (bảo trì bàn phím)
(9, N'Bàn phím cơ DareU EK87', 'broken'),
(9, N'Chuột Gaming Logitech G102', 'good'),
(9, N'Tai nghe DareU EH416', 'good'),
-- Trang bị máy V05 (VIP Zone)
(45, N'Bàn phím cơ Corsair K70 RGB MK.2', 'good'),
(45, N'Chuột Razer DeathAdder V3', 'good'),
(45, N'Tai nghe HyperX Cloud II Red', 'good'),
(45, N'Màn hình ASUS TUF Gaming 27" 240Hz 2K', 'good'),
-- Trang bị máy P01 (Pro Stage)
(61, N'Bàn phím cơ Custom Leopold FC750R', 'good'),
(61, N'Chuột Logitech G Pro X Superlight Wireless', 'good'),
(61, N'Tai nghe Sennheiser Game One eSports', 'good'),
(61, N'Màn hình BenQ ZOWIE XL2546K 240Hz 0.5ms DyAc+', 'good'),
-- Trang bị phòng Stream R01
(71, N'Micro chuyên dụng Rode NT-USB Mini', 'good'),
(71, N'Webcam Razer Kiyo Pro Full HD 60FPS', 'good'),
(71, N'Bộ điều khiển Elgato Stream Deck MK.2', 'good'),
(71, N'Đèn Stream Ring Light Elgato Key Light Air', 'good'),
(71, N'Tai nghe Audio-Technica ATH-M50x', 'good');
GO

-- -----------------------------------------------------------------------------
-- 4.6. BẢNG FOODS: THỰC ĐƠN ĐẦY ĐỦ (KHỚP HOÀN TOÀN MÃ & HÌNH ẢNH FRONTEND)
-- -----------------------------------------------------------------------------
INSERT INTO FOODS (name, category, price, image_url, status) VALUES
-- Đồ ăn (food)
(N'Mì xào bò', 'food', 35000.00, 'https://images.unsplash.com/photo-1612929633738-8fe44f7ec841?auto=format&fit=crop&w=400&q=80', 'available'),
(N'Cơm rang dưa bò', 'food', 45000.00, 'https://images.unsplash.com/photo-1512058564366-18510be2db19?auto=format&fit=crop&w=400&q=80', 'available'),
(N'Bánh mì pate trứng', 'food', 25000.00, 'https://images.unsplash.com/photo-1606850239638-b78f8b8e0508?auto=format&fit=crop&w=400&q=80', 'available'),
(N'Mì tôm 2 trứng xúc xích', 'food', 30000.00, 'https://images.unsplash.com/photo-1569718212165-3a8278d5f624?auto=format&fit=crop&w=400&q=80', 'available'),
(N'Cơm gà xối mỡ', 'food', 45000.00, 'https://images.unsplash.com/photo-1598515214211-89d3c73ae83b?auto=format&fit=crop&w=400&q=80', 'available'),

-- Nước uống (drink)
(N'Sting Dâu', 'drink', 15000.00, 'https://images.unsplash.com/photo-1622483767028-3f66f32aef97?auto=format&fit=crop&w=400&q=80', 'available'),
(N'Bò húc (Redbull)', 'drink', 20000.00, 'https://images.unsplash.com/photo-1622483767028-3f66f32aef97?auto=format&fit=crop&w=400&q=80', 'available'),
(N'Coca Cola', 'drink', 15000.00, 'https://images.unsplash.com/photo-1622483767028-3f66f32aef97?auto=format&fit=crop&w=400&q=80', 'available'),
(N'Trà đào cam sả', 'drink', 30000.00, 'https://images.unsplash.com/photo-1556679343-c7306c1976bc?auto=format&fit=crop&w=400&q=80', 'available'),
(N'Cà phê sữa đá', 'drink', 22000.00, 'https://images.unsplash.com/photo-1517256064527-09c73fc73e38?auto=format&fit=crop&w=400&q=80', 'available'),
(N'Nước khoáng Aquafina', 'drink', 10000.00, 'https://images.unsplash.com/photo-1523362628745-0c100150b504?auto=format&fit=crop&w=400&q=80', 'available'),

-- Đồ ăn vặt (snack)
(N'Khoai tây chiên', 'snack', 25000.00, 'https://images.unsplash.com/photo-1576107232684-1279f3908594?auto=format&fit=crop&w=400&q=80', 'available'),
(N'Xúc xích Đức nướng', 'snack', 15000.00, 'https://images.unsplash.com/photo-1599598425947-33002620ea1f?auto=format&fit=crop&w=400&q=80', 'available'),
(N'Khô gà lá chanh', 'snack', 30000.00, 'https://images.unsplash.com/photo-1621996316521-8789db4c8ff9?auto=format&fit=crop&w=400&q=80', 'available'),
(N'Đậu phộng rang tỏi ớt', 'snack', 15000.00, 'https://images.unsplash.com/photo-1571556948574-d023f0343a41?auto=format&fit=crop&w=400&q=80', 'available'),
(N'Bỏng ngô phô mai', 'snack', 20000.00, 'https://images.unsplash.com/photo-1578849278619-e73505e9610f?auto=format&fit=crop&w=400&q=80', 'available'),

-- Tiện ích (utility)
(N'Bọc tai nghe (1 lần)', 'utility', 5000.00, 'https://images.unsplash.com/photo-1505740420928-5e560c06d30e?auto=format&fit=crop&w=400&q=80', 'available'),
(N'Áo mưa dùng 1 lần', 'utility', 10000.00, 'https://images.unsplash.com/photo-1515694346937-94d85e41e6f0?auto=format&fit=crop&w=400&q=80', 'available'),
(N'Gối chữ U kê cổ', 'utility', 20000.00, 'https://images.unsplash.com/photo-1584100936595-c0654b35a113?auto=format&fit=crop&w=400&q=80', 'available'),
(N'Chăn đắp mỏng', 'utility', 25000.00, 'https://images.unsplash.com/photo-1580252541459-7b3d328325db?auto=format&fit=crop&w=400&q=80', 'available');
GO

-- -----------------------------------------------------------------------------
-- 4.7. BẢNG KNOWLEDGE_BASE: CÂU HỎI THƯỜNG GẶP (FAQ / CHATBOT)
-- -----------------------------------------------------------------------------
INSERT INTO KNOWLEDGE_BASE (question, answer, category, status) VALUES
(N'Làm thế nào để đặt chỗ trước tại Cyber Game?', N'Bạn có thể vào mục ĐẶT MÁY trên thanh menu, chọn khu vực mong muốn (Standard, VIP, Pro Stage, Stream Room), chọn vị trí ghế và khung giờ chơi, sau đó xác nhận thông tin đặt chỗ.', N'Booking', 'active'),
(N'Bảng giá giờ chơi tại Cyber Game như thế nào?', N'Giá giờ chơi theo từng khu vực: Standard Zone: 10.000đ/h, VIP Zone: 15.000đ/h, Pro Stage: 20.000đ/h, Stream Room riêng tư: 35.000đ/h.', N'Pricing', 'active'),
(N'Tôi có thể nạp tiền vào tài khoản bằng những cách nào?', N'Bạn có thể nạp tiền trực tiếp tại quầy thu ngân bằng tiền mặt, hoặc quét mã chuyển khoản VietQR, ví điện tử MoMo trên hệ thống website.', N'Payment', 'active'),
(N'Thời gian phục vụ món ăn và đồ uống là bao lâu?', N'Sau khi bạn gửi đơn gọi món qua mục MENU trên máy hoặc website, nhân viên bếp sẽ chuẩn bị và giao tận máy cho bạn trong vòng 5 - 10 phút.', N'FnB', 'active'),
(N'Phòng game mở cửa những khung giờ nào?', N'Toàn bộ hệ thống Cyber Game mở cửa phục vụ 24/7 kể cả ngày lễ và Tết.', N'General', 'active'),
(N'Chính sách hủy hoặc đổi giờ đặt máy?', N'Bạn có thể hủy hoặc đổi máy trước giờ hẹn ít nhất 15 phút mà không mất phí. Nếu quá giờ hẹn 15 phút chưa đến nhận máy, hệ thống sẽ tự động hủy giữ chỗ.', N'Booking', 'active'),
(N'Phòng máy có khu vực hút thuốc riêng không?', N'Cyber Game có khu vực hút thuốc (Smoking Area) riêng biệt ngoài trời. Trong không gian máy lạnh hoàn toàn cấm hút thuốc để đảm bảo không khí trong lành.', N'Policy', 'active'),
(N'Phòng Stream Room trang bị những gì?', N'Phòng Stream trang bị PC cấu hình khủng Core i9-13900K, RTX 4080, 2 màn hình 240Hz, bộ đèn Key Light, Micro Rode thu âm chống ồn và Stream Deck hỗ trợ phát sóng mượt mà.', N'Specs', 'active');
GO

-- -----------------------------------------------------------------------------
-- 4.8. CÁC NGHIỆP VỤ MẪU LIÊN KẾT DUY NHẤT VỚI TÀI KHOẢN ADMIN (user_id = 1)
-- -----------------------------------------------------------------------------

-- 1 Lịch sử nạp tiền mẫu của Admin
INSERT INTO RECHARGES (user_id, amount, method_id, status, transaction_code, created_at, updated_at)
VALUES (1, 5000000.00, 2, 'completed', 'VQR_ADMIN_INIT_001', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- Biến động số dư ví tương ứng
INSERT INTO WALLET_TRANSACTIONS (user_id, type, amount, reference_id, created_at)
VALUES (1, 'recharge', 5000000.00, 1, CURRENT_TIMESTAMP);

-- 1 Đơn đặt máy mẫu của Admin tại máy VIP V05
INSERT INTO BOOKINGS (user_id, computer_id, start_time, end_time, hold_expires_at, status, created_at)
VALUES (
    1,
    45, -- Máy V05
    DATEADD(HOUR, 1, CURRENT_TIMESTAMP),
    DATEADD(HOUR, 4, CURRENT_TIMESTAMP),
    DATEADD(MINUTE, 30, CURRENT_TIMESTAMP),
    'confirmed',
    CURRENT_TIMESTAMP
);

-- 1 Phiên chơi mẫu đang diễn ra của Admin tại máy Pro Stage P01
INSERT INTO SESSIONS (booking_id, user_id, computer_id, start_time, total_minutes, total_cost)
VALUES (
    NULL,
    1,
    61, -- Máy P01
    DATEADD(MINUTE, -90, CURRENT_TIMESTAMP),
    90,
    30000.00
);

-- 1 Đơn gọi món F&B mẫu tại máy P01 của Admin
INSERT INTO FOOD_ORDERS (user_id, booking_id, session_id, seat_label, payment_method, status, total_amount, created_at)
VALUES (
    1,
    NULL,
    1,
    'P01',
    'transfer',
    'preparing',
    65000.00,
    DATEADD(MINUTE, -15, CURRENT_TIMESTAMP)
);

-- Chi tiết đơn món (1 Cơm rang dưa bò + 1 Bò húc)
INSERT INTO FOOD_ORDER_ITEMS (order_id, food_id, quantity, unit_price, subtotal) VALUES
(1, 2, 1, 45000.00, 45000.00), -- Cơm rang dưa bò
(1, 7, 1, 20000.00, 20000.00); -- Bò húc

-- 1 Phiên chat CSKH mẫu của Admin
INSERT INTO CHAT_SESSIONS (user_id, started_at, status)
VALUES (1, DATEADD(MINUTE, -40, CURRENT_TIMESTAMP), 'open');

-- Tin nhắn trong phiên chat
INSERT INTO CHAT_MESSAGES (chat_session_id, sender, content, created_at) VALUES
(1, 'user', N'Xin chào, tôi muốn hỏi về cấu hình phòng Stream Room?', DATEADD(MINUTE, -40, CURRENT_TIMESTAMP)),
(1, 'bot', N'Chào bạn! Phòng Stream Room trang bị Core i9-13900K, 64GB RAM, RTX 4080, Dual Screen 240Hz kèm Micro Rode và Elgato Stream Deck bạn nhé!', DATEADD(MINUTE, -39, CURRENT_TIMESTAMP)),
(1, 'user', N'Cảm ơn bạn, tôi muốn đặt trước phòng R01 cho tối nay.', DATEADD(MINUTE, -35, CURRENT_TIMESTAMP)),
(1, 'staff', N'Dạ nhân viên trực đã ghi nhận yêu cầu của bạn và giữ phòng R01 rồi ạ!', DATEADD(MINUTE, -30, CURRENT_TIMESTAMP));

-- 1 Phiếu hỗ trợ kỹ thuật liên kết phiên chat
INSERT INTO SUPPORT_TICKETS (chat_session_id, assigned_staff, subject, status, created_at)
VALUES (
    1,
    1, -- Phân công cho Admin
    N'Tư vấn & đặt trước phòng máy Stream Room R01',
    'in_progress',
    DATEADD(MINUTE, -30, CURRENT_TIMESTAMP)
);
GO

-- =============================================================================
-- HOÀN TẤT KHỞI TẠO CƠ SỞ DỮ LIỆU CYBERGAMEDB
-- =============================================================================
PRINT N'Đã khởi tạo thành công CSDL CyberGameDB và nạp dữ liệu hoàn chỉnh.';
PRINT N'Chỉ có 1 tài khoản quản trị: username = admin / password = Admin@123';
GO
