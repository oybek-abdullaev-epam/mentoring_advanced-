# Home Task: Measure System Scalability with Load Testing

**Module**: 01 – Understanding ASR of a High Load System  
**Topic**: Turning NFR theory into measurable metrics  

---

## Overview

This lab uses NBomber (a .NET load testing framework) to send HTTP requests to an .NET server at increasing concurrency levels. You will observe how response time and throughput change as the system approaches its limits — connecting the theory of Scalability, Availability, and Reliability to real numbers.

---

## Prerequisites

- Docker Desktop is not free, so please consider the usage of [Docker CLI](https://www.docker.com/products/cli/)(without desktop) or [Podman Desktop](https://podman-desktop.io/downloads/windows). It has an integration with docker containers
- 4 GB RAM available for containers
- [Understanding of latency metrics](https://medium.com/tuanhdotnet/statistics-behind-latency-metrics-understanding-p90-p95-and-p99-dc87420d505d)

---

## Setup & Execution

```bash
cd 01-load-testing

# Start the server and run the load test (takes ~4 minutes)
docker-compose up --build

# Metrics will be written to:
#   01-load-testing/metrics/load-test-results.csv
```

The compose file starts:
1. **Server** – .NET 8 app server exposing `GET /api/data?size=100`
2. **Load Test Runner** – NBomber running 5 scenarios (1 → 10 → 50 → 100 → 500 concurrent users)

**Note:** Make sure that containers are started via Podman desktop

To run the server only (for manual testing):
```bash
docker-compose up server
curl http://localhost:5000/api/health
curl "http://localhost:5000/api/data?size=100"
```

---

## Interpreting Results

### Example Output

```
| Concurrent Users | Avg (ms) | p95 (ms) | p99 (ms) | RPS    | Errors |
|------------------|----------|----------|----------|--------|--------|
|                1 |      3.1 |      5.2 |      7.1 |  312.5 |      0 |
|                5 |      4.8 |      9.3 |     14.2 |  987.4 |      3 |
```

### What to look for
- Avg response time increasing steeply
- p95 >> Avg
- RPS plateau or drop
- Errors > 0

---

## Opened questions

1. **NFR Mapping** – Identify which metrics from your results of load tests execution are related to the following NFRs:
   - **Scalability**
   - **Availability**
   - **Reliability**
2. **Design recommendation** – Assume that, there is a real system under load of 500 concurrent users (almost like in sample above with Server and Test Runner), what design proposal or change you could recommend?

---

## Troubleshooting

| Problem | Solution |
|---------|----------|
| `docker-compose up` fails | Verify Docker Desktop is running; try `docker system prune -f` |
| Server unhealthy | Check `docker logs load-test-server`; ensure port 5000 is free |
| Metrics directory empty | Check `docker logs load-test-runner`; ensure server was healthy before tests started |

---

## Scoring Guide

| Score | Criteria |
|-------|----------|
| 1-59 | Self-check answers are provided |
| 60–89 | Lab attempted; metrics table is collected |
| 90–100 | NFR mapping completed and design recommendation is provided with explanation |
