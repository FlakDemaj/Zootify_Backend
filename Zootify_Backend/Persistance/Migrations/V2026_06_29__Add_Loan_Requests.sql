-- ────────────────────────────────────────────────────────────
--  LOAN_REQUESTS
-- ────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = 'zootify' AND t.name = 'loan_requests'
)
BEGIN
    CREATE TABLE [zootify].[loan_requests] (
        guid                    UNIQUEIDENTIFIER    NOT NULL,
        specimen_guid           UNIQUEIDENTIFIER    NOT NULL,
        requester_guid          UNIQUEIDENTIFIER    NOT NULL,
        status                  INT                 NOT NULL,
        desired_due_date        DATETIME2           NOT NULL,
        requester_note          NVARCHAR(2000)      NULL,
        requested_at            DATETIME2           NOT NULL,
        admin_approver_guid     UNIQUEIDENTIFIER    NULL,
        admin_approved_at       DATETIME2           NULL,
        creator_approver_guid   UNIQUEIDENTIFIER    NULL,
        creator_approved_at     DATETIME2           NULL,
        rejected_by_guid        UNIQUEIDENTIFIER    NULL,
        rejected_at             DATETIME2           NULL,
        rejection_reason        NVARCHAR(2000)      NULL,
        created_loan_guid       UNIQUEIDENTIFIER    NULL,

        CONSTRAINT pk_loan_requests           PRIMARY KEY (guid),
        CONSTRAINT fk_loan_requests_specimen  FOREIGN KEY (specimen_guid)
            REFERENCES [zootify].[specimens] (guid) ON DELETE CASCADE,
        CONSTRAINT fk_loan_requests_requester FOREIGN KEY (requester_guid)
            REFERENCES [zootify].[users] (guid)     ON DELETE NO ACTION,
        CONSTRAINT fk_loan_requests_admin     FOREIGN KEY (admin_approver_guid)
            REFERENCES [zootify].[users] (guid)     ON DELETE NO ACTION,
        CONSTRAINT fk_loan_requests_creator   FOREIGN KEY (creator_approver_guid)
            REFERENCES [zootify].[users] (guid)     ON DELETE NO ACTION,
        CONSTRAINT fk_loan_requests_rejecter  FOREIGN KEY (rejected_by_guid)
            REFERENCES [zootify].[users] (guid)     ON DELETE NO ACTION
    )
END
GO
