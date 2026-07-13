-- Add a nullable rejected_date to taxonomy nodes. It records when a node was moved to the
-- "rejected" approval status, so the purge job can hard-delete it after the retention period.

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'zootify.taxonomy_nodes') AND name = 'rejected_date'
)
    ALTER TABLE [zootify].[taxonomy_nodes] ADD [rejected_date] DATETIME2 NULL;
