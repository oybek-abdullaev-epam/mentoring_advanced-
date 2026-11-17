-- job-orchestrator: schema (Job Orchestrator, UC 1.1 snapshot + UC 2.1 runs). Idempotent: safe to re-run.
-- Notes:
--   * READ_COMMITTED_SNAPSHOT (RCSI) is ON by default in Azure SQL Database - do not toggle it here.
--   * All timestamps are UTC (DATETIME2(3)). Enum columns store the C# enum member name exactly.
--   * No cross-database foreign keys (JobScheduleSnapshot is a copy of job-details data, linked by JobId only).
SET ANSI_NULLS ON; SET QUOTED_IDENTIFIER ON; SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.JobScheduleSnapshot', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.JobScheduleSnapshot (
    JobId           UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_JobScheduleSnapshot PRIMARY KEY,
    UserId          UNIQUEIDENTIFIER NOT NULL,
    ConcurrencyMode NVARCHAR(20)     NOT NULL CONSTRAINT CK_Snapshot_ConcurrencyMode CHECK (ConcurrencyMode IN (N'Singleton', N'Concurrent')),
    ScheduleType    NVARCHAR(20)     NOT NULL CONSTRAINT CK_Snapshot_ScheduleType CHECK (ScheduleType IN (N'OneTime', N'Recurring')),
    RepeatStepSize  INT              NULL,
    RepeatStepUnit  NVARCHAR(10)     NULL CONSTRAINT CK_Snapshot_RepeatStepUnit CHECK (RepeatStepUnit IN (N'Minute', N'Hour', N'Day', N'Week', N'Month')),
    NextRunAt       DATETIME2(3)     NOT NULL,
    LaunchDetails   NVARCHAR(MAX)    NOT NULL CONSTRAINT CK_Snapshot_LaunchDetails CHECK (ISJSON(LaunchDetails) = 1),
    Status          NVARCHAR(20)     NOT NULL CONSTRAINT CK_Snapshot_Status CHECK (Status IN (N'Active', N'Inactive', N'Deleted')),
    Version         INT              NOT NULL,
    CreatedAt       DATETIME2(3)     NOT NULL CONSTRAINT DF_Snapshot_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CurrentRunId    UNIQUEIDENTIFIER NULL,
    LockedUntil     DATETIME2(3)     NULL
  );
END
GO

-- Added after the table: also applies to databases created before this constraint existed.
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Snapshot_Repeat' AND parent_object_id = OBJECT_ID(N'dbo.JobScheduleSnapshot'))
BEGIN
  ALTER TABLE dbo.JobScheduleSnapshot ADD CONSTRAINT CK_Snapshot_Repeat CHECK (
       (ScheduleType = N'OneTime'   AND RepeatStepSize IS NULL AND RepeatStepUnit IS NULL)
    OR (ScheduleType = N'Recurring' AND RepeatStepSize > 0    AND RepeatStepUnit IS NOT NULL));
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_JobScheduleSnapshot_Due' AND object_id = OBJECT_ID(N'dbo.JobScheduleSnapshot'))
BEGIN
  CREATE INDEX IX_JobScheduleSnapshot_Due ON dbo.JobScheduleSnapshot (NextRunAt) WHERE Status = N'Active';
END
GO

IF OBJECT_ID(N'dbo.JobRun', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.JobRun (
    Id                       UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_JobRun PRIMARY KEY,
    JobId                    UNIQUEIDENTIFIER NOT NULL CONSTRAINT FK_JobRun_Snapshot REFERENCES dbo.JobScheduleSnapshot (JobId),
    ScheduledFor             DATETIME2(3)     NOT NULL,
    Status                   NVARCHAR(20)     NOT NULL CONSTRAINT CK_JobRun_Status CHECK (Status IN (N'Pending', N'Running', N'Succeeded', N'Failed', N'TimedOut', N'LockFailed', N'Cancelled')),
    ServiceBusSequenceNumber BIGINT           NULL,
    Deadline                 DATETIME2(3)     NULL,
    StartedAt                DATETIME2(3)     NULL,
    FinishedAt               DATETIME2(3)     NULL,
    ErrorSummary             NVARCHAR(1000)   NULL,
    CreatedAt                DATETIME2(3)     NOT NULL CONSTRAINT DF_JobRun_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT UQ_JobRun_JobId_ScheduledFor UNIQUE (JobId, ScheduledFor)
  );
END
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_JobRun_Status_Deadline' AND object_id = OBJECT_ID(N'dbo.JobRun'))
BEGIN
  DROP INDEX IX_JobRun_Status_Deadline ON dbo.JobRun;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_JobRun_Overdue' AND object_id = OBJECT_ID(N'dbo.JobRun'))
BEGIN
  CREATE INDEX IX_JobRun_Overdue ON dbo.JobRun (Deadline) WHERE Status IN (N'Pending', N'Running');
END
GO

IF OBJECT_ID(N'dbo.JobRunOutbox', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.JobRunOutbox (
    Id                 UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_JobRunOutbox PRIMARY KEY,
    RunId              UNIQUEIDENTIFIER NOT NULL CONSTRAINT FK_JobRunOutbox_JobRun REFERENCES dbo.JobRun (Id),
    EventType          NVARCHAR(20)     NOT NULL CONSTRAINT CK_JobRunOutbox_EventType CHECK (EventType IN (N'ExecuteJob', N'NotifyFailure')),
    Payload            NVARCHAR(MAX)    NOT NULL CONSTRAINT CK_JobRunOutbox_Payload CHECK (ISJSON(Payload) = 1),
    ScheduledEnqueueAt DATETIME2(3)     NOT NULL,
    CreatedAt          DATETIME2(3)     NOT NULL CONSTRAINT DF_JobRunOutbox_CreatedAt DEFAULT (SYSUTCDATETIME()),
    ProcessedAt        DATETIME2(3)     NULL,
    Attempts           INT              NOT NULL CONSTRAINT DF_JobRunOutbox_Attempts DEFAULT (0)
  );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_JobRunOutbox_Pending' AND object_id = OBJECT_ID(N'dbo.JobRunOutbox'))
BEGIN
  CREATE INDEX IX_JobRunOutbox_Pending ON dbo.JobRunOutbox (CreatedAt) WHERE ProcessedAt IS NULL;
END
GO
