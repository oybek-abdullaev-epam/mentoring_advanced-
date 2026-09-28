# Self-Study Materials

> **Total reading time: ~75 minutes**

## Observability Fundamentals

1. [Logs vs Metrics vs Traces (reading, 5 minutes)](https://microsoft.github.io/code-with-engineering-playbook/observability/log-vs-metric-vs-trace/)

2. [Observability Primer — OpenTelemetry (reading, 25 minutes)](https://opentelemetry.io/docs/concepts/observability-primer)

## High-Performance Logging in .NET

3. [Best Practices for High-Performance Logging in .NET (reading, 20 minutes)](https://learn.microsoft.com/en-us/dotnet/core/extensions/high-performance-logging)

## Distributed Systems Observability

4. [Correlation IDs in Distributed Systems (reading, 5 minutes)](https://microsoft.github.io/code-with-engineering-playbook/observability/correlation-id/)

5. [Service Level Objectives — Google SRE Book (reading, 20 minutes)](https://sre.google/sre-book/service-level-objectives/)

6. [Distributed Tracing in .NET: .NET 6 — Web API Correlation Id](https://mohamadlawand.medium.com/net-6-web-api-correlation-id-aeb9e646269b) (reading, 3 min) — Covers: Correlation ID middleware, Request tracing across distributed transactions, ICorrelationIdGenerator interface, Header propagation

## Sampling at High Load

At high throughput you cannot afford to log or trace every operation. These resources cover how to make principled decisions about what to keep.

7. [Sampling Concepts: Head-Based vs Tail-Based — OpenTelemetry (reading, ~10 minutes)](https://opentelemetry.io/docs/concepts/sampling/)
    *Covers when each strategy is appropriate, the tradeoffs, and how the OTel Collector enables tail sampling.*

8. [Sampling in Application Insights with OpenTelemetry — Microsoft Learn (reading, ~10 minutes)](https://learn.microsoft.com/en-us/azure/azure-monitor/app/opentelemetry-sampling)
    *Covers fixed-rate and rate-limited sampling in the Azure Monitor OTel distro for .NET, plus a KQL query to verify sampling is active.*

---

## Trace Context in Async and Message-Driven Systems

9. [End-to-End Tracing with Azure Service Bus — Microsoft Learn (reading, ~15 minutes)](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-end-to-end-tracing)
    *Shows how the Service Bus .NET SDK propagates `Diagnostic-Id` (W3C `traceparent`) across producers and consumers, with C# code samples for both auto-instrumentation and manual correlation.*

---

## Log Schema Design

10. [OpenTelemetry Semantic Conventions (reference)](https://opentelemetry.io/docs/specs/semconv/)
    *The canonical specification for attribute names across logs, metrics, and traces. Essential when designing structured log schemas that need to be queryable and consistent across services.*

---

## Telemetry Pipeline Resilience

11. [OpenTelemetry Collector Resiliency — Official Docs (reading, ~10 minutes)](https://opentelemetry.io/docs/collector/resiliency/)
    *Covers sending queues, retry with exponential backoff, persistent write-ahead log (WAL) storage, and the agent-gateway deployment pattern for handling load spikes and backend outages without data loss.*

---

## Cost of Observability

12. [Cost Optimization in Azure Monitor — Microsoft Learn (reading, ~15 minutes)](https://learn.microsoft.com/en-us/azure/azure-monitor/fundamentals/best-practices-cost)
    *Covers commitment tiers, data retention tiers, Basic Logs tables, sampling, diagnostic settings filtering, and daily caps — all directly applicable when telemetry volume grows with system load.*

---

## PII and Sensitive Data in Logs

13. [Log Redaction in OpenTelemetry .NET — Official Docs (reading, ~10 minutes)](https://opentelemetry.io/docs/languages/dotnet/logs/redaction/)
    *Demonstrates building a custom processor to scrub sensitive attributes (credit card numbers, emails, API keys) before telemetry leaves the process.*

---

## Advanced Alerting: Multi-Burn-Rate SLOs

14. [Alerting on SLOs — Google SRE Workbook, Chapter 5 (reading, ~20 minutes)](https://sre.google/workbook/alerting-on-slos/)
    *Walks through all six alerting approaches culminating in the recommended multiwindow, multi-burn-rate alert. Includes PromQL examples and the rationale for the 2%/1h and 5%/6h fast/slow burn thresholds.*

---

## Production Diagnostics in Practice

15. [Production Grade Observability with Azure Monitor Application Insights — .NET Aspire Developers Day 2024 (video, ~24 minutes)](https://learn.microsoft.com/en-us/shows/dotnet-aspire-developers-day-2024/production-grade-observability-with-azure-monitor-application-insights)
    *Live demo starting at 07:32 showing transition from dev-time Aspire telemetry to production Application Insights: traces, failures blade, dependency maps, and the investigation workflow.*

- [Production Diagnostics in Practice](./02-optional-materials.md#production-diagnostics-in-practice) to see how a senior engineer investigates incidents end-to-end in a real monitoring tool
