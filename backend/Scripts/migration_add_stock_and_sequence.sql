-- Migration: Thêm cột stock_id vào orders + bảng sequence cho order_code
-- Chạy: mysql -u root -p < migration_add_stock_and_sequence.sql

USE business_db;

-- 1. Thêm cột stock_id vào orders (idempotent - không lỗi nếu đã tồn tại)
SET @col_exists = (SELECT COUNT(*) FROM information_schema.COLUMNS
                   WHERE TABLE_SCHEMA = 'business_db'
                     AND TABLE_NAME = 'orders'
                     AND COLUMN_NAME = 'stock_id');
SET @sql = IF(@col_exists = 0,
              'ALTER TABLE orders ADD COLUMN stock_id VARCHAR(36) AFTER customer_id',
              'SELECT 1 AS skip_alter');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- 2. Bảng sequence cho order_code (atomic increment với row lock)
CREATE TABLE IF NOT EXISTS order_sequence (
    sequence_name VARCHAR(50) PRIMARY KEY,
    current_value BIGINT NOT NULL DEFAULT 0,
    updated_date DATETIME
);

INSERT INTO order_sequence (sequence_name, current_value, updated_date)
VALUES ('order_code', 0, NOW())
ON DUPLICATE KEY UPDATE sequence_name = sequence_name;

-- 3. Verify
SELECT 'orders schema:' AS info;
DESCRIBE orders;

SELECT 'order_sequence:' AS info;
SELECT * FROM order_sequence;