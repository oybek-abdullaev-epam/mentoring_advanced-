-- job-details: seed data (3 jobs, fixed IDs so evidence is reproducible). Idempotent: every INSERT is guarded.
-- NextRunAt/StartAt is 2099-01-01 so integration-test claims never pick seed rows.
-- Outbox payloads are JobSnapshotPayload (camelCase, enums as strings); headers/payload inside launchDetails
-- are JSON *strings* (escaped).
SET ANSI_NULLS ON; SET QUOTED_IDENTIFIER ON; SET NOCOUNT ON;
GO

DECLARE @UserA UNIQUEIDENTIFIER = 'AAAAAAAA-0000-0000-0000-000000000001';
DECLARE @UserB UNIQUEIDENTIFIER = 'BBBBBBBB-0000-0000-0000-000000000002';
DECLARE @Job1  UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000001';
DECLARE @Job2  UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000002';
DECLARE @Job3  UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000003';
DECLARE @Outbox1 UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000001';
DECLARE @Outbox2 UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000002';
DECLARE @Outbox3 UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000003';
DECLARE @Processed DATETIME2(3) = '2026-01-01T00:00:00';

-- ---- Job -------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Job WHERE Id = @Job1)
  INSERT INTO dbo.Job (Id, UserId, Name, Description, Status, ConcurrencyMode, Version)
  VALUES (@Job1, @UserA, N'nightly-report', N'Generates the nightly PDF report', N'Active', N'Concurrent', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.Job WHERE Id = @Job2)
  INSERT INTO dbo.Job (Id, UserId, Name, Description, Status, ConcurrencyMode, Version)
  VALUES (@Job2, @UserA, N'sync-crm', N'Pulls CRM changes every 15 minutes', N'Active', N'Singleton', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.Job WHERE Id = @Job3)
  INSERT INTO dbo.Job (Id, UserId, Name, Description, Status, ConcurrencyMode, Version)
  VALUES (@Job3, @UserB, N'one-off-migration', N'One-time data migration', N'Active', N'Singleton', 1);

-- ---- JobSchedule -----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.JobSchedule WHERE JobId = @Job1)
  INSERT INTO dbo.JobSchedule (JobId, ScheduleType, StartAt, RepeatStepSize, RepeatStepUnit)
  VALUES (@Job1, N'Recurring', '2099-01-01T02:00:00', 1, N'Day');

IF NOT EXISTS (SELECT 1 FROM dbo.JobSchedule WHERE JobId = @Job2)
  INSERT INTO dbo.JobSchedule (JobId, ScheduleType, StartAt, RepeatStepSize, RepeatStepUnit)
  VALUES (@Job2, N'Recurring', '2099-01-01T00:00:00', 15, N'Minute');

IF NOT EXISTS (SELECT 1 FROM dbo.JobSchedule WHERE JobId = @Job3)
  INSERT INTO dbo.JobSchedule (JobId, ScheduleType, StartAt, RepeatStepSize, RepeatStepUnit)
  VALUES (@Job3, N'OneTime', '2099-01-01T05:00:00', NULL, NULL);

-- ---- JobLaunchDetails ------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.JobLaunchDetails WHERE JobId = @Job1)
  INSERT INTO dbo.JobLaunchDetails (JobId, Endpoint, HttpMethod, Headers, Payload)
  VALUES (@Job1, N'https://example.com/hooks/nightly-report', N'POST', N'{"Content-Type":"application/json"}', N'{"format":"pdf"}');

IF NOT EXISTS (SELECT 1 FROM dbo.JobLaunchDetails WHERE JobId = @Job2)
  INSERT INTO dbo.JobLaunchDetails (JobId, Endpoint, HttpMethod, Headers, Payload)
  VALUES (@Job2, N'https://example.com/hooks/sync-crm', N'GET', NULL, NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.JobLaunchDetails WHERE JobId = @Job3)
  INSERT INTO dbo.JobLaunchDetails (JobId, Endpoint, HttpMethod, Headers, Payload)
  VALUES (@Job3, N'https://example.com/hooks/migrate', N'PUT', NULL, N'{"batch":42}');

-- ---- JobOutbox (already processed, so no relay picks them up) --------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.JobOutbox WHERE Id = @Outbox1)
  INSERT INTO dbo.JobOutbox (Id, JobId, EventType, JobVersion, Payload, ProcessedAt)
  VALUES (@Outbox1, @Job1, N'JobCreated', 1,
    N'{"jobId":"10000000-0000-0000-0000-000000000001","userId":"AAAAAAAA-0000-0000-0000-000000000001","version":1,"status":"Active","concurrencyMode":"Concurrent","scheduleType":"Recurring","startAt":"2099-01-01T02:00:00","repeatStepSize":1,"repeatStepUnit":"Day","launchDetails":{"endpoint":"https://example.com/hooks/nightly-report","httpMethod":"POST","headers":"{\"Content-Type\":\"application/json\"}","payload":"{\"format\":\"pdf\"}"}}',
    @Processed);

IF NOT EXISTS (SELECT 1 FROM dbo.JobOutbox WHERE Id = @Outbox2)
  INSERT INTO dbo.JobOutbox (Id, JobId, EventType, JobVersion, Payload, ProcessedAt)
  VALUES (@Outbox2, @Job2, N'JobCreated', 1,
    N'{"jobId":"10000000-0000-0000-0000-000000000002","userId":"AAAAAAAA-0000-0000-0000-000000000001","version":1,"status":"Active","concurrencyMode":"Singleton","scheduleType":"Recurring","startAt":"2099-01-01T00:00:00","repeatStepSize":15,"repeatStepUnit":"Minute","launchDetails":{"endpoint":"https://example.com/hooks/sync-crm","httpMethod":"GET","headers":null,"payload":null}}',
    @Processed);

IF NOT EXISTS (SELECT 1 FROM dbo.JobOutbox WHERE Id = @Outbox3)
  INSERT INTO dbo.JobOutbox (Id, JobId, EventType, JobVersion, Payload, ProcessedAt)
  VALUES (@Outbox3, @Job3, N'JobCreated', 1,
    N'{"jobId":"10000000-0000-0000-0000-000000000003","userId":"BBBBBBBB-0000-0000-0000-000000000002","version":1,"status":"Active","concurrencyMode":"Singleton","scheduleType":"OneTime","startAt":"2099-01-01T05:00:00","repeatStepSize":null,"repeatStepUnit":null,"launchDetails":{"endpoint":"https://example.com/hooks/migrate","httpMethod":"PUT","headers":null,"payload":"{\"batch\":42}"}}',
    @Processed);
GO

-- Expected: Job 3, JobSchedule 3, JobLaunchDetails 3, JobOutbox 3
SELECT N'Job' AS TableName, COUNT(*) AS [RowCount] FROM dbo.Job
UNION ALL SELECT N'JobSchedule',      COUNT(*) FROM dbo.JobSchedule
UNION ALL SELECT N'JobLaunchDetails', COUNT(*) FROM dbo.JobLaunchDetails
UNION ALL SELECT N'JobOutbox',        COUNT(*) FROM dbo.JobOutbox;
GO
