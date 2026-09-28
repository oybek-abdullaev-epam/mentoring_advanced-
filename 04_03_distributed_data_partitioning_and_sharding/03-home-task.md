## Distributed Data: Partitioning and Sharding

The goal of this task is to design a partitioning/sharding strategy for system components and enhance the Data Access Layer implementation from *'Module 04_02'* with partition/shard-aware data routing logic.

---

## Home Task

**Prerequisites** (choose one of the following options):

1. Containerization Tools
- [Rancher Desktop](https://rancherdesktop.io/)
- Docker CLI (*Note: Docker Desktop requires a paid subscription*)
  - [How to use Docker without Docker Desktop on Windows 10 or 11](https://blog.novacare.no/how-to-use-docker-without-docker-desktop-on-windows-10-or-11/)
  - [Installing Docker on Windows without Docker Desktop](https://gist.github.com/rwcitek/f2b6fb5baddf16f6dc1f277a5d9f8366)

2. Cloud Subscription
- [AWS Free Tier](https://aws.amazon.com/free/)
- [Azure EPAM Subscription](https://kb.epam.com/spaces/EMSFTCC/pages/1333374937/Software+Packages+with+Azure+Subscriptions+50)

**Scope of work:**
- `UC 1.1: Create a New Job`
- `UC 2.1: Execute a Job At a Scheduled Time`

Please refer to [Online Job Scheduler Domain Entities](https://git.epam.com/epmc-msft/net-mentoring-programs/-/blob/main/advanced+/02_practical_cases_domain_intro/01-self-study-materials.md?ref_type=heads#3-online-job-scheduler-domain-entities) and [Conceptual Solution Components Diagram](https://git.epam.com/epmc-msft/net-mentoring-programs/-/blob/main/advanced+/02_practical_cases_domain_intro/01-self-study-materials.md?ref_type=heads#5-conceptual-solution-components-diagram:~:text=5.%20Conceptual%20Solution%20Components%20Diagram).

---

### 1. Partitioning/Sharding Strategy Decision

Prepare a document that describes your partitioning/sharding strategy:

**1) Analyze data growth and access patterns:**
- Expected data growth rate (records per day/month)
- Query patterns (time-based queries, user-based queries, etc.)

**2) Decide on partitioning vs sharding approach and justify:**
- Should you use **partitioning** (single database instance) or **sharding** (multiple database instances)?
- For sharding: what number of shards is required?
- What partitioning/sharding strategy will you choose for your database? (range-based, hash-based, list-based, etc.)
- What partition/shard key will you choose?

### 2. Apply Partitioning/Sharding Configuration

Apply the partitioning/sharding configuration to the database set up in *'Module 04_02'*:
- Implement the selected partitioning/sharding strategy in the database
- Attach configuration document or a set-up screenshot to the DAL project
- Adjust data seeding to include sufficient test data (50-100+ records) to demonstrate data distribution across partitions/shards

### 3. Update Data Access Layer Implementation

Update your Data Access Layer implementation from *'Module 04_02'* to support partitioning/sharding. Add partition/shard key to CRUD operations where applicable. Add Debug logs to track which partition/shard was used for a CRUD operation.

---

### Scoreboard Criteria:
- **1-30 points:** Partitioning/Sharding Strategy Decision document is complete with data growth analysis, partitioning vs sharding justification, strategy selection, and partition/shard key definition.
- **31-60 points:** Partitioning/sharding configuration is applied to the database with documented implementation steps.
- **61-100 points:** Data Access Layer is updated with partition/shard-aware logic, demonstrates proper query routing, and works correctly with the partitioned/sharded database.