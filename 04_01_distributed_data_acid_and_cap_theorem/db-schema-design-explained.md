# DB Schema Design: UC 1.1 and UC 2.1 Explained

Personal notes for the diagrams:
- `diagrams/UC1.1_CreateNewJob_DbSchemaDesign.png`
- `diagrams/UC2.1_ExecuteJobAtScheduledTime_DbSchemaDesign.png`

They cover why every table and column exists, what the notes on the diagrams mean, what was
deliberately left out, and questions a reviewer is likely to ask.

---

## 1. The big picture

The schema follows the architecture from lesson 3 (`03_application_components_capabilities/03-home-task-solution.md`).
Each service owns its own database, and no other service reads or writes it.

| Database | Owner | Holds | Used in |
|---|---|---|---|
| Azure SQL Database `job-details` | Job Manager | What the user defined: job, schedule, HTTP call | UC 1.1 |
| Azure SQL Hyperscale `job-orchestrator` | Job Orchestrator | Schedule snapshots, runs, locks | UC 1.1 (snapshot), UC 2.1 |
| Azure Cosmos DB `job-outputs` | Runners | Logs and output of each run | UC 2.1 |

Lines between tables **inside** one database are real foreign keys. Arrows **between** databases are
**logical references**. SQL Server can't enforce a foreign key across databases, and each service must
stay independent. The same ID is used on both sides (`Job.Id = JobScheduleSnapshot.JobId`,
`JobRun.Id = JobOutputEntry.RunId`), and the application keeps them consistent.

**Why the Orchestrator keeps its own copy of each job (the "snapshot")** instead of asking Job Manager:
- **Availability:** jobs keep running even if Job Manager or its database is down.
- **Performance:** the scheduling loop asks "which jobs are due now?" many times a minute, so it needs a
  local, indexed table. A REST call per job would be far too slow.
- **Isolation:** the scheduler's load never touches the database that serves user requests.

The cost of keeping a copy is that the two must be kept in sync, which needs an outbox, versioning and
an idempotent upsert. Most of the UC 1.1 design deals with that.

---

## 2. Notation and type conventions

- `[PK]` primary key, `[FK]` foreign key, `[PK, FK]` both: the table shares the parent's key (1:1).
- `(required)` means NOT NULL, `(optional)` means nullable, and `(auto)` means filled by a DEFAULT, e.g. `SYSUTCDATETIME()`.
- `UQ (...)` is a unique constraint or unique index. `IX (...)` is a non-unique index. `WHERE ...` means a **filtered index**.
- **`UNIQUEIDENTIFIER`**: GUIDs. IDs can be generated in the application before the insert, which helps
  idempotency (a retried request carries the same ID). They're also safe to use across services. The downside
  is index fragmentation when random GUIDs are the clustered key; see section 7.
- **`DATETIME2`**: more precise than `DATETIME`, with a wider range. **All times are UTC.**
- **`ENUM`**: shorthand only, because SQL Server has no ENUM type. In practice it's `TINYINT` or `VARCHAR(20)`
  plus a `CHECK` constraint, or a lookup table.
- **`NVARCHAR(MAX) JSON`**: JSON stored as text. SQL Server can query it with `JSON_VALUE` / `OPENJSON`,
  but we never filter on what's inside it. It's only passed through.
- **Cosmos types** (`GUID`, `LONG`, `DATETIME`, `TEXT`): conceptual. In JSON they're stored as string or number.

---

## 3. UC 1.1: Create a New Job

### 3.1 The flow
1. The user submits a job. Job Manager validates it: name, schedule, HTTPS public endpoint.
2. **One transaction** in `job-details`: INSERT `Job` + `JobSchedule` + `JobLaunchDetails` + `JobOutbox`.
3. Return `201 Created` with the JobId to the user.
4. A background outbox worker reads unsent `JobOutbox` rows and calls the Orchestrator REST endpoint,
   which upserts `JobScheduleSnapshot`. It retries until it succeeds.

> Note: the lesson 2 sequence diagram shows the sync to the Orchestrator happening **before** the user gets
> a response. The schema follows the lesson 3 **outbox** design instead, so the sync is asynchronous
> (eventually consistent). The user gets a fast response, and the job reaches the scheduler within seconds.
> The reason is that a synchronous call in the middle of the request makes job creation fail whenever the
> Orchestrator is down.

