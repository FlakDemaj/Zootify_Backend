-- ============================================================
--  Zootify Database  –  Init Script (consolidated)
--  Microsoft SQL Server  |  Schema: zootify
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'zootify')
    EXEC('CREATE SCHEMA [zootify]')
GO

-- ────────────────────────────────────────────────────────────
--  USERS
-- ────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = 'zootify' AND t.name = 'users'
)
BEGIN
    CREATE TABLE [zootify].[users] (
        guid                    UNIQUEIDENTIFIER    NOT NULL,

        first_name              NVARCHAR(100)       NOT NULL,
        last_name               NVARCHAR(100)       NOT NULL,
        email                   NVARCHAR(256)       NOT NULL,
        password_hash           NVARCHAR(512)       NOT NULL,
        login_attempts          INT                 NOT NULL,
        role                    INT                 NOT NULL DEFAULT 0,
        status                  INT                 NOT NULL DEFAULT 0,

        created_at              DATETIME2           NOT NULL DEFAULT GETUTCDATE(),

        updated_date            DATETIME2           NULL,
        updated_by_user_guid    UNIQUEIDENTIFIER    NULL,

        deletion_date           DATETIME2           NULL,
        deleted_by_user_guid    UNIQUEIDENTIFIER    NULL,

        CONSTRAINT pk_users       PRIMARY KEY (guid),
        CONSTRAINT uq_users_email UNIQUE      (email)
    )
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'fk_users_updated_by')
BEGIN
    ALTER TABLE [zootify].[users]
        ADD CONSTRAINT fk_users_updated_by
        FOREIGN KEY (updated_by_user_guid)
        REFERENCES [zootify].[users] (guid) ON DELETE NO ACTION
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'fk_users_deleted_by')
BEGIN
    ALTER TABLE [zootify].[users]
        ADD CONSTRAINT fk_users_deleted_by
        FOREIGN KEY (deleted_by_user_guid)
        REFERENCES [zootify].[users] (guid) ON DELETE NO ACTION
END
GO

-- ────────────────────────────────────────────────────────────
--  REGISTRATION_TOKENS
-- ────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = 'zootify' AND t.name = 'registration_tokens'
)
BEGIN
    CREATE TABLE [zootify].[registration_tokens] (
        guid        UNIQUEIDENTIFIER    NOT NULL,
        token       NVARCHAR(128)       NOT NULL,
        user_guid   UNIQUEIDENTIFIER    NOT NULL,
        expires_at  DATETIME2           NOT NULL,

        CONSTRAINT pk_registration_tokens      PRIMARY KEY (token),
        CONSTRAINT fk_registration_tokens_user FOREIGN KEY (user_guid)
            REFERENCES [zootify].[users] (guid) ON DELETE CASCADE
    )
END
GO

-- ────────────────────────────────────────────────────────────
--  RESET_PASSWORD_TOKENS
-- ────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = 'zootify' AND t.name = 'reset_password_tokens'
)
BEGIN
    CREATE TABLE [zootify].[reset_password_tokens] (
        guid        UNIQUEIDENTIFIER    NOT NULL,
        token       NVARCHAR(128)       NOT NULL,
        user_guid   UNIQUEIDENTIFIER    NOT NULL,
        expires_at  DATETIME2           NOT NULL,

        CONSTRAINT pk_reset_password_tokens      PRIMARY KEY (token),
        CONSTRAINT fk_reset_password_tokens_user FOREIGN KEY (user_guid)
            REFERENCES [zootify].[users] (guid) ON DELETE CASCADE
    )
END
GO

-- ────────────────────────────────────────────────────────────
--  GEO_LOCATIONS
-- ────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = 'zootify' AND t.name = 'geo_locations'
)
BEGIN
    CREATE TABLE [zootify].[geo_locations] (
        guid        UNIQUEIDENTIFIER    NOT NULL,
        name        NVARCHAR(200)       NOT NULL,
        level       INT                 NOT NULL,
        parent_guid UNIQUEIDENTIFIER    NULL,

        CONSTRAINT pk_geo_locations        PRIMARY KEY (guid),
        CONSTRAINT fk_geo_locations_parent FOREIGN KEY (parent_guid)
            REFERENCES [zootify].[geo_locations] (guid) ON DELETE NO ACTION
    )
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'uq_geo_locations_parent_name')
BEGIN
    CREATE UNIQUE INDEX uq_geo_locations_parent_name
        ON [zootify].[geo_locations] (parent_guid, name)
        WHERE parent_guid IS NOT NULL
