-- Remove updated_by / deleted_by audit columns from taxonomy_nodes.
-- TaxonomyNodeEntity no longer inherits BaseUpdateEntity / BaseDeletionEntity.

-- Drop FK for updated_by_user_guid
DECLARE @fkUpdated NVARCHAR(256);
SELECT @fkUpdated = fk.name
FROM   sys.foreign_keys fk
JOIN   sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
JOIN   sys.columns c               ON c.object_id = fkc.parent_object_id
                                   AND c.column_id = fkc.parent_column_id
WHERE  fk.parent_object_id = OBJECT_ID(N'zootify.taxonomy_nodes')
AND    c.name               = 'updated_by_user_guid';

IF @fkUpdated IS NOT NULL
    EXEC('ALTER TABLE zootify.taxonomy_nodes DROP CONSTRAINT [' + @fkUpdated + ']');

-- Drop FK for deleted_by_user_guid
DECLARE @fkDeleted NVARCHAR(256);
SELECT @fkDeleted = fk.name
FROM   sys.foreign_keys fk
JOIN   sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
JOIN   sys.columns c               ON c.object_id = fkc.parent_object_id
                                   AND c.column_id = fkc.parent_column_id
WHERE  fk.parent_object_id = OBJECT_ID(N'zootify.taxonomy_nodes')
AND    c.name               = 'deleted_by_user_guid';

IF @fkDeleted IS NOT NULL
    EXEC('ALTER TABLE zootify.taxonomy_nodes DROP CONSTRAINT [' + @fkDeleted + ']');

-- Drop the columns
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'zootify.taxonomy_nodes') AND name = 'updated_by_user_guid')
    ALTER TABLE zootify.taxonomy_nodes DROP COLUMN updated_by_user_guid;

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'zootify.taxonomy_nodes') AND name = 'updated_date')
    ALTER TABLE zootify.taxonomy_nodes DROP COLUMN updated_date;

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'zootify.taxonomy_nodes') AND name = 'deleted_by_user_guid')
    ALTER TABLE zootify.taxonomy_nodes DROP COLUMN deleted_by_user_guid;

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'zootify.taxonomy_nodes') AND name = 'deletion_date')
    ALTER TABLE zootify.taxonomy_nodes DROP COLUMN deletion_date;