### 3.2 `Job`: identity and metadata of a job
| Column | Why |
|---|---|
| `Id` | Job identity, also used as the key of the snapshot in the other database. |
| `UserId` | Owner. Comes from Entra External ID (the `oid` claim). There's no Users table because identity lives in Entra. |
| `Name` | Shown to the user. 200 characters is plenty. |
| `Description` | Optional free text. |
| `Status` Active / Inactive / Deleted | Inactive means paused, so the schedule is kept but nothing runs. **Deleted is a soft delete**; see below. |
| `ConcurrencyMode` Singleton / Concurrent | Singleton means at most one run at a time, and the next run waits for the lock. Concurrent means runs may overlap. It drives the lock logic in UC 2.1. |
| `Version` | Starts at 1, +1 on every edit. Copied into the outbox and the snapshot so the Orchestrator can **reject out-of-date syncs**; see 3.6. |
| `CreatedAt` / `UpdatedAt` | Auditing. `UpdatedAt` is set by the application (or a trigger), because a DEFAULT only applies on INSERT. |
| `UQ (UserId, Name) WHERE Status <> Deleted` | A user can't have two live jobs with the same name. The filter lets a deleted job's name be reused. Technically it's a **unique filtered index**, because a UNIQUE *constraint* can't have a WHERE clause. |

**Why soft delete?** The outbox has a foreign key to `Job`, and `JobDeleted` must still be sent after the
delete. A hard delete would be blocked by that FK, or would cascade and remove the event before it's sent.
Soft delete also keeps run history linked to a real job.

### 3.3 `JobSchedule`: when it runs (1:1 with Job)
| Column | Why |
|---|---|
| `JobId` [PK, FK] | Shares the job's key. One schedule per job. |
| `ScheduleType` OneTime / Recurring | OneTime runs once at `StartAt`. Recurring repeats. |
| `StartAt` | First (or only) run time, UTC. |
| `RepeatStepSize` + `RepeatStepUnit` | "Every 15 minutes", "every 2 days". Only filled for Recurring. A CHECK could enforce that. |

### 3.4 `JobLaunchDetails`: what HTTP call to make (1:1 with Job)
| Column | Why |
|---|---|
| `Endpoint` | Target URL. 2048 characters is the usual practical URL limit. |
| `HttpMethod` | GET / POST / PUT / PATCH / DELETE. |
| `Headers`, `Payload` | JSON. They can be large, so they're `NVARCHAR(MAX)`. |

**Why three 1:1 tables instead of one wide `Job` table?**
- It separates concerns: who and what (Job), when (Schedule), how (Launch).
- The large `NVARCHAR(MAX)` columns stay out of the frequently read `Job` row. Job lists only need
  `Job` and never load payloads.
- Each part can change on its own, for example a new schedule type with no change to Launch.

One table would also be valid. This is a design choice, not a requirement.

### 3.5 `JobOutbox`: the Transactional Outbox pattern
**The problem it solves:** we need to save the job and notify the Orchestrator. Doing both directly
can't be atomic. If the DB commit succeeds and the REST call fails, the job is never scheduled. If the
REST call succeeds and the commit fails, the Orchestrator schedules a job that doesn't exist.

**The solution:** write the "send this" message as a row **in the same transaction** as the job. Either
both are committed or neither is. A background worker then delivers the row, at least once, retrying
until it succeeds.

| Column | Why |
|---|---|
| `Id` | Message ID. The receiver can use it to detect duplicates. |
| `JobId` [FK] | Which job. |
| `EventType` JobCreated / JobUpdated / JobDeleted | What happened. Created and Updated are both upserts on the Orchestrator side. Deleted sets the snapshot to Status = Deleted. |
| `JobVersion` | Version of the job at the time of the event, used for the version check below. |
| `Payload` | The Job Snapshot JSON the Orchestrator needs (schedule, launch details, mode). |
| `CreatedAt` | Order of delivery. |
| `ProcessedAt` | NULL means not yet delivered. It's set when the Orchestrator responds with success. |
| `Attempts` | Retry counter for backoff and alerting. After N failures, alert or move the row aside. |
| `IX (CreatedAt) WHERE ProcessedAt IS NULL` | The worker's query is "oldest unsent rows first". The filter keeps the index tiny because only pending rows are in it. |

