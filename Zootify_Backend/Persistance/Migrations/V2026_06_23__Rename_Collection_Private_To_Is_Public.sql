-- Rename collections.private to collections.is_public and invert its meaning so the
-- column matches the specimens.is_public convention (true = visible to everyone).
--
-- Statements that reference the new column name are wrapped in EXEC('...') so SQL Server
-- defers their name resolution to runtime: is_public does not yet exist when the batch is
-- compiled, only after sp_rename has run.

-- Step 1: Rename the column and flip the stored values (private => is_public).
IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID(N'zootify.collections') AND name = 'private')
   AND NOT EXISTS (SELECT 1 FROM sys.columns
                   WHERE object_id = OBJECT_ID(N'zootify.collections') AND name = 'is_public')
BEGIN
    EXEC sp_rename 'zootify.collections.private', 'is_public', 'COLUMN';

    -- A previously private collection (1) becomes not public (0), and vice versa.
    EXEC('UPDATE [zootify].[collections] SET is_public = CASE WHEN is_public = 1 THEN 0 ELSE 1 END;');
END

-- Step 2: Default new rows to public, matching specimens.is_public.
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints
               WHERE parent_object_id = OBJECT_ID(N'zootify.collections')
                 AND name = 'df_collections_is_public')
    EXEC('ALTER TABLE [zootify].[collections] ADD CONSTRAINT df_collections_is_public DEFAULT 1 FOR is_public;');
