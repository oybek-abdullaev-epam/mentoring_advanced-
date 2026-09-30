_Questions for the self-check:_

1. Which scalability option is the primary choice for the high load systems?

- Horizontal scaling is the primary choice for the high load system, because vertical scaling has limitations, such hardware limits and cost

2. What are the main challenges to address horizontal scalability?

- Load balancing: round-robbin, consistent hashing
- Data consistency: either eventual consistency or distributed transactions
- State and session management: prefer not to have state / session, or move to external store such Redis
- Debugging and monitoring: distributed tracing should be used such OTel
- Deployment complexity: harder ci/cd, orchestrators such as Kubernetes should be used

3. Name at least 3 metrics for hardware and software metrics which you need to consider when designing a high load system?

- Hardware metrics: cpu, memory, disk saturation
- Software metrics: latency, throughput, error rate

4. Give examples of the domains where high load system design is required and is not required.

High load system design is required for high load applications, such as:
- large e-commerce
- banking and payment services
- gaming

High load system is not necessary for these kind of applications:
- small internal tools
- prototypes and MVPs
- products with small number of users