**Important consequence:** delivery is **at least once**, so the same event may arrive twice, or an old
event may arrive after a newer one. That's why the receiver must be **idempotent** and **version-aware**.

### 3.6 `JobScheduleSnapshot` (in `job-orchestrator`)
A denormalized copy of everything the scheduler needs, so it never has to call Job Manager.

| Column | Why |
|---|---|
| `JobId` [PK] | Equal to `Job.Id`, which makes the upsert natural (one row per job). |
| `UserId` | Needed for notifications and execution history without calling Job Manager. |
| `ConcurrencyMode` | Decides whether the claim takes the singleton lock. |
| `ScheduleType`, `RepeatStepSize`, `RepeatStepUnit` | Needed to calculate the **next** run after each run. |
| `NextRunAt` | **Pre-calculated** next run time, which is what "pre-calculated snapshot" means. The scheduler only needs `WHERE NextRunAt <= now`. It never computes schedules inside the query. |
| `LaunchDetails` | JSON blob passed to the Runner. The Orchestrator doesn't look inside it. |
| `Status` | Only Active rows get scheduled. |
| `Version` | Latest version applied, used for the rule below. |
| `IX (NextRunAt) WHERE Status = Active` | **The most important index in the system**: the "due jobs" query. Filtering out paused and deleted jobs keeps it small. |

**The upsert rule:** apply an incoming snapshot only if `incoming Version > stored Version`.
- A duplicate delivery has the same version, so it's ignored. That makes it idempotent.
- An old event arriving late has a lower version, so it's ignored. That handles out-of-order delivery.

**How to write the upsert safely:**
- Plain "SELECT, then INSERT or UPDATE" is a **race**. Two concurrent retries can both see "no row" and
  both try to INSERT.
- **Option A:** just INSERT. If it fails with a PK violation (error 2627), do
  `UPDATE ... WHERE JobId = @Id AND Version < @Version`.
- **Option B:** `MERGE ... WITH (HOLDLOCK)`. HOLDLOCK makes MERGE hold a range lock on the key, so two
  MERGEs can't both decide "not matched". MERGE **without** HOLDLOCK has the same race as SELECT-then-INSERT.

**The upsert must not touch the UC 2.1 lock columns** (`CurrentRunId`, `LockedUntil`). Editing a job must not
release a lock held by a running execution.

### 3.7 The UC 1.1 isolation note
> **READ COMMITTED (RCSI)**. One transaction inserts four new rows and never reads, then writes, so dirty,
> non-repeatable and phantom reads can't affect it. Serializable or Snapshot would add cost with no benefit.

What that means:
- **RCSI = Read Committed Snapshot Isolation.** It's READ COMMITTED where readers see the last committed
  **row version** instead of waiting for locks. Readers don't block writers, and writers don't block readers.
  **It's the default in Azure SQL Database.**
- The isolation-level anomalies (dirty, non-repeatable, phantom reads) all happen when a transaction **reads**
  data that another transaction changes. Our create transaction only **inserts brand-new rows** with
  new GUIDs, and there's nothing for it to read wrongly.
- **Serializable** would add range locks, causing blocking and deadlocks, to protect reads we don't do.
- **Snapshot** (the full SNAPSHOT isolation level) gives a consistent view across the whole transaction,
  which we don't need, and adds update-conflict errors.

> **UQ (UserId, Name) still takes key locks under RCSI → 409 Conflict.**

- RCSI only changes how **reads** work. **Uniqueness checks during INSERT still take locks.**
- So if two requests create "Daily report" for the same user at the same moment, one commits and the
  other either waits and then fails, or fails at once, with a unique violation. We return `409 Conflict`.
- This is why we **don't** check "does the name exist?" with a SELECT first. Under any level below
  Serializable, both requests could pass the SELECT. Letting the database enforce the constraint is
  correct and cheaper.

---

## 4. UC 2.1: Execute a Job At a Scheduled Time

