## Distributed Data and Replication

The goal of this task is to design a replication strategy for system components and implement a Data Access Layer with replication support. You will analyze system requirements, choose appropriate databases, design replication configurations, and implement read/write splitting between replicas.

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

### 1. Replication Strategy Decision

For each use case, prepare a short document that describes the following aspects for each system component/service:

**1) Formulate system component requirements:**
- Expected data volume (number of records, data size in GB)
- Expected load (read/write requests per second)
- Consistency requirements (strong or eventual consistency)
- Availability requirements (uptime expectations, acceptable downtime)
- Geographic distribution (single region or multi-region deployment)

**2) Select the most suitable database and justify your choice:**
- Which database type is most appropriate: SQL or NoSQL?
- What are the advantages and disadvantages of the selected database given these requirements?
- Deployment approach: self-hosted or cloud service?

**3) Design the replication strategy for the selected database:**
- Justify why replication is required for this system component
- Describe the replication strategy:
  - For cloud service databases: Identify the default replication strategy and how you will configure it
  - For self-hosted databases: Specify which replication strategy you will implement (leader-follower, multi-leader, or leaderless)
- Define the replication configuration parameters (number of replicas, synchronous/asynchronous mode, quorum settings if applicable)

### 2. Setup Database with Replication

Setup database instance(s) with replication configured based on the Replication Strategy Decision and Database Schema Design:
- Attach configuration document or a set-up screenshot to the DAL project.
- Do a minimal data seeding (1-5 records per table) to check replication;

### 3. Implement Data Access Layer (DAL)

**Implement a Data Access Layer** that demonstrates replication-aware data access patterns:

- Use the requirements and database selections from the 'Replication Strategy Decision' section
- Develop the required CRUD operations for the specified use cases
- **Implement read/write splitting**: route read operations to read-only replicas and write operations to read-write replicas

---

### Scoreboard Criteria:
- **1-30 points:** Replication Strategy Decision document is complete with requirements analysis, database justification, and replication design.
- **31-65 points:** Database is setup with replication configured, replication is verified, and minimal test data is seeded.
- **65-100 points:** Data Access Layer is implemented with proper CRUD operations and read/write replica routing.