
## Databases ACID and Transaction Isolation Levels

The goal of this task is to demonstrate transaction isolation levels in SQL Server using Docker Compose on Windows and to design database schema diagrams for specific use-cases.

---

## Home Task

**Prerequisites:**

- Docker CLI (or Rancher Desktop) installed
- SQL Server client (SSMS or Azure Data Studio)

### 1. Check Transaction Isolation Levels In Practice

**1) Launch SQL Server using Docker Compose**

<details>
  <summary><i>Expand to find the detailed instructions how to launch 'ms-sql-demo' project in a Docker container.</i></summary>

**Project Structure:**

```
ms-sql-demo/
├── Dockerfile
├── docker-compose.yml
├── entrypoint.sh
├── init-db/
│   └── init-demo.sql
└── scripts-demo/
    ├── *.sql
    └── README.md
```

**1. Start the SQL Server container** by running the following scripts:

```bash
docker-compose build
docker-compose up -d
```

**2. Connect to the launched SQL Server:**

**IMPORTANT!** Ensure you don't have any other containers running on the same port.

- **Server**: `localhost,1433`
- **Authentication**: SQL Server Auth
- **Database**: `IsolationDemo` (created at startup)
- **User**: `sa`
- **Password**: `YourStrongPassword123`

</details>

---

**2) Demo Scripts for Isolation Levels**

Navigate to [scripts-demo](https://git.epam.com/epmc-msft/net-mentoring-programs/-/tree/main/advanced+/04_01_distributed_data_acid_and_cap_theorem/ms-sql-demo/scripts-demo?ref_type=heads) folder. Use the following script to play around with transaction isolation levels to find out their behavior in particular scenario.
For each scenario leverage script pair in **two sessions** (A & B) to observe transaction behavior.
Below, for 'Dirty Read' scenario -> `read_uncommitted_dirty_read.sql` (Session A), `read_uncommitted_dirty_read_check.sql` (Session B). Follow this approach for the rest of scenarios.

| Scenario            | Description                          | Files                                                        |
|---------------------|--------------------------------------|--------------------------------------------------------------|
| Dirty Read          | Read uncommitted changes             | `read_uncommitted_dirty_read.sql`, `read_uncommitted_dirty_read_check.sql` |
| Non-repeatable Read | Read changed data in same transaction| `read_committed_nonrepeatable_read.sql`                      |
| Prevent Non-repeatable | Block updates during read          | `repeatable_read_protection.sql`                             |
| Phantom Read        | New rows appear between reads        | `phantom_read_repeatable_read.sql`                           |
| Prevent Phantom     | Block inserts in range               | `phantom_read_serializable.sql`                              |
| Snapshot Isolation  | Read consistent snapshot             | `snapshot_isolation.sql`                                     |

**Tips:**

- Use `BEGIN TRAN`, `COMMIT`, `ROLLBACK` to control boundaries
- Use `WAITFOR DELAY '00:00:10'` to simulate timing
- View `demo/README.md` for script explanations
- Explore locking with Activity Monitor or Profiler

---

**Expected Isolation Levels Summary Table**

| Isolation Level    | Dirty Read | Non-repeatable Read | Phantom Read |
|--------------------|------------|--------------------|--------------|
| Read Uncommitted   | ✅ Yes     | ✅ Yes             | ✅ Yes       |
| Read Committed     | ❌ No      | ✅ Yes             | ✅ Yes       |
| Repeatable Read    | ❌ No      | ❌ No              | ✅ Yes       |
| Serializable       | ❌ No      | ❌ No              | ❌ No        |
| Snapshot           | ❌ No      | ❌ No              | ❌ No        |

### 2. Create Database Schema Design diagrams

As a preparatory step for the next module, create **Database Schema Design** diagrams for the following use-cases:

- `UC 1.1: Create a New Job`
- `UC 2.1: Execute a Job At a Scheduled Time`

For each use-case:
- Show the data models with their structure
- Include primary keys and properties with expected data types
- Indicate cross-model relationships if applicable
- **Consider what Isolation Level is preferable for this use-case**

Refer to the example for `UC 2.3: View Job Execution History`: [Database Schema Design Example](./../04_01_distributed_data_acid_and_cap_theorem/diagrams/UC2.3_ViewJobExecutionHistory_DbSchemaDesign.png).

---

### Scoreboard Criteria:

- **1-50 points:** Demo Scripts for Isolation Levels was completed and demoed (recording or on a session)
- **51-100 points:** Database Schema Design diagrams are created with appropriate quality and detail.