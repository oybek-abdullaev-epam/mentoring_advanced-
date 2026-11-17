# job-scheduler

## Purpose
Shared code for the **Online Job Scheduler** used in the mentoring program. It is reused across lessons: each lesson extends the same solution instead of starting a new one. Lesson folders hold only documents and evidence.

## Structure
```
job-scheduler/
  README.md
  .gitignore
  global.json                  # SDK pin + Microsoft.Testing.Platform runner
  Directory.Build.props
  Directory.Packages.props     # central package versions
  JobScheduler.slnx
  db/
    job-details/               # 001_schema.sql, 002_seed.sql
    job-orchestrator/          # 001_schema.sql, 002_seed.sql
    verify-replication.sql
    verify-replica-is-readonly.sql
  infra/azure-sql/             # README.md — step-by-step Azure Portal setup guide (manual, no IaC)
  src/JobScheduler.Dal/        # Connections, Common, Contracts, JobDetails, JobOrchestrator, DependencyInjection
  tests/JobScheduler.Dal.UnitTests/
  tests/JobScheduler.Dal.IntegrationTests/
```

## Prerequisites
- .NET 10 SDK (see `global.json`)
- Azure CLI: `az` (login + read-only checks; resources are created by hand in the Portal)
- go-sqlcmd: `brew install sqlcmd`

## Build & test
Run from this folder:
```bash
dotnet build
dotnet test
# filter (xUnit v3 on Microsoft Testing Platform)
dotnet test -- --filter-class "JobScheduler.Dal.UnitTests.*"
```
Integration tests report **skipped** (not failed) when Azure SQL is not configured.

## Transient-error retry
Every repository method runs through `ISqlConnectionFactory.ExecuteAsync`, which retries transient SQL errors (failover, reconfiguration, replica query termination, deadlock) on a new connection with a [Polly](https://github.com/App-vNext/Polly) exponential backoff. Tune it under `JobSchedulerDal:Retry`:

| Key | Env var | Default |
|---|---|---|
| `JobSchedulerDal:Retry:MaxAttempts` | `JobSchedulerDal__Retry__MaxAttempts` | `5` (`1` disables retry) |
| `JobSchedulerDal:Retry:FirstDelay` | `JobSchedulerDal__Retry__FirstDelay` | `00:00:05` |
| `JobSchedulerDal:Retry:MaxDelay` | `JobSchedulerDal__Retry__MaxDelay` | `00:01:00` |

Each retry logs a Warning with EventId 1002. Writes whose commit outcome is unknown after a dropped connection are reconciled per method (see [§9.3 Retries](../04_02_distributed_data_replication/03-home-task-solution.md#93-retries)).

## Lessons that touched this
| Lesson | What changed | Solution document |
|---|---|---|
| 4.2 Replication | DAL with read/write splitting; job-details Business Critical readable secondary, job-orchestrator Hyperscale HA + named replica | [../04_02_distributed_data_replication/03-home-task-solution.md](../04_02_distributed_data_replication/03-home-task-solution.md) |