END
GO

-- ────────────────────────────────────────────────────────────
--  TAXONOMY_NODES
-- ────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = 'zootify' AND t.name = 'taxonomy_nodes'
)
BEGIN
    CREATE TABLE [zootify].[taxonomy_nodes] (
        guid                    UNIQUEIDENTIFIER    NOT NULL,
        name                    NVARCHAR(200)       NOT NULL,
        rank                    INT                 NOT NULL,
        approval_status         INT                 NOT NULL DEFAULT 0,
        parent_guid             UNIQUEIDENTIFIER    NULL,

        created_at              DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
        created_by_user_guid    UNIQUEIDENTIFIER    NULL,

        updated_date            DATETIME2           NULL,
        updated_by_user_guid    UNIQUEIDENTIFIER    NULL,

        deletion_date           DATETIME2           NULL,
        deleted_by_user_guid    UNIQUEIDENTIFIER    NULL,

        CONSTRAINT pk_taxonomy_nodes            PRIMARY KEY (guid),
        CONSTRAINT fk_taxonomy_nodes_parent     FOREIGN KEY (parent_guid)
            REFERENCES [zootify].[taxonomy_nodes] (guid) ON DELETE NO ACTION,
        CONSTRAINT fk_taxonomy_nodes_created_by FOREIGN KEY (created_by_user_guid)
            REFERENCES [zootify].[users] (guid) ON DELETE NO ACTION,
        CONSTRAINT fk_taxonomy_nodes_updated_by FOREIGN KEY (updated_by_user_guid)
            REFERENCES [zootify].[users] (guid) ON DELETE NO ACTION,
        CONSTRAINT fk_taxonomy_nodes_deleted_by FOREIGN KEY (deleted_by_user_guid)
            REFERENCES [zootify].[users] (guid) ON DELETE NO ACTION
    )
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'uq_taxonomy_nodes_parent_name_rank')
BEGIN
    CREATE UNIQUE INDEX uq_taxonomy_nodes_parent_name_rank
        ON [zootify].[taxonomy_nodes] (parent_guid, name, rank)
END
GO

-- ────────────────────────────────────────────────────────────
--  TAGS
-- ────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = 'zootify' AND t.name = 'tags'
)
BEGIN
    CREATE TABLE [zootify].[tags] (
        guid    UNIQUEIDENTIFIER    NOT NULL,
        tag     NVARCHAR(100)       NOT NULL,

        CONSTRAINT pk_tags PRIMARY KEY (guid)
    )
END
GO

