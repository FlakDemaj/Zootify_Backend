-- Move the specimen <-> image relationship so the specimen owns the foreign key.
-- The image content is fetched separately by image_guid, so the (large) image row no
-- longer has to be joined when loading specimens.

-- 1. Add the nullable image_guid reference column to specimens.
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'zootify.specimens') AND name = 'image_guid'
)
    ALTER TABLE [zootify].[specimens] ADD [image_guid] UNIQUEIDENTIFIER NULL;
GO

-- 2. Backfill from the existing image -> specimen link (one image per specimen).
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'zootify.specimen_images') AND name = 'specimen_guid'
)
    UPDATE s
    SET s.[image_guid] = i.[guid]
    FROM [zootify].[specimens] s
    INNER JOIN [zootify].[specimen_images] i ON i.[specimen_guid] = s.[guid];
GO

-- 3. Drop the old foreign key and column on specimen_images.
IF EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = 'fk_specimen_images_specimen'
)
    ALTER TABLE [zootify].[specimen_images] DROP CONSTRAINT [fk_specimen_images_specimen];
GO

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'zootify.specimen_images') AND name = 'specimen_guid'
)
    ALTER TABLE [zootify].[specimen_images] DROP COLUMN [specimen_guid];
GO

-- 4. Enforce the one-to-one relationship with a unique index on the new reference column.
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'ux_specimens_image_guid' AND object_id = OBJECT_ID(N'zootify.specimens')
)
    CREATE UNIQUE INDEX [ux_specimens_image_guid]
        ON [zootify].[specimens] ([image_guid])
        WHERE [image_guid] IS NOT NULL;
GO

-- 5. Add the new foreign key on specimens.image_guid; clearing it on image deletion.
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = 'fk_specimens_image'
)
    ALTER TABLE [zootify].[specimens]
        ADD CONSTRAINT [fk_specimens_image] FOREIGN KEY ([image_guid])
            REFERENCES [zootify].[specimen_images] ([guid]) ON DELETE SET NULL;
GO
