# JobScheduler.Dal.IntegrationTests

Integration tests of the DAL against the **real Azure SQL databases** from the Portal guide ([`../../infra/azure-sql/README.md`](../../infra/azure-sql/README.md)). They cover read/write routing, replica write rejection, replication lag, both repositories and the claim algorithm. Without configuration every test is **skipped**, not failed.

## Configure
The tests read three keys (user-secrets of this project, or environment variables). Exact values are in step 9 of the Portal guide.

| User-secrets key | Environment variable | Required |
|---|---|---|
| `JobSchedulerDal:JobDetails:ConnectionString` | `JobSchedulerDal__JobDetails__ConnectionString` | yes |
| `JobSchedulerDal:JobOrchestrator:ConnectionString` | `JobSchedulerDal__JobOrchestrator__ConnectionString` | yes |
| `JobSchedulerDal:JobOrchestrator:ReportingDatabase` | `JobSchedulerDal__JobOrchestrator__ReportingDatabase` | named-replica tests only |

If either primary connection string is missing, all tests skip. If the optional `ReportingDatabase` key is missing, only the named-replica tests skip.

Authentication is your Entra identity (`az login`); there are no passwords.

## Run
From `job-scheduler/`:
```bash
dotnet test                                                          # everything
dotnet test -- --filter-class "JobScheduler.Dal.IntegrationTests.*"  # integration tests only
```
`RetryTests` (`Retry_*`) force the "commit succeeded, acknowledgement lost" case with a test-only `CommitThenFailConnectionFactory` that fails attempt 1 after it committed, so attempt 2 runs the repository's real reconciliation code. The decorator uses its own zero-delay retry options, so these tests need no waiting.

Replication-lag and routing tests write evidence lines (server names, `Updateability`, lag in ms) to the test output; use `--output Detailed` to see them.

## Cost and cleanup
The tests add no resources and no cost beyond the databases that are already running (≈ $2.03/h idle). Each test registers the job ids it creates and deletes its rows in `DisposeAsync` (children first, on the primaries). Claims use synthetic timestamps in 2001–2002, so they never touch the seed rows (due in 2099) or real data. Remember to tear the resource group down afterwards (guide step 10).