-- ────────────────────────────────────────────────────────────
--  SPECIMENS
-- ────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = 'zootify' AND t.name = 'specimens'
)
BEGIN
    CREATE TABLE [zootify].[specimens] (
        guid                    UNIQUEIDENTIFIER    NOT NULL,

        species_guid            UNIQUEIDENTIFIER    NOT NULL,
        geo_location_guid       UNIQUEIDENTIFIER    NOT NULL,

        name                    NVARCHAR(255)       NOT NULL,
        latin_name              NVARCHAR(255)       NOT NULL,
        date_of_discovery       DATETIME2           NOT NULL,

        latitude                FLOAT               NOT NULL,
        longitude               FLOAT               NOT NULL,

        storage_building        NVARCHAR(100)       NULL,
        storage_cabinet         NVARCHAR(50)        NULL,
        storage_shelf           NVARCHAR(50)        NULL,
        storage_compartment     NVARCHAR(50)        NULL,

        comment                 NVARCHAR(2000)      NULL,
        is_public               BIT                 NOT NULL DEFAULT 1,
        status                  INT                 NOT NULL DEFAULT 1,

        created_at              DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
        created_by_user_guid    UNIQUEIDENTIFIER    NULL,

        updated_date            DATETIME2           NULL,
        updated_by_user_guid    UNIQUEIDENTIFIER    NULL,

        deletion_date           DATETIME2           NULL,
        deleted_by_user_guid    UNIQUEIDENTIFIER    NULL,

        CONSTRAINT pk_specimens              PRIMARY KEY (guid),
        CONSTRAINT fk_specimens_species      FOREIGN KEY (species_guid)
            REFERENCES [zootify].[taxonomy_nodes] (guid) ON DELETE NO ACTION,
        CONSTRAINT fk_specimens_geo_location FOREIGN KEY (geo_location_guid)
            REFERENCES [zootify].[geo_locations] (guid)  ON DELETE NO ACTION,
        CONSTRAINT fk_specimens_created_by   FOREIGN KEY (created_by_user_guid)
            REFERENCES [zootify].[users] (guid)          ON DELETE NO ACTION,
        CONSTRAINT fk_specimens_updated_by   FOREIGN KEY (updated_by_user_guid)
            REFERENCES [zootify].[users] (guid)          ON DELETE NO ACTION,
        CONSTRAINT fk_specimens_deleted_by   FOREIGN KEY (deleted_by_user_guid)
            REFERENCES [zootify].[users] (guid)          ON DELETE NO ACTION
    )
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'ix_specimens_is_public')
BEGIN
    CREATE INDEX ix_specimens_is_public ON [zootify].[specimens] (is_public)
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'ix_specimens_species_guid')
BEGIN
    CREATE INDEX ix_specimens_species_guid ON [zootify].[specimens] (species_guid)
END
GO

-- ────────────────────────────────────────────────────────────
--  SPECIMENS_TAGS  (join table)
-- ────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = 'zootify' AND t.name = 'specimens_tags'
)
BEGIN
    CREATE TABLE [zootify].[specimens_tags] (
        guid            UNIQUEIDENTIFIER    NOT NULL,
        tag_guid        UNIQUEIDENTIFIER    NOT NULL,
        specimen_guid   UNIQUEIDENTIFIER    NOT NULL,

        CONSTRAINT pk_specimens_tags          PRIMARY KEY (guid),
        CONSTRAINT fk_specimens_tags_tag      FOREIGN KEY (tag_guid)
            REFERENCES [zootify].[tags] (guid)      ON DELETE CASCADE,
        CONSTRAINT fk_specimens_tags_specimen FOREIGN KEY (specimen_guid)
            REFERENCES [zootify].[specimens] (guid) ON DELETE CASCADE
    )
END
GO

-- ────────────────────────────────────────────────────────────
--  SPECIMEN_IMAGES
-- ────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = 'zootify' AND t.name = 'specimen_images'
)
BEGIN
    CREATE TABLE [zootify].[specimen_images] (
        guid            UNIQUEIDENTIFIER    NOT NULL,
        specimen_guid   UNIQUEIDENTIFIER    NOT NULL,
        image_url       NVARCHAR(1000)      NOT NULL,
        uploaded_at     DATETIME2           NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT pk_specimen_images          PRIMARY KEY (guid),
        CONSTRAINT fk_specimen_images_specimen FOREIGN KEY (specimen_guid)
            REFERENCES [zootify].[specimens] (guid) ON DELETE CASCADE
    )
END
GO

