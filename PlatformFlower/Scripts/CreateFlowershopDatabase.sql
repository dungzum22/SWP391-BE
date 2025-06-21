-- Kiểm tra và xóa database Flowershop nếu tồn tại
IF DB_ID('Flowershop') IS NOT NULL
    DROP DATABASE Flowershop;
GO

-- Tạo mới database Flowershop
CREATE DATABASE Flowershop;
GO

-- Sử dụng database Flowershop
USE Flowershop;
GO

-- Bảng Users với username là unique và các trường reset password
CREATE TABLE Users (
    user_id INT IDENTITY(1,1) PRIMARY KEY,
    username NVARCHAR(255) NOT NULL UNIQUE,  -- Username phải là duy nhất
    password NVARCHAR(255) NOT NULL,
    email NVARCHAR(255) NOT NULL,
    type NVARCHAR(20) NOT NULL,
    created_date DATETIME DEFAULT GETDATE(),
    status NVARCHAR(20) DEFAULT 'active',
    reset_password_token NVARCHAR(255) NULL,  -- Token để reset password
    reset_password_token_expiry DATETIME NULL,  -- Thời gian hết hạn của token
    CONSTRAINT chk_role CHECK (type IN ('admin', 'seller', 'user')),
    CONSTRAINT chk_status CHECK (status IN ('active', 'inactive'))
);
GO

CREATE TABLE Seller (
    seller_id INT IDENTITY(1,1) PRIMARY KEY,
    user_id INT NOT NULL,
    shop_name NVARCHAR(255) NOT NULL,
    address_seller NVARCHAR(255) NOT NULL,
    created_at DATETIME DEFAULT GETDATE(),
    updated_at DATETIME DEFAULT GETDATE(),
    total_product INT DEFAULT 0,
    role NVARCHAR(20) NOT NULL CHECK (role IN ('individual', 'enterprise')),
    introduction TEXT,
    FOREIGN KEY (user_id) REFERENCES Users(user_id)
);

-- Bảng User_Info
CREATE TABLE User_Info (
    user_info_id INT IDENTITY(1,1) PRIMARY KEY,
    user_id INT,
    address NVARCHAR(255),
    full_name NVARCHAR(255),
    birth_date DATE,
    sex NVARCHAR(10),
    is_seller BIT DEFAULT 0,  -- Boolean thay bằng BIT
    avatar NVARCHAR(255),
    created_date DATETIME DEFAULT GETDATE(),
    updated_date DATETIME DEFAULT GETDATE(),
    Points INT CONSTRAINT DF_User_Info_Points DEFAULT 100,
    FOREIGN KEY (user_id) REFERENCES Users(user_id),
    CONSTRAINT chk_sex CHECK (sex IN ('male', 'female', 'other'))
);
GO

-- Bảng Category cho các loại hoa
CREATE TABLE Category (
    category_id INT IDENTITY(1,1) PRIMARY KEY,
    category_name NVARCHAR(255) NOT NULL,
    status NVARCHAR(20) DEFAULT 'active',
    created_at DATETIME DEFAULT GETDATE(),
    updated_at DATETIME DEFAULT GETDATE(),
    CONSTRAINT chk_category_status CHECK (status IN ('active', 'inactive'))
);
GO

-- Bảng Flower_Info với liên kết đến bảng Category và Seller
CREATE TABLE Flower_Info (
    flower_id INT IDENTITY(1,1) PRIMARY KEY,
    flower_name NVARCHAR(255) NOT NULL,
    flower_description NVARCHAR(255),
    price DECIMAL(10, 2) NOT NULL,
    image_url NVARCHAR(255),
    available_quantity INT NOT NULL,
    status NVARCHAR(20) NOT NULL DEFAULT 'active',
    created_at DATETIME DEFAULT GETDATE(),
    updated_at DATETIME DEFAULT GETDATE(),
    category_id INT,
    seller_id INT,
    is_deleted BIT NOT NULL DEFAULT 0,
    FOREIGN KEY (category_id) REFERENCES Category(category_id),
    FOREIGN KEY (seller_id) REFERENCES Seller(seller_id),
    CONSTRAINT chk_flower_status CHECK (status IN ('active', 'inactive'))
);
GO

-- Bảng Cart chỉ cho phép người dùng 'user'
CREATE TABLE Cart (
    cart_id INT IDENTITY(1,1) PRIMARY KEY,
    user_id INT,
    flower_id INT,
    quantity INT NOT NULL,
    unit_price DECIMAL(10,2) NOT NULL,
    FOREIGN KEY (user_id) REFERENCES Users(user_id),
    FOREIGN KEY (flower_id) REFERENCES Flower_Info(flower_id)
);
GO

