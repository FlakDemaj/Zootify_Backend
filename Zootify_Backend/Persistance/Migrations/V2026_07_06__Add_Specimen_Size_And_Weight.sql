-- Add nullable size and weight columns to specimens.

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'zootify.specimens') AND name = 'size'
)
    ALTER TABLE zootify.specimens ADD size FLOAT NULL;

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'zootify.specimens') AND name = 'weight'
)
    ALTER TABLE zootify.specimens ADD weight FLOAT NULL;
