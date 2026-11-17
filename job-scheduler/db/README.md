# db — SQL scripts

T-SQL for the two Azure SQL databases — `job-details` (Business Critical, built-in readable secondary) and `job-orchestrator` (Hyperscale serverless) — plus replica verification scripts.

## Order

Per database run `001_schema.sql` first, then `002_seed.sql`:

1. `job-details/001_schema.sql` → `job-details/002_seed.sql`
2. `job-orchestrator/001_schema.sql` → `job-orchestrator/002_seed.sql`

The two databases are independent (no cross-database FKs), so the pairs can run in any order relative to each other.

## Idempotency

Every script can be re-run without error and without changing data:

- Each `CREATE TABLE` is wrapped in `IF OBJECT_ID(N'dbo.X', N'U') IS NULL`; each index in `IF NOT EXISTS (… sys.indexes …)`.
- Each seed `INSERT` is guarded by `IF NOT EXISTS (SELECT 1 FROM dbo.X WHERE Id = …)` (`JobId` for 1:1 tables), using fixed GUIDs.
- Every script starts with `SET ANSI_NULLS ON; SET QUOTED_IDENTIFIER ON; SET NOCOUNT ON;` (required by the filtered indexes).
- RCSI is on by default in Azure SQL Database; the scripts do not toggle it.

## Expected row counts after seed

| Database | Table | Rows |
|---|---|---|
| `job-details` | `Job` | 3 |
| | `JobSchedule` | 3 |
| | `JobLaunchDetails` | 3 |
| | `JobOutbox` | 3 |
| `job-orchestrator` | `JobScheduleSnapshot` | 3 |
| | `JobRun` | 2 |
| | `JobRunOutbox` | 2 |

Each seed script ends with a `SELECT` of these counts. Seed `NextRunAt`/`StartAt` is `2099-01-01`, so integration-test claims never pick seed rows.

## How they are applied

You run them by hand with go-`sqlcmd` (Entra auth, no passwords) against the **primary** of each database, in the order above — see steps 6–7 of the [Portal setup guide](../infra/azure-sql/README.md). From `job-scheduler/`:

```bash
sqlcmd -S "tcp:$EUS_FQDN,1433" -d job-details      --authentication-method ActiveDirectoryDefault -b -i "db/job-details/001_schema.sql"
sqlcmd -S "tcp:$EUS_FQDN,1433" -d job-details      --authentication-method ActiveDirectoryDefault -b -i "db/job-details/002_seed.sql"
sqlcmd -S "tcp:$EUS_FQDN,1433" -d job-orchestrator --authentication-method ActiveDirectoryDefault -b -i "db/job-orchestrator/001_schema.sql"
sqlcmd -S "tcp:$EUS_FQDN,1433" -d job-orchestrator --authentication-method ActiveDirectoryDefault -b -i "db/job-orchestrator/002_seed.sql"
```

`$EUS_FQDN` is the logical server (defined in the guide); without `-K ReadOnly` sqlcmd connects to the primary of each database.

## Which script runs against which target

| Script | Primaries (R/W) | job-details BC readable secondary (`-K ReadOnly`) | job-orchestrator HA replica (`-K ReadOnly`) | Named replica (`job-orchestrator-reporting`) |
|---|---|---|---|---|
| `*/001_schema.sql`, `*/002_seed.sql` | yes — only here | never (writes fail with 3906) | never | never |
| `verify-replication.sql` | yes (`READ_WRITE`) | yes (`READ_ONLY`) | yes (`READ_ONLY`) | yes (`READ_ONLY`) |
| `verify-replica-is-readonly.sql` | no (would report `UNEXPECTED`) | yes (expects error 3906) | yes (expects error 3906) | yes (expects error 3906) |

Seeded rows reach the replicas asynchronously — via asynchronous redo on the BC readable secondary (job-details) and Hyperscale log apply (job-orchestrator) — so replica row counts may lag the primary briefly.