-- Bảng Address
CREATE TABLE Address (
address_id INT IDENTITY(1,1) PRIMARY KEY,
    user_info_id INT,
    description NVARCHAR(255),
    FOREIGN KEY (user_info_id) REFERENCES User_Info(user_info_id)
);
GO

-- Bảng User_Voucher_Status
CREATE TABLE User_Voucher_Status (
    user_voucher_status_id INT IDENTITY(1,1) PRIMARY KEY,
    user_info_id INT,                       -- Liên kết tới người dùng
    voucher_code NVARCHAR(50) NOT NULL,     -- Mã voucher
    discount FLOAT NOT NULL,                -- Phần trăm/giá trị giảm giá của voucher
    description NVARCHAR(255),              -- Mô tả voucher
    start_date DATETIME NOT NULL,           -- Ngày bắt đầu của voucher
    end_date DATETIME NOT NULL,             -- Ngày kết thúc của voucher
    usage_limit INT,                        -- Giới hạn số lần sử dụng của voucher
    usage_count INT DEFAULT 0,              -- Số lần voucher đã được sử dụng bởi người dùng này
    remaining_count INT,                    -- Số lượng voucher còn lại cho người dùng
    created_at DATETIME DEFAULT GETDATE(),  -- Thời gian tạo voucher
    shop_id INT,
    status NVARCHAR(20) DEFAULT 'active',   -- Trạng thái voucher (active, inactive, expired)
    is_deleted BIT DEFAULT 0,               -- Soft delete flag (0=active, 1=deleted)
    FOREIGN KEY (user_info_id) REFERENCES User_Info(user_info_id),
    FOREIGN KEY (shop_id) REFERENCES Seller(seller_id),
    CONSTRAINT chk_voucher_status CHECK (status IN ('active', 'inactive', 'expired'))
);
GO

-- Bảng Orders
CREATE TABLE Orders (
    order_id INT IDENTITY(1,1) PRIMARY KEY,
    user_id INT,
    phone_number NVARCHAR(20),
    payment_method NVARCHAR(50) NOT NULL,
    delivery_method NVARCHAR(255) NOT NULL,
    created_date DATETIME DEFAULT GETDATE(),
    user_voucher_status_id INT,
    address_id INT,
    cart_id INT,
    status_payment NVARCHAR(20),
    total_price DECIMAL(10, 2),  -- Tổng giá trị của đơn hàng
    FOREIGN KEY (user_id) REFERENCES Users(user_id),
    FOREIGN KEY (user_voucher_status_id) REFERENCES User_Voucher_Status(user_voucher_status_id),
    FOREIGN KEY (address_id) REFERENCES Address(address_id),
    FOREIGN KEY (cart_id) REFERENCES Cart(cart_id)
);
GO

-- Bảng Orders_Detail
CREATE TABLE Orders_Detail (
    order_detail_id INT IDENTITY(1,1) PRIMARY KEY,
    order_id INT,
    seller_id INT,
    flower_id INT,
    price DECIMAL(10, 2) NOT NULL,
    amount INT NOT NULL,
    user_voucher_status_id INT,
    status NVARCHAR(20) DEFAULT 'pending',
    created_at DATETIME DEFAULT GETDATE(),
    address_id INT,
    delivery_method NVARCHAR(255) NOT NULL,
    FOREIGN KEY (order_id) REFERENCES Orders(order_id) ON DELETE CASCADE,
    FOREIGN KEY (flower_id) REFERENCES Flower_Info(flower_id),
    FOREIGN KEY (seller_id) REFERENCES Seller(seller_id),
    FOREIGN KEY (address_id) REFERENCES Address(address_id),
    FOREIGN KEY (user_voucher_status_id) REFERENCES User_Voucher_Status(user_voucher_status_id),
    CONSTRAINT chk_order_detail_status CHECK (status IN ('pending', 'delivered', 'canceled','accepted','pending delivery'))
);
GO

-- Bảng Product_Report để lưu thông tin báo cáo sản phẩm
CREATE TABLE Report (
report_id INT IDENTITY(1,1) PRIMARY KEY,
    user_id INT NOT NULL,  -- Người dùng báo cáo
    flower_id INT NOT NULL,  -- Sản phẩm bị báo cáo
    seller_id INT NOT NULL,  -- Người bán sản phẩm bị báo cáo
    report_reason NVARCHAR(255) NOT NULL,  -- Lý do báo cáo
    report_description NVARCHAR(255),  -- Mô tả chi tiết về báo cáo
    status NVARCHAR(20) DEFAULT 'pending',  -- Trạng thái xử lý báo cáo
    created_at DATETIME DEFAULT GETDATE(),
    updated_at DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (user_id) REFERENCES Users(user_id),
    FOREIGN KEY (flower_id) REFERENCES Flower_Info(flower_id),
    FOREIGN KEY (seller_id) REFERENCES Seller(seller_id),
    CONSTRAINT chk_report_status CHECK (status IN ('pending', 'resolved', 'dismissed'))  -- Ràng buộc trạng thái
);
GO

-- Indexes for Flower_Info status and soft delete optimization
-- Index for public user queries (only active, non-deleted flowers)
CREATE INDEX IX_FlowerInfo_Public_Display
ON Flower_Info (is_deleted, status, category_id)
INCLUDE (flower_name, price, available_quantity, image_url)
WHERE is_deleted = 0;
GO

-- Index for seller dashboard (all flowers by seller)
CREATE INDEX IX_FlowerInfo_Seller_Dashboard
ON Flower_Info (seller_id, is_deleted, status)
INCLUDE (flower_name, price, available_quantity, created_at, updated_at);
GO

-- Index for category filtering (public)
CREATE INDEX IX_FlowerInfo_Category_Public
ON Flower_Info (category_id, is_deleted, status)
INCLUDE (flower_name, price, available_quantity)
WHERE is_deleted = 0 AND status = 'active';
GO

-- Index for search and filtering
CREATE INDEX IX_FlowerInfo_Search_Filter
ON Flower_Info (is_deleted, status)
INCLUDE (flower_name, flower_description, price, category_id, seller_id)
WHERE is_deleted = 0;
GO

-- Indexes for User_Voucher_Status soft delete optimization

-- Index for seller dashboard (shows all vouchers including deleted for history)
CREATE INDEX IX_UserVoucherStatus_Seller_Dashboard
ON User_Voucher_Status (shop_id, is_deleted, status)
INCLUDE (voucher_code, discount, start_date, end_date, created_at, description, usage_limit, remaining_count);
GO

-- Index for user queries (only active, non-deleted vouchers)
CREATE INDEX IX_UserVoucherStatus_User_Available
ON User_Voucher_Status (user_info_id, is_deleted, status, start_date, end_date)
INCLUDE (voucher_code, discount, remaining_count, usage_limit, description)
WHERE is_deleted = 0 AND status = 'active';
GO

-- Index for voucher code lookup (for applying vouchers)
CREATE INDEX IX_UserVoucherStatus_Code_Lookup
ON User_Voucher_Status (voucher_code, is_deleted, status)
INCLUDE (user_info_id, shop_id, discount, start_date, end_date, remaining_count, usage_limit)
WHERE is_deleted = 0;
GO

-- Index for shop-specific voucher management
CREATE INDEX IX_UserVoucherStatus_Shop_Management
ON User_Voucher_Status (shop_id, status, is_deleted, created_at)
INCLUDE (voucher_code, discount, start_date, end_date, description);
GO

-- Create view for user-visible vouchers (helper for application queries)
CREATE VIEW vw_UserAvailableVouchers AS
SELECT
    user_voucher_status_id,
    user_info_id,
    voucher_code,
    discount,
    description,
    start_date,
    end_date,
    usage_limit,
    usage_count,
    remaining_count,
    created_at,
    shop_id
FROM User_Voucher_Status
WHERE is_deleted = 0
  AND status = 'active'
  AND start_date <= GETDATE()
  AND end_date >= GETDATE()
  AND (remaining_count IS NULL OR remaining_count > 0);
GO

-- Insert default admin account (password: Admin123)
DECLARE @AdminUserId INT;

INSERT INTO Users (username, password, email, type, status)
VALUES ('admin', '$2a$12$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', 'flowershopplatform@gmail.com', 'admin', 'active');

SET @AdminUserId = SCOPE_IDENTITY();

-- Insert admin user info
INSERT INTO User_Info (user_id, full_name, address, sex, is_seller, points)
VALUES (@AdminUserId, 'System Administrator', 'Platform Flower HQ', 'other', 0, 1000);
GO

-- Insert default categories
INSERT INTO Category (category_name, status) VALUES
('Hoa hồng', 'active'),
('Hoa tulip', 'active'),
('Hoa cúc', 'active'),
('Hoa lan', 'active'),
('Hoa hướng dương', 'active');
GO

PRINT 'Database Flowershop created successfully with default admin account and categories!';