### 4.1 The flow
1. **Claim:** the Orchestrator's scheduling loop runs on one leader, elected with a Blob lease. It finds due
   jobs and, in one transaction per job, creates a `JobRun` plus an outbox message.
2. An outbox worker publishes an **ExecuteJob** message to the Service Bus execution queue, **scheduled** for the
   run time. It stores the returned Service Bus sequence number in `JobRun`.
3. A Runner (Azure Function) receives the message, calls the external HTTP endpoint and streams
   logs to Cosmos DB.
4. The Runner publishes the final status to the status queue. The Orchestrator updates `JobRun`, releases the
   singleton lock and, on failure, queues a notification (UC 3.2).

### 4.2 `JobScheduleSnapshot`: same table as UC 1.1, plus lock columns
| Column | Why |
|---|---|
| `CurrentRunId` | **Who holds the singleton lock.** NULL means free. Points to the `JobRun` that owns it. |
| `LockedUntil` | **Lease expiry**, set to the run's `Deadline`. If a Runner crashes and never reports back, the lock frees itself once this time passes. Without it, a lost status message would block the job forever. |

**Why lock columns on the row instead of a separate lock table, or "check JobRun for a running row"?**
- Taking the lock is **one conditional UPDATE on one row**, which is atomic in SQL Server with no extra hints.
- The lock is **visible in the schema**, and debugging it is just `SELECT CurrentRunId`.
- The check is a primary-key lookup, not a scan of `JobRun`.
- The lease handles crashes automatically.

### 4.3 `JobRun`: one row per execution attempt
| Column | Why |
|---|---|
| `Id` | RunId. Travels in every message and is the Cosmos partition key. |
| `JobId` [FK] | Which job. A real FK, because both tables are in the same database. |
| `ScheduledFor` | **The planned run time (UTC)**, not when the row was created. Together with JobId it identifies a run uniquely. |
| `Status` | Pending (claimed, message queued), Running, then **terminal**: Succeeded / Failed / TimedOut / LockFailed / Cancelled. |
| `ServiceBusSequenceNumber` | Service Bus returns this when a message is **scheduled** for later. It's the **only way to cancel** a scheduled message before delivery (`CancelScheduledMessageAsync(seq)`). Optional because it's filled after the outbox worker publishes. |
| `Deadline` | Latest time the run may finish. Used for timeouts and as the lock lease. |
| `StartedAt` / `FinishedAt` | Duration and history (UC 2.3). |
| `ErrorSummary` | Short failure reason for notifications and history. Full logs are in Cosmos. |
| `CreatedAt` | When the claim happened. |
| `UQ (JobId, ScheduledFor)` | **Idempotency of the claim**; see 4.6. |
| `IX (Status, Deadline)` | The **timeout sweeper** query: "runs still Pending or Running whose deadline has passed", which become TimedOut and release the lock. |

**LockFailed** means the scheduler tried to run a Singleton job while the previous run still held the lock.
The run is recorded, not silently skipped, so it appears in history and triggers a notification (UC 3.2),
matching the lesson 2 activity diagram.

### 4.4 `JobRunOutbox`: the same outbox pattern, for Service Bus
Same reason as in UC 1.1: "create the JobRun" and "publish to Service Bus" must not drift apart.
If the claim commits but publishing fails, the run would stay Pending forever. If publishing succeeds
but the claim rolls back, there would be a run nobody tracks.

| Column | Why |
|---|---|
| `RunId` [FK] | Which run the message is for. |
| `EventType` ExecuteJob / NotifyFailure | ExecuteJob goes to the execution queue. NotifyFailure goes to the notification queue (UC 3.2), covering both lock failures and run failures. |
| `Payload` | Message body: RunId, LaunchDetails, Deadline. Large content goes to Blob (claim-check), as in lesson 3. |
| `ScheduledEnqueueAt` | When Service Bus should deliver it, which is the run time. |
| `ProcessedAt`, `Attempts`, `IX (CreatedAt) WHERE ProcessedAt IS NULL` | Same as `JobOutbox`. |

