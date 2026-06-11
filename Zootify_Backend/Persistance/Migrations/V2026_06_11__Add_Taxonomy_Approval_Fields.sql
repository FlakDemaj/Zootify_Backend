IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID('[zootify].[taxonomy_nodes]')
      AND name = 'approved_by_user_guid'
)
BEGIN
    ALTER TABLE [zootify].[taxonomy_nodes]
        ADD approved_by_user_guid UNIQUEIDENTIFIER NULL,
            approved_date         DATETIME2        NULL;

    ALTER TABLE [zootify].[taxonomy_nodes]
        ADD CONSTRAINT fk_taxonomy_nodes_approved_by
            FOREIGN KEY (approved_by_user_guid)
            REFERENCES [zootify].[users] (guid)
            ON DELETE NO ACTION;
END
