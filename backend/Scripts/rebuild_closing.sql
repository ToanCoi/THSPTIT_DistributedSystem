-- =====================================================
-- Rebuild led_inventory_item_ledger_closing từ inwards + outwards
--
-- Lý do: Bug cũ của UpsertClosingAsync ghi đè tuyệt đối (quantity = @quantity)
-- thay vì cộng dồn (quantity = quantity + @quantity) khiến closing cũ không
-- phản ánh đúng tồn kho thực tế. Script này rebuild từ nguồn gốc là 2 bảng
-- giao dịch inwards + outwards.
--
-- Chạy: mysql -h <host> -u <user> -p business_db < rebuild_closing.sql
-- Hoặc:  docker exec -i ecom-mysql-business mysql -uroot -pP@ssw0rd123 business_db < rebuild_closing.sql
-- =====================================================

USE business_db;

-- Backup bảng cũ đề phòng
DROP TABLE IF EXISTS led_inventory_item_ledger_closing_backup;
CREATE TABLE led_inventory_item_ledger_closing_backup AS
SELECT * FROM led_inventory_item_ledger_closing;

-- Xoá bảng hiện tại
TRUNCATE TABLE led_inventory_item_ledger_closing;

-- Rebuild từ inwards (cộng) và outwards (trừ)
INSERT INTO led_inventory_item_ledger_closing
    (closing_id, product_id, stock_id, quantity, updated_date)
SELECT
    UUID() AS closing_id,
    product_id,
    stock_id,
    COALESCE(in_qty, 0) - COALESCE(out_qty, 0) AS quantity,
    NOW() AS updated_date
FROM (
    SELECT
        i.product_id,
        i.stock_id,
        SUM(i.quantity) AS in_qty,
        (SELECT COALESCE(SUM(o.quantity), 0)
         FROM outwards o
         WHERE o.product_id = i.product_id
           AND o.stock_id = i.stock_id) AS out_qty
    FROM inwards i
    GROUP BY i.product_id, i.stock_id
) AS combined
WHERE COALESCE(in_qty, 0) - COALESCE(out_qty, 0) <> 0;

-- Verify
SELECT
    COUNT(*) AS total_rows,
    SUM(quantity) AS total_quantity
FROM led_inventory_item_ledger_closing;

SELECT
    COUNT(*) AS total_inwards,
    SUM(quantity) AS total_in_qty
FROM inwards;

SELECT
    COUNT(*) AS total_outwards,
    SUM(quantity) AS total_out_qty
FROM outwards;
