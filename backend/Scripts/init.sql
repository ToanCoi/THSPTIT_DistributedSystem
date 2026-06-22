-- =====================================================
-- Database Initialization Script for Ecom Microservices
-- =====================================================

-- Create master_db for authentication
CREATE DATABASE IF NOT EXISTS master_db;
USE master_db;

-- Users table for authentication
CREATE TABLE IF NOT EXISTS users (
    user_id VARCHAR(36) PRIMARY KEY,
    username VARCHAR(100) UNIQUE NOT NULL,
    password_hash VARCHAR(255),
    email VARCHAR(255),
    google_id VARCHAR(255),
    full_name VARCHAR(255),
    phone_number VARCHAR(20),
    avatar_url VARCHAR(500),
    is_active TINYINT(1) DEFAULT 1,
    is_verified TINYINT(1) DEFAULT 0,
    refresh_token VARCHAR(500),
    refresh_token_expire DATETIME,
    role_code VARCHAR(50) DEFAULT 'USER',
    created_date DATETIME,
    created_by VARCHAR(100),
    modified_date DATETIME,
    modified_by VARCHAR(100)
);

-- Create business_db for business operations
CREATE DATABASE IF NOT EXISTS business_db;
USE business_db;

-- Customers table
CREATE TABLE IF NOT EXISTS customers (
    customer_id VARCHAR(36) PRIMARY KEY,
    user_id VARCHAR(36),
    full_name VARCHAR(255),
    phone VARCHAR(20),
    email VARCHAR(255),
    address TEXT,
    created_date DATETIME,
    created_by VARCHAR(100),
    modified_date DATETIME,
    modified_by VARCHAR(100)
);

-- Products table
CREATE TABLE IF NOT EXISTS products (
    product_id VARCHAR(36) PRIMARY KEY,
    product_code VARCHAR(50) UNIQUE,
    product_name VARCHAR(255),
    price DECIMAL(18,2),
    unit VARCHAR(50),
    created_date DATETIME,
    created_by VARCHAR(100),
    modified_date DATETIME,
    modified_by VARCHAR(100)
);

-- Stocks table
CREATE TABLE IF NOT EXISTS stocks (
    stock_id VARCHAR(36) PRIMARY KEY,
    stock_code VARCHAR(50) UNIQUE,
    stock_name VARCHAR(255),
    address TEXT,
    created_date DATETIME,
    created_by VARCHAR(100),
    modified_date DATETIME,
    modified_by VARCHAR(100)
);

-- Inwards table (Nhập kho)
CREATE TABLE IF NOT EXISTS inwards (
    inward_id VARCHAR(36) PRIMARY KEY,
    product_id VARCHAR(36),
    stock_id VARCHAR(36),
    quantity DECIMAL(18,4),
    unit_price DECIMAL(18,2),
    selling_price DECIMAL(18,2) DEFAULT 0,
    supplier VARCHAR(255),
    invoice_date DATE,
    created_date DATETIME,
    created_by VARCHAR(100)
);

-- Outwards table (Xuất kho)
CREATE TABLE IF NOT EXISTS outwards (
    outward_id VARCHAR(36) PRIMARY KEY,
    order_id VARCHAR(36),
    product_id VARCHAR(36),
    stock_id VARCHAR(36),
    quantity DECIMAL(18,4),
    unit_price DECIMAL(18,2),
    outward_date DATE,
    created_date DATETIME,
    created_by VARCHAR(100)
);

-- Orders table
CREATE TABLE IF NOT EXISTS orders (
    order_id VARCHAR(36) PRIMARY KEY,
    customer_id VARCHAR(36),
    order_code VARCHAR(50),
    total_amount DECIMAL(18,2),
    status VARCHAR(50),
    order_date DATE,
    created_date DATETIME,
    created_by VARCHAR(100)
);

-- Order Items table
CREATE TABLE IF NOT EXISTS order_items (
    order_item_id VARCHAR(36) PRIMARY KEY,
    order_id VARCHAR(36),
    product_id VARCHAR(36),
    quantity DECIMAL(18,4),
    unit_price DECIMAL(18,2),
    created_date DATETIME,
    created_by VARCHAR(100)
);

-- Ledger Tables for inventory management
-- Main ledger table
CREATE TABLE IF NOT EXISTS led_inventory_item_ledger (
    ledger_id VARCHAR(36) PRIMARY KEY,
    product_id VARCHAR(36),
    stock_id VARCHAR(36),
    inward_quantity DECIMAL(18,4) DEFAULT 0,
    outward_quantity DECIMAL(18,4) DEFAULT 0,
    reference_id VARCHAR(36),
    reference_type VARCHAR(50),
    ledger_date DATETIME,
    created_date DATETIME,
    created_by VARCHAR(100)
);

-- Daily ledger summary
CREATE TABLE IF NOT EXISTS led_inventory_item_ledger_date (
    ledger_date_id VARCHAR(36) PRIMARY KEY,
    product_id VARCHAR(36),
    stock_id VARCHAR(36),
    inward_quantity DECIMAL(18,4) DEFAULT 0,
    outward_quantity DECIMAL(18,4) DEFAULT 0,
    ledger_date DATE,
    created_date DATETIME,
    UNIQUE KEY unique_product_stock_date (product_id, stock_id, ledger_date)
);

-- Closing balance table
CREATE TABLE IF NOT EXISTS led_inventory_item_ledger_closing (
    closing_id VARCHAR(36) PRIMARY KEY,
    product_id VARCHAR(36),
    stock_id VARCHAR(36),
    quantity DECIMAL(18,4) DEFAULT 0,
    updated_date DATETIME,
    UNIQUE KEY unique_product_stock (product_id, stock_id)
);

-- =====================================================
-- Insert sample data for testing
-- =====================================================

-- Insert sample user
INSERT INTO master_db.users (user_id, username, password_hash, email, full_name, role_code, created_date, is_active)
VALUES (
    'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
    'admin',
    '$2a$11$rqFX1rkYiVFLTkKy1gL4KeN9LmqKLRH5.9VD1aLwOKXqH5rP4L.Oy',
    'admin@ecom.com',
    'Administrator',
    'ADMIN',
    NOW(),
    1
);

-- Insert sample customer
INSERT INTO business_db.customers (customer_id, full_name, phone, email, address, created_date)
VALUES (
    'b2c3d4e5-f6a7-8901-bcde-f23456789012',
    'Nguyễn Văn A',
    '0901234567',
    'nguyenvana@email.com',
    '123 Đường ABC, Quận 1, TP.HCM',
    NOW()
);

-- Insert sample stock
INSERT INTO business_db.stocks (stock_id, stock_code, stock_name, address, created_date)
VALUES (
    'c3d4e5f6-a7b8-9012-cdef-345678901234',
    'KHO001',
    'Kho Hàng Chính',
    '456 Đường XYZ, Quận 2, TP.HCM',
    NOW()
);

-- Insert sample product
INSERT INTO business_db.products (product_id, product_code, product_name, price, unit, created_date)
VALUES (
    'd4e5f6a7-b8c9-0123-def0-456789012345',
    'SP001',
    'Sản phẩm Mẫu',
    100000.00,
    'Chiếc',
    NOW()
);