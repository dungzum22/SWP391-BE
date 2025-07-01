-- Optional: Add status column to Orders table for better order management
-- This script adds a separate status field to track order processing status
-- distinct from payment status

-- Check if the column already exists
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'status')
BEGIN
    -- Add status column to Orders table
    ALTER TABLE Orders 
    ADD status NVARCHAR(20) DEFAULT 'pending';
    
    -- Add check constraint for valid status values
    ALTER TABLE Orders 
    ADD CONSTRAINT chk_order_status 
    CHECK (status IN ('pending', 'accepted', 'pending delivery', 'delivered', 'canceled'));
    
    PRINT 'Status column added to Orders table successfully';
END
ELSE
BEGIN
    PRINT 'Status column already exists in Orders table';
END

-- Update existing orders to have status based on their order details
-- This synchronizes the new Orders.status with existing Orders_Detail.status
UPDATE o
SET status = (
    SELECT TOP 1 od.status 
    FROM Orders_Detail od 
    WHERE od.order_id = o.order_id
    ORDER BY od.created_at DESC
)
FROM Orders o
WHERE o.status = 'pending' -- Only update orders that still have default status
AND EXISTS (
    SELECT 1 FROM Orders_Detail od 
    WHERE od.order_id = o.order_id
);

PRINT 'Existing orders status synchronized with order details';

-- Optional: Create an index on the new status column for better query performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Orders_Status')
BEGIN
    CREATE INDEX IX_Orders_Status ON Orders(status);
    PRINT 'Index created on Orders.status column';
END

-- Show summary of orders by status
SELECT 
    status,
    COUNT(*) as order_count,
    SUM(total_price) as total_revenue
FROM Orders 
GROUP BY status
ORDER BY 
    CASE status
        WHEN 'pending' THEN 1
        WHEN 'accepted' THEN 2
        WHEN 'pending delivery' THEN 3
        WHEN 'delivered' THEN 4
        WHEN 'canceled' THEN 5
        ELSE 6
    END;