### 4.5 `JobOutputEntry` (Cosmos DB `job-outputs`)
| Field | Why |
|---|---|
| `id` | Cosmos item id. |
| `RunId` (partition key) | All logs of one run live in one **logical partition**, so "show logs of run X" is a cheap single-partition query. It also spreads writes evenly across runs. |
| `Timestamp`, `Level`, `Message` | The log line itself. Lines are ordered by `Timestamp` within a run, so ties or small clock differences can reorder lines. |
| `ttl` | Cosmos deletes the item automatically after N seconds, so storage doesn't grow forever. |

**Why Cosmos and not SQL?** Bursty, high-volume, append-only writes from many parallel Runners;
reads always by RunId; built-in TTL. That pattern suits Cosmos well, and it keeps that load off the SQL databases.

**Consistency: Session.** Cosmos lets you choose consistency per account or request. Session means
**within one client session you always read your own writes** and see them in order. Other readers may lag
slightly. That fits here: a Runner reads back its own logs consistently, and a user viewing logs can
tolerate a second of lag. Strong consistency would cost more request units and latency for no benefit.
Eventual consistency could show a Runner out-of-order logs.

### 4.6 The UC 2.1 isolation note, line by line
> **READ COMMITTED (RCSI)**

The same default level as UC 1.1. Here the correctness doesn't come from the isolation level. It comes from
**explicit row locks and conditional UPDATEs** on the specific rows that matter. That's the usual
approach in SQL Server: keep the cheap default and lock exactly what needs protecting.

> **1. SELECT due snapshots WITH (UPDLOCK, READPAST)**: rows stay locked until commit, so parallel
> claimers skip them instead of double-claiming.

- Under RCSI a plain SELECT takes **no locks**, so two claimers would both see and claim the same job.
- `UPDLOCK` turns the read into a locking read. It takes an **update lock** on each row it returns, held
  **until the transaction ends**. Two update locks on the same row conflict, so only one transaction can hold it.
- `READPAST` means "skip rows that someone else has locked" instead of waiting. A second claimer
  silently moves on to other jobs. This is the standard **SQL Server queue pattern**.
- Sketch:
  ```sql
  BEGIN TRAN;
  SELECT TOP (@batch) JobId, ...
  FROM JobScheduleSnapshot WITH (UPDLOCK, READPAST, ROWLOCK)
  WHERE Status = 'Active' AND NextRunAt <= SYSUTCDATETIME()
  ORDER BY NextRunAt;
  -- steps 2-4 for each row, then COMMIT
  ```
- With a single leader, there's only one claimer today. These hints make it safe anyway: during
  leader failover two instances may briefly overlap, and the schedule may be sharded later.

> **2. Advance NextRunAt for every claimed job (OneTime → Status = Inactive).**

- Without this step, the job would still be due on the next tick and would be claimed again and again.
- It runs for **every** claimed job, whether Singleton or Concurrent, and whether or not the lock succeeds.
- Recurring: `NextRunAt = NextRunAt + step`. Add the step to the **previous scheduled time**, not to "now", so
  the schedule doesn't drift. OneTime: there's no next run, so set Status = Inactive.

> **3. Singleton only: UPDATE ... SET CurrentRunId, LockedUntil WHERE CurrentRunId IS NULL OR LockedUntil < now;
> 0 rows → LockFailed.**

```sql
UPDATE JobScheduleSnapshot
SET CurrentRunId = @RunId, LockedUntil = @Deadline
WHERE JobId = @JobId
  AND (CurrentRunId IS NULL OR LockedUntil < SYSUTCDATETIME());
-- @@ROWCOUNT = 1 -> lock acquired; 0 -> previous run still holds it -> JobRun.Status = LockFailed
```
- The condition means "free, or the previous holder's lease has expired".
- An UPDATE always locks the row it changes and evaluates WHERE against the **latest committed** row,
  even under RCSI. A conditional UPDATE is therefore an atomic **compare-and-set**, and two transactions
  can never both get `@@ROWCOUNT = 1`.

> **4. INSERT JobRun (Pending) + JobRunOutbox (ExecuteJob).** Same transaction, so it's the outbox pattern.

> **UQ (JobId, ScheduledFor) makes the claim idempotent.**

If the same (job, time) is claimed twice for any reason, such as a retry after a crash or two leaders during
failover, the second INSERT fails on the unique key and is treated as "already claimed". This is the
**last line of defence** against double execution, independent of all the locking above.

