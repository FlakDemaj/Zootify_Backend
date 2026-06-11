-- Make specimens.species_guid nullable and change FK to ON DELETE SET NULL.
-- Add specimens.rejected_at for rejected-specimen purge job.

-- Step 1: Drop existing FK constraint on species_guid (find by table reference)
DECLARE @fkName NVARCHAR(256);
SELECT @fkName = fk.name
FROM   sys.foreign_keys fk
JOIN   sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
JOIN   sys.columns c               ON c.object_id = fkc.parent_object_id
                                   AND c.column_id = fkc.parent_column_id
WHERE  fk.parent_object_id  = OBJECT_ID(N'zootify.specimens')
AND    c.name                = 'species_guid';

IF @fkName IS NOT NULL
    EXEC('ALTER TABLE zootify.specimens DROP CONSTRAINT [' + @fkName + ']');

-- Step 2: Make the column nullable
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'zootify.specimens') AND name = 'species_guid' AND is_nullable = 0
)
    ALTER TABLE zootify.specimens ALTER COLUMN species_guid UNIQUEIDENTIFIER NULL;

-- Step 3: Recreate FK with ON DELETE SET NULL
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE parent_object_id  = OBJECT_ID(N'zootify.specimens')
      AND referenced_object_id = OBJECT_ID(N'zootify.taxonomy_nodes')
)
    ALTER TABLE zootify.specimens
        ADD CONSTRAINT FK_specimens_taxonomy_nodes_species_guid
        FOREIGN KEY (species_guid)
        REFERENCES zootify.taxonomy_nodes (guid)
        ON DELETE SET NULL;

-- Step 4: Add rejected_at column
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'zootify.specimens') AND name = 'rejected_at'
)
    ALTER TABLE zootify.specimens ADD rejected_at DATETIME2 NULL;
