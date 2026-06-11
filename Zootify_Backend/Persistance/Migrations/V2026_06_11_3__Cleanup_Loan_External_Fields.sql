-- Remove external borrower columns (only registered users can borrow)
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('[zootify].[loans]') AND name = 'borrower_external_email'
)
BEGIN
    ALTER TABLE [zootify].[loans] DROP COLUMN borrower_external_email;
END

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('[zootify].[loans]') AND name = 'borrower_external_name'
)
BEGIN
    ALTER TABLE [zootify].[loans] DROP COLUMN borrower_external_name;
END

-- Make borrower_guid required
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('[zootify].[loans]') AND name = 'borrower_guid' AND is_nullable = 1
)
BEGIN
    ALTER TABLE [zootify].[loans] ALTER COLUMN borrower_guid UNIQUEIDENTIFIER NOT NULL;
END