> **Status updates: compare-and-set on the expected status; terminal states are never overwritten.**

```sql
UPDATE JobRun SET Status = 'Succeeded', FinishedAt = @t
WHERE Id = @RunId AND Status IN ('Pending', 'Running');
```
- Service Bus delivers **at least once**, so a status message may arrive twice, late, or after the timeout
  sweeper already marked the run TimedOut.
- Applying a status only from the expected previous state makes duplicates harmless (0 rows updated), and a
  final state can't be changed once set.

> **Lock release: ... WHERE CurrentRunId = @RunId (only the owning run).**

```sql
UPDATE JobScheduleSnapshot SET CurrentRunId = NULL, LockedUntil = NULL
WHERE JobId = @JobId AND CurrentRunId = @RunId;
```
Scenario this prevents:
1. Run A's lease expires and run B takes the lock.
2. A's late "finished" message arrives.
3. Without the `CurrentRunId = @RunId` check, A would release **B's** lock, and a third run could overlap with B.

> **Job edited/deleted: the Orchestrator cancels the scheduled message by ServiceBusSequenceNumber,
> marks the run Cancelled, releases the lock.**

- Runs are queued **ahead of time** as scheduled Service Bus messages. If the user changes or deletes the job,
  that queued message describes the old job.
- **Runners never read the Orchestrator database**, so they can't check whether the job changed. Only the
  Orchestrator can act, and the only way to stop a scheduled message is to cancel it by its sequence number.
- Steps: cancel the message, then set JobRun = Cancelled (compare-and-set), then release the lock (owner check).
  The next run is then claimed from the new snapshot.

> **Serializable is not needed: every conflict is on a single locked row.**

Every race in this flow is about **one specific row**: one snapshot (claim and lock) or one JobRun (status).
Row locks and conditional UPDATEs handle these exactly. Serializable protects **ranges** against phantom inserts,
which we don't rely on. It would add range locks on the busiest table and cause blocking and deadlocks.

---

## 5. Isolation levels refresher (as applied here)

| Level | What it prevents | Cost | Used here? |
|---|---|---|---|
| Read Uncommitted | nothing | none | ❌ Could read a JobRun that gets rolled back |
| Read Committed (locking) | dirty reads | readers wait for writers | ❌ Not the Azure SQL default |
| **Read Committed Snapshot (RCSI)** | dirty reads; readers never block | tempdb / row-version store | ✅ **Default for both use cases** |
| Repeatable Read | + non-repeatable reads | holds read locks until commit | ❌ Not needed |
| Snapshot | + consistent view of the whole transaction; update conflicts raise errors | version store + retry logic | ❌ No multi-read consistency needed |
| Serializable | + phantoms | range locks, deadlocks | ❌ Conflicts are single-row |

**Key idea to remember:** the isolation level is the **default** behaviour for a whole transaction.
For specific hot spots we add **targeted** protection instead of raising the level for everything:
- unique constraints
- `UPDLOCK` / `READPAST` hints
- conditional UPDATEs (compare-and-set)
- version checks

That gives correctness where it matters and RCSI's low blocking everywhere else.

---

## 6. Patterns used (cheat sheet)

| Pattern | Where | Problem it solves |
|---|---|---|
| Database per service | 3 databases | Services deploy, scale and fail independently |
| Denormalized snapshot | `JobScheduleSnapshot` | The scheduler works without Job Manager and has fast local queries |
| Transactional outbox | `JobOutbox`, `JobRunOutbox` | DB write + message send that can't be made atomic directly |
| Idempotent receiver | Snapshot upsert, `UQ (JobId, ScheduledFor)`, status compare-and-set | At-least-once delivery means duplicates |
| Optimistic versioning | `Version`, `JobVersion` | Out-of-order delivery of edits |
| Lease lock | `CurrentRunId` + `LockedUntil` | Singleton jobs, and recovering from crashed Runners |
| Owner-checked release | `WHERE CurrentRunId = @RunId` | A late message releasing someone else's lock |
| Queue claim with `UPDLOCK, READPAST` | Due-jobs SELECT | Parallel claimers without double work or blocking |
| Soft delete | `Status = Deleted` | Deletes that still need an outbox event and history |
| Filtered indexes | Due jobs, pending outbox, unique name | Small, fast indexes for hot queries |
| TTL | Cosmos `ttl` | Automatic cleanup of old logs |

