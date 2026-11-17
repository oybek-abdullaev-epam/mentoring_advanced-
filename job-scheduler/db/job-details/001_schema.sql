-- job-details: schema (Job Manager, UC 1.1). Idempotent: safe to re-run.
-- Notes:
--   * READ_COMMITTED_SNAPSHOT (RCSI) is ON by default in Azure SQL Database - do not toggle it here.
--   * All timestamps are UTC (DATETIME2(3)). Enum columns store the C# enum member name exactly.
--   * No cross-database foreign keys.
SET ANSI_NULLS ON; SET QUOTED_IDENTIFIER ON; SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.Job', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.Job (
    Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Job PRIMARY KEY,
    UserId          UNIQUEIDENTIFIER NOT NULL,
    Name            NVARCHAR(200)    NOT NULL,
    Description     NVARCHAR(1000)   NULL,
    Status          NVARCHAR(20)     NOT NULL CONSTRAINT CK_Job_Status CHECK (Status IN (N'Active', N'Inactive', N'Deleted')),
    ConcurrencyMode NVARCHAR(20)     NOT NULL CONSTRAINT CK_Job_ConcurrencyMode CHECK (ConcurrencyMode IN (N'Singleton', N'Concurrent')),
    Version         INT              NOT NULL CONSTRAINT DF_Job_Version DEFAULT (1),
    CreatedAt       DATETIME2(3)     NOT NULL CONSTRAINT DF_Job_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt       DATETIME2(3)     NOT NULL CONSTRAINT DF_Job_UpdatedAt DEFAULT (SYSUTCDATETIME())
  );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Job_UserId_Name_NotDeleted' AND object_id = OBJECT_ID(N'dbo.Job'))
BEGIN
  CREATE UNIQUE INDEX UX_Job_UserId_Name_NotDeleted ON dbo.Job (UserId, Name) WHERE Status <> N'Deleted';
END
GO

IF OBJECT_ID(N'dbo.JobSchedule', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.JobSchedule (
    JobId          UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_JobSchedule PRIMARY KEY
                                    CONSTRAINT FK_JobSchedule_Job REFERENCES dbo.Job (Id),
    ScheduleType   NVARCHAR(20)     NOT NULL CONSTRAINT CK_JobSchedule_ScheduleType CHECK (ScheduleType IN (N'OneTime', N'Recurring')),
    StartAt        DATETIME2(3)     NOT NULL,
    RepeatStepSize INT              NULL,
    RepeatStepUnit NVARCHAR(10)     NULL CONSTRAINT CK_JobSchedule_RepeatStepUnit CHECK (RepeatStepUnit IN (N'Minute', N'Hour', N'Day', N'Week', N'Month')),
    CONSTRAINT CK_JobSchedule_Repeat CHECK (
         (ScheduleType = N'OneTime'   AND RepeatStepSize IS NULL AND RepeatStepUnit IS NULL)
      OR (ScheduleType = N'Recurring' AND RepeatStepSize > 0    AND RepeatStepUnit IS NOT NULL))
  );
END
GO

IF OBJECT_ID(N'dbo.JobLaunchDetails', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.JobLaunchDetails (
    JobId      UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_JobLaunchDetails PRIMARY KEY
                                CONSTRAINT FK_JobLaunchDetails_Job REFERENCES dbo.Job (Id),
    Endpoint   NVARCHAR(2048)   NOT NULL,
    HttpMethod NVARCHAR(10)     NOT NULL CONSTRAINT CK_JobLaunchDetails_HttpMethod CHECK (HttpMethod IN (N'GET', N'POST', N'PUT', N'PATCH', N'DELETE')),
    Headers    NVARCHAR(MAX)    NULL CONSTRAINT CK_JobLaunchDetails_Headers CHECK (Headers IS NULL OR ISJSON(Headers) = 1),
    Payload    NVARCHAR(MAX)    NULL CONSTRAINT CK_JobLaunchDetails_Payload CHECK (Payload IS NULL OR ISJSON(Payload) = 1)
  );
END
GO

IF OBJECT_ID(N'dbo.JobOutbox', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.JobOutbox (
    Id          UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_JobOutbox PRIMARY KEY,
    JobId       UNIQUEIDENTIFIER NOT NULL CONSTRAINT FK_JobOutbox_Job REFERENCES dbo.Job (Id),
    EventType   NVARCHAR(20)     NOT NULL CONSTRAINT CK_JobOutbox_EventType CHECK (EventType IN (N'JobCreated', N'JobUpdated', N'JobDeleted')),
    JobVersion  INT              NOT NULL,
    Payload     NVARCHAR(MAX)    NOT NULL CONSTRAINT CK_JobOutbox_Payload CHECK (ISJSON(Payload) = 1),
    CreatedAt   DATETIME2(3)     NOT NULL CONSTRAINT DF_JobOutbox_CreatedAt DEFAULT (SYSUTCDATETIME()),
    ProcessedAt DATETIME2(3)     NULL,
    Attempts    INT              NOT NULL CONSTRAINT DF_JobOutbox_Attempts DEFAULT (0)
  );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_JobOutbox_Pending' AND object_id = OBJECT_ID(N'dbo.JobOutbox'))
BEGIN
  CREATE INDEX IX_JobOutbox_Pending ON dbo.JobOutbox (CreatedAt) WHERE ProcessedAt IS NULL;
END
GO
