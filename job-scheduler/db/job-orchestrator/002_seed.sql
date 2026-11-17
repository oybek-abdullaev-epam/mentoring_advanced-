-- job-orchestrator: seed data (3 snapshots, 2 finished runs, 2 processed run-outbox rows). Idempotent: every INSERT is guarded.
-- Snapshot NextRunAt is 2099-01-01 so integration-test claims never pick seed rows.
-- Insert order follows FK order: JobScheduleSnapshot -> JobRun -> JobRunOutbox.
-- Snapshot.LaunchDetails = LaunchDetailsPayload JSON; JobRunOutbox.Payload = ExecuteJobPayload JSON
-- (deadline = ScheduledFor + 15 min). headers/payload inside launchDetails are JSON *strings*.
SET ANSI_NULLS ON; SET QUOTED_IDENTIFIER ON; SET NOCOUNT ON;
GO

DECLARE @UserA UNIQUEIDENTIFIER = 'AAAAAAAA-0000-0000-0000-000000000001';
DECLARE @UserB UNIQUEIDENTIFIER = 'BBBBBBBB-0000-0000-0000-000000000002';
DECLARE @Job1  UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000001';
DECLARE @Job2  UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000002';
DECLARE @Job3  UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000003';
DECLARE @Run1  UNIQUEIDENTIFIER = '30000000-0000-0000-0000-000000000001';
DECLARE @Run2  UNIQUEIDENTIFIER = '30000000-0000-0000-0000-000000000002';
DECLARE @RunOutbox1 UNIQUEIDENTIFIER = '40000000-0000-0000-0000-000000000001';
DECLARE @RunOutbox2 UNIQUEIDENTIFIER = '40000000-0000-0000-0000-000000000002';

-- ---- JobScheduleSnapshot (CurrentRunId/LockedUntil NULL: no lock held) -----------------------
IF NOT EXISTS (SELECT 1 FROM dbo.JobScheduleSnapshot WHERE JobId = @Job1)
  INSERT INTO dbo.JobScheduleSnapshot (JobId, UserId, ConcurrencyMode, ScheduleType, RepeatStepSize, RepeatStepUnit, NextRunAt, LaunchDetails, Status, Version)
  VALUES (@Job1, @UserA, N'Concurrent', N'Recurring', 1, N'Day', '2099-01-01T02:00:00',
    N'{"endpoint":"https://example.com/hooks/nightly-report","httpMethod":"POST","headers":"{\"Content-Type\":\"application/json\"}","payload":"{\"format\":\"pdf\"}"}',
    N'Active', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.JobScheduleSnapshot WHERE JobId = @Job2)
  INSERT INTO dbo.JobScheduleSnapshot (JobId, UserId, ConcurrencyMode, ScheduleType, RepeatStepSize, RepeatStepUnit, NextRunAt, LaunchDetails, Status, Version)
  VALUES (@Job2, @UserA, N'Singleton', N'Recurring', 15, N'Minute', '2099-01-01T00:00:00',
    N'{"endpoint":"https://example.com/hooks/sync-crm","httpMethod":"GET","headers":null,"payload":null}',
    N'Active', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.JobScheduleSnapshot WHERE JobId = @Job3)
  INSERT INTO dbo.JobScheduleSnapshot (JobId, UserId, ConcurrencyMode, ScheduleType, RepeatStepSize, RepeatStepUnit, NextRunAt, LaunchDetails, Status, Version)
  VALUES (@Job3, @UserB, N'Singleton', N'OneTime', NULL, NULL, '2099-01-01T05:00:00',
    N'{"endpoint":"https://example.com/hooks/migrate","httpMethod":"PUT","headers":null,"payload":"{\"batch\":42}"}',
    N'Active', 1);

-- ---- JobRun (two finished historical runs) ---------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.JobRun WHERE Id = @Run1)
  INSERT INTO dbo.JobRun (Id, JobId, ScheduledFor, Status, Deadline, StartedAt, FinishedAt)
  VALUES (@Run1, @Job1, '2026-01-01T02:00:00', N'Succeeded', '2026-01-01T02:15:00', '2026-01-01T02:00:01', '2026-01-01T02:00:30');

IF NOT EXISTS (SELECT 1 FROM dbo.JobRun WHERE Id = @Run2)
  INSERT INTO dbo.JobRun (Id, JobId, ScheduledFor, Status, Deadline, StartedAt, FinishedAt)
  VALUES (@Run2, @Job2, '2026-01-01T00:15:00', N'Succeeded', '2026-01-01T00:30:00', '2026-01-01T00:15:01', '2026-01-01T00:15:10');

-- ---- JobRunOutbox (already processed) --------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.JobRunOutbox WHERE Id = @RunOutbox1)
  INSERT INTO dbo.JobRunOutbox (Id, RunId, EventType, Payload, ScheduledEnqueueAt, ProcessedAt)
  VALUES (@RunOutbox1, @Run1, N'ExecuteJob',
    N'{"runId":"30000000-0000-0000-0000-000000000001","jobId":"10000000-0000-0000-0000-000000000001","scheduledFor":"2026-01-01T02:00:00","deadline":"2026-01-01T02:15:00","launchDetails":{"endpoint":"https://example.com/hooks/nightly-report","httpMethod":"POST","headers":"{\"Content-Type\":\"application/json\"}","payload":"{\"format\":\"pdf\"}"}}',
    '2026-01-01T02:00:00', '2026-01-01T02:00:00');

IF NOT EXISTS (SELECT 1 FROM dbo.JobRunOutbox WHERE Id = @RunOutbox2)
  INSERT INTO dbo.JobRunOutbox (Id, RunId, EventType, Payload, ScheduledEnqueueAt, ProcessedAt)
  VALUES (@RunOutbox2, @Run2, N'ExecuteJob',
    N'{"runId":"30000000-0000-0000-0000-000000000002","jobId":"10000000-0000-0000-0000-000000000002","scheduledFor":"2026-01-01T00:15:00","deadline":"2026-01-01T00:30:00","launchDetails":{"endpoint":"https://example.com/hooks/sync-crm","httpMethod":"GET","headers":null,"payload":null}}',
    '2026-01-01T00:15:00', '2026-01-01T00:15:00');
GO

-- Expected: JobScheduleSnapshot 3, JobRun 2, JobRunOutbox 2
SELECT N'JobScheduleSnapshot' AS TableName, COUNT(*) AS [RowCount] FROM dbo.JobScheduleSnapshot
UNION ALL SELECT N'JobRun',       COUNT(*) FROM dbo.JobRun
UNION ALL SELECT N'JobRunOutbox', COUNT(*) FROM dbo.JobRunOutbox;
GO
