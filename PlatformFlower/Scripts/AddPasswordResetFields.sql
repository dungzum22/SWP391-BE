-- Add password reset fields to Users table
-- This script adds the necessary columns for password reset functionality

-- Check if columns don't exist before adding them
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'reset_password_token')
BEGIN
    ALTER TABLE Users 
    ADD reset_password_token NVARCHAR(255) NULL;
    PRINT 'Added reset_password_token column to Users table';
END
ELSE
BEGIN
    PRINT 'reset_password_token column already exists in Users table';
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'reset_password_token_expiry')
BEGIN
    ALTER TABLE Users 
    ADD reset_password_token_expiry DATETIME NULL;
    PRINT 'Added reset_password_token_expiry column to Users table';
END
ELSE
BEGIN
    PRINT 'reset_password_token_expiry column already exists in Users table';
END

-- Verify the changes
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Users' 
AND COLUMN_NAME IN ('reset_password_token', 'reset_password_token_expiry')
ORDER BY COLUMN_NAME;

PRINT 'Password reset fields have been successfully added to the Users table';
