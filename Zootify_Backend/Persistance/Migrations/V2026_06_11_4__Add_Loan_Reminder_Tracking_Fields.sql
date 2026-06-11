IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'zootify.loans') AND name = 'stage1_reminder_sent_at'
)
    ALTER TABLE zootify.loans ADD stage1_reminder_sent_at DATETIME2 NULL;

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'zootify.loans') AND name = 'stage2_reminder_sent_at'
)
    ALTER TABLE zootify.loans ADD stage2_reminder_sent_at DATETIME2 NULL;

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'zootify.loans') AND name = 'stage3_reminder_sent_at'
)
    ALTER TABLE zootify.loans ADD stage3_reminder_sent_at DATETIME2 NULL;