---

## 7. Known limitations and things deliberately left out

These were considered and judged as polish rather than gaps. Be ready to discuss them.

- **ENUM types:** in a real schema, use `TINYINT` + `CHECK`, or lookup tables. Filtered indexes must then compare
  to the literal value, e.g. `WHERE Status = 1`.
- **GUID clustered keys:** random GUIDs fragment clustered indexes on insert-heavy tables (`JobRun`, the outboxes).
  Fix with `NEWSEQUENTIALID()`, sequential GUIDs generated in the application, or a `BIGINT IDENTITY` clustered key with the GUID as a non-clustered unique key.
- **Secrets:** `Headers` / `LaunchDetails` may contain API tokens, stored in plain text in two databases.
  Store Key Vault references instead, or encrypt the columns (Always Encrypted).
- **Time zones:** everything is UTC. "Every day at 09:00 local time" across a DST change would need a
  `TimeZoneId` column and local-time schedule maths.
- **Who sets `Running` / `StartedAt`?** The lesson 2 activity diagram has the Runner publish only the **final** status.
  Either add a "started" status event or fill `StartedAt` from the final message.
- **Test runs / trigger type:** lesson 3 mentions test runs published with no delay. A `TriggerType` (Scheduled / Test)
  column would separate them, and a test run could otherwise collide with `UQ (JobId, ScheduledFor)`.
- **`JobRun` has no `UserId`:** history and notifications get it by joining to the snapshot. Denormalizing
  it would make history queries per user cheaper.
- **Cosmos lookups by job:** `JobOutputEntry` has no `JobId`, so "all logs of a job" goes through `JobRun` first.
- **Cancellation races:**
  - If a job is edited **before** the outbox worker has published the message, there's no sequence number yet.
    Cancel by marking the `JobRunOutbox` row processed, so it's never sent.
  - If the message was **already delivered**, cancelling fails and that one run executes with the old definition.
    That's acceptable with at-least-once semantics, but worth knowing.
- **Missed runs:** if the scheduler was down for an hour, should a "every 5 minutes" job run 12 times or once?
  The usual policy is to run once, then jump `NextRunAt` to the next future slot.
- **Lease length:** `LockedUntil = Deadline` assumes runs finish before their deadline. Very long runs would need to
  extend the lease (a heartbeat).
- **Outbox cleanup:** processed outbox rows should be deleted or archived after N days.
- **Growth of `JobRun`:** Hyperscale grows automatically. Lesson 3 also plans date-based partitioning and archiving.

---

## 8. Likely reviewer questions (with short answers)

- **Why READ COMMITTED and not Serializable for job execution?** Every conflict is on one row, and row locks plus
  conditional UPDATEs solve it precisely. Serializable would lock ranges on the hottest table and cause deadlocks.
- **Doesn't RCSI break the unique name check?** No. RCSI changes reads only. Unique checks on INSERT still lock.
- **How do you prevent a job running twice?** There are three layers:
  1. `UPDLOCK, READPAST` on the claim
  2. `UQ (JobId, ScheduledFor)`
  3. compare-and-set status updates for duplicate messages
- **What if the Runner crashes?** No status arrives, so the timeout sweeper (`IX (Status, Deadline)`) marks the run TimedOut,
  and the lease (`LockedUntil`) has expired anyway, so the next run can take the lock.
- **What if the Orchestrator is down when a job is created?** The job is safely in `job-details` with an outbox row,
  and it's synced once the Orchestrator is back.
- **Why no foreign key between `Job` and `JobScheduleSnapshot`?** They're in different databases owned by different services.
  Consistency comes from the outbox plus versioning, which is eventual consistency.
- **Why is `NextRunAt` stored and not calculated?** So the due-jobs query is a simple indexed range scan.
- **Why is the Cosmos partition key `RunId`?** Reads are always "logs of run X", which makes them single-partition. Writes spread
  across many runs, so there's no hot partition. One run's logs stay well below the 20 GB limit of a logical partition.
- **Why Session consistency?** Read-your-own-writes for the Runner, at lower cost than Strong.