-- ────────────────────────────────────────────────────────────
--  LOANS
-- ────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = 'zootify' AND t.name = 'loans'
)
BEGIN
    CREATE TABLE [zootify].[loans] (
        guid                    UNIQUEIDENTIFIER    NOT NULL,
        specimen_guid           UNIQUEIDENTIFIER    NOT NULL,
        lender_guid             UNIQUEIDENTIFIER    NOT NULL,
        borrower_guid           UNIQUEIDENTIFIER    NULL,
        borrower_external_name  NVARCHAR(256)       NULL,
        loan_date               DATETIME2           NOT NULL,
        due_date                DATETIME2           NOT NULL,
        return_date             DATETIME2           NULL,
        notes                   NVARCHAR(2000)      NULL,

        CONSTRAINT pk_loans          PRIMARY KEY (guid),
        CONSTRAINT fk_loans_specimen FOREIGN KEY (specimen_guid)
            REFERENCES [zootify].[specimens] (guid) ON DELETE CASCADE,
        CONSTRAINT fk_loans_lender   FOREIGN KEY (lender_guid)
            REFERENCES [zootify].[users] (guid)     ON DELETE NO ACTION,
        CONSTRAINT fk_loans_borrower FOREIGN KEY (borrower_guid)
            REFERENCES [zootify].[users] (guid)     ON DELETE NO ACTION
    )
END
GO

-- ────────────────────────────────────────────────────────────
--  USER_SETTINGS
-- ────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = 'zootify' AND t.name = 'user_settings'
)
BEGIN
    CREATE TABLE [zootify].[user_settings] (
        guid        UNIQUEIDENTIFIER    NOT NULL,
        user_guid   UNIQUEIDENTIFIER    NOT NULL,
        dark_mode   BIT                 NOT NULL CONSTRAINT df_user_settings_dark_mode DEFAULT 0,
        language    VARCHAR(10)         NOT NULL CONSTRAINT df_user_settings_language  DEFAULT 'de',

        CONSTRAINT pk_user_settings            PRIMARY KEY (guid),
        CONSTRAINT uq_user_settings_user_guid  UNIQUE      (user_guid),
        CONSTRAINT fk_user_settings_user       FOREIGN KEY (user_guid)
            REFERENCES [zootify].[users] (guid) ON DELETE CASCADE
    )
END
GO

-- ────────────────────────────────────────────────────────────
--  COLLECTIONS
-- ────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = 'zootify' AND t.name = 'collections'
)
BEGIN
    CREATE TABLE [zootify].[collections] (
        guid                    UNIQUEIDENTIFIER    NOT NULL,
        name                    VARCHAR(200)        NOT NULL,
        description             VARCHAR(400)        NULL,
        private                 BIT                 NOT NULL,
        status                  INT                 NOT NULL DEFAULT 1,

        created_at              DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
        created_by_user_guid    UNIQUEIDENTIFIER    NULL,

        updated_date            DATETIME2           NULL,
        updated_by_user_guid    UNIQUEIDENTIFIER    NULL,

        deletion_date           DATETIME2           NULL,
        deleted_by_user_guid    UNIQUEIDENTIFIER    NULL,

        CONSTRAINT pk_collections            PRIMARY KEY (guid),
        CONSTRAINT fk_collection_created_by  FOREIGN KEY (created_by_user_guid)
            REFERENCES [zootify].[users] (guid) ON DELETE NO ACTION,
        CONSTRAINT fk_collections_updated_by FOREIGN KEY (updated_by_user_guid)
            REFERENCES [zootify].[users] (guid) ON DELETE NO ACTION,
        CONSTRAINT fk_collections_deleted_by FOREIGN KEY (deleted_by_user_guid)
            REFERENCES [zootify].[users] (guid) ON DELETE NO ACTION
    )
END
GO

-- ────────────────────────────────────────────────────────────
--  SPECIMEN_COLLECTION  (join table)
-- ────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = 'zootify' AND t.name = 'specimen_collection'
)
BEGIN
    CREATE TABLE [zootify].[specimen_collection] (
        guid            UNIQUEIDENTIFIER    NOT NULL,
        collection_guid UNIQUEIDENTIFIER    NOT NULL,
        specimen_guid   UNIQUEIDENTIFIER    NOT NULL,

        CONSTRAINT pk_specimen_collection PRIMARY KEY (guid),
        CONSTRAINT fk_collection_guid     FOREIGN KEY (collection_guid)
            REFERENCES [zootify].[collections] (guid) ON DELETE CASCADE,
        CONSTRAINT fk_specimen_guid       FOREIGN KEY (specimen_guid)
            REFERENCES [zootify].[specimens] (guid)   ON DELETE CASCADE
    )
END
GO
