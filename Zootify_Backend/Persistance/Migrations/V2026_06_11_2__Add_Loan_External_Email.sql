IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID('[zootify].[loans]')
      AND name = 'borrower_external_email'
)
BEGIN
    ALTER TABLE [zootify].[loans]
        ADD borrower_external_email NVARCHAR(256) NULL;
END
