### Home Task: Observability Design for Long-Running and Failed Background Tasks

#### Task Overview
The goal of this home task is to design an observability setup for a high-load system that executes background or asynchronous tasks (e.g., workers, Functions, Lambdas, or message consumers).
You are expected to define and configure **custom metrics**, **alerts**, and **dashboards** that allow engineering teams to monitor system health, detect performance degradation, and respond to incidents in a timely manner.

**Scope:** You are monitoring the **Job Runner** — the component that consumes jobs from the queue and executes them. If you prefer to focus on the orchestration layer, you may use the **Job Orchestrator** instead, but note that execution-duration and CPU metrics apply primarily to the Job Runner. API gateways, databases, and other upstream or downstream dependencies are out of scope for this task.

The task aligns with typical Non-Functional Requirements (NFRs) such as:
- Reliability
- Operability
- Monitoring and alerting
- Time to detect and recover from incidents

### Notes About Scalability and High-Load Considerations

- **Metric Cardinality**
  - Avoid unbounded dimensions (e.g., taskId, userId) to prevent metric explosion.
- **Efficient Aggregation**
  - Prefer counters, histograms, and server-side aggregation over emitting excessive metric events.
- **Alert Stability**
  - Alerts should rely on stable, low-cardinality metrics; dashboards may include more detailed breakdowns.
- **Telemetry Resilience**
  - Metrics and logging should be non-blocking and must not affect task execution.

---

### Steps to Complete the Task

#### 1. Configure Dashboards and Alerts

##### Alerts
Use built-in metrics like the following:
- CPU, Memory, Requests Count (# of executions for lambda), Request Time
- Task execution duration percentiles (p95/p99), Retry count or DLQ size (if applicable)

Create alerts for the metrics above. Trigger an alert in case if CPU, Memory or Requests Count thresholds are exceeded (**Sample**: CPU usage >= 70%)

Each alert should define:
- Evaluation window
- Severity level
- Notification channel (email, Slack, Teams, etc.)
- Short operational hint (what to check first)

---

#### 2. Define and Create Custom Metrics
Select a monitoring stack based on **AWS, Azure, or Grafana (or a combination)** and instrument a background task processing component.

Create **at least two** of the following custom metrics and add them to the dashboard:
- **LongRunningTasksCount**
  - Number of tasks whose execution time exceeds a defined threshold (e.g., >30s or >1 minute).
- **FailedTasksCount**
  - Number of tasks that ended with an error or were not successfully completed.
- **TasksProcessedPerHour**
  - Throughput metric showing how many tasks were processed per hour.

In addition, configure an **alert for failed tasks** (absolute count or failure rate threshold).

---

### Scoreboard Criteria

1. **1–59:** Self-check questions answers were provided (the more questions answered the higher the grade).
2. **60–89:** A dashboard is created with built-in metrics (CPU, Memory, Requests Count, Request Time). At least one alert is configured with an evaluation window, severity level, notification channel, and operational hint.
3. **90–100:** At least two of the three custom metrics are implemented, added to the dashboard, and an alert for failed tasks (absolute count or failure rate) is configured.