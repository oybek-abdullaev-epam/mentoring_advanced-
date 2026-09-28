# Optional Materials

## Observability System Design

1. [Observability Primer — Microsoft Well-Architected (reading, 25 minutes)](https://learn.microsoft.com/en-us/azure/well-architected/operational-excellence/observability)
   *Azure-specific architectural guidance on monitoring stack design, telemetry collection strategies, and alerting.*

2. [Distributed Tracing with OpenTelemetry (reading, 15 minutes)](https://opentelemetry.io/docs/concepts/signals/traces/)
   *Deep dive into spans, trace context, and propagation mechanics — go here after the OTel primer if you want to understand tracing at the implementation level.*

---

## APM and Monitoring Tools

3. [Application Performance Monitoring with Azure Application Insights (reading, 30 minutes)](https://learn.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview)
   *The primary APM tool for .NET on Azure: live metrics, distributed tracing, failure analysis, dependency maps.*

4. [Health Monitoring in .NET Microservices (reading, 20 minutes)](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/monitor-app-health)
   *.NET health check patterns for background services and microservices — relevant if you're implementing liveness/readiness probes.*

5. [ELK Stack Overview (reading, 15 minutes)](https://www.elastic.co/what-is/elk-stack)

6. [Prometheus Overview (reading, 10 minutes)](https://prometheus.io/docs/introduction/overview/)

7. [Prometheus Monitoring Guide — deep dive (reading, ~10 minutes)](https://www.tigera.io/learn/guides/prometheus-monitoring/)

8. [Grafana Introduction (reading, 10 minutes)](https://grafana.com/docs/grafana/latest/introduction/)

9. [Grafana Cloud Metrics Monitoring — deep dive (reading, ~7 minutes)](https://grafana.com/blog/2022/09/16/grafana-cloud-metrics-a-guide-to-what-metrics-to-monitor-and-best-practices/)

10. [New Relic Introduction (reading, ~5 minutes)](https://newrelic.com/platform)
    > **Note:** The New Relic docs site requires JavaScript to render. If the link above does not load, use this alternative: [What is New Relic? (docs)](https://docs.newrelic.com/docs/new-relic-solutions/get-started/intro-new-relic/)

11. [AWS X-Ray: Guide for AWS Solutions Architects (reading, ~15 minutes)](https://k21academy.com/amazon-web-services/aws-solutions-architect/aws-x-ray/)
    [AWS X-Ray Official Documentation (reference)](https://docs.aws.amazon.com/xray/latest/devguide/aws-xray.html)

12. [Best Practices for Data Visualization (reading, ~8 minutes)](https://www.influxdata.com/blog/7-best-practices-data-visualization/)

---

## Cloud Monitoring

13. [Azure Monitor Best Practices (Microsoft Architecture, reading, 30 minutes)](https://learn.microsoft.com/en-us/azure/architecture/best-practices/monitoring)

14. [Azure Monitor Overview (reading, 15 minutes)](https://learn.microsoft.com/en-us/azure/azure-monitor/overview)

15. [AWS CloudWatch — What Is CloudWatch? (reading, 20 minutes)](https://docs.aws.amazon.com/AmazonCloudWatch/latest/monitoring/WhatIsCloudWatch.html)

16. [AWS CloudWatch Alarms Best Practices (reading, 15 minutes)](https://docs.aws.amazon.com/AmazonCloudWatch/latest/monitoring/Best-Practice-Alarms.html)

17. [AI-Driven Observability with Azure Monitor (reading, ~10 minutes)](https://learn.microsoft.com/en-us/azure/azure-monitor/aiops/aiops-machine-learning)

---

## Production Diagnostics in Practice

These resources show real investigation workflows — how a senior engineer moves from an alert firing to a root cause using Application Insights.

18. [Use OpenTelemetry with Azure Monitor Application Insights in .NET — Microsoft Learn (walkthrough, ~15 minutes)](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/observability-applicationinsights)
    *End-to-end setup walkthrough: instrument a .NET app with the Azure Monitor OTel distro, then explore logs, metrics, and distributed traces in the Application Insights portal.*

19. [Incident Investigation and Diagnosis — Azure Architecture Center (reading, 15 minutes)](https://learn.microsoft.com/en-us/azure/architecture/best-practices/monitoring#diagnosing-issues)
    *Structured guidance on the investigation workflow: from alert to diagnosis to root cause validation.*

20. [Practical OpenTelemetry for .NET — Martin Thwaites, NDC Oslo 2023 (video, ~60 minutes)](https://www.youtube.com/@NDCConferences)
    *Highly rated conference talk covering real-world OTel instrumentation in .NET: traces, metrics, and logs end-to-end with live demos. Search "Practical OpenTelemetry for .NET Martin Thwaites NDC Oslo 2023" on the NDC Conferences YouTube channel.*
