# Distributed Systems - Replication

### Disclaimer

1) Multiple learning resources may cover similar topics from different perspectives. Where alternatives are provided, choose the format (reading/video) that best suits your learning style.
2) Please, keep in mind that some links (*for example [Medium](https://codexbook.medium.com) links*) may require site authorization.
3) Please, let your mentor know if any link is not working. This will help improve the learning materials.

---

## Mandatory Module Materials

Mandatory module materials cover fundamental Replication concepts, strategies, conflict Resolution and NET-Specific Replication Implementation.

### Replication Fundamentals

- **What is Data Replication?**: [What is Data Replication?](https://www.ibm.com/think/topics/data-replication) (reading, 12 min) — *Covers: Replication Concept, Synchronous vs. Asynchronous Replication, Replication Benefits and Risks, Replication Types, Techniques and Schemes*

- **Synchronous vs Asynchronous Replication**
  - [Synchronous vs Asynchronous Replication in Databases](https://apoorvashettigar.substack.com/p/replication-101-synchronous-versus?utm_medium=web) (reading, 8 min) **OR** [Database Failover & Performance: Synchronous vs. Asynchronous Replication](https://www.youtube.com/watch?v=FzUNbm01gJ0) (video, 5 min) - *Covers: Synchronous vs. Asynchronous Replication Benefits, Risks and Use-Cases*

- **Replication Lag**: [What is Replication Lag and What are the Solutions?](https://programmingappliedai.substack.com/p/what-is-replication-lag-and-what) (reading, 7 min) — *Covers: Replication Lag Concept, Reasons, Impact and Mitigation*

### Replication Strategies & Conflict Resolution

- **How is Data Replicated in Distributed Systems?**: [How is Data Replicated in Distributed Systems?](https://newsletter.scalablethread.com/p/single-leader-multi-leader-and-leaderless) (reading, 8 min) — *Covers: Single Leader Replication, Multi-Leader Replication, Leaderless Replication*

- **Leader-Based / Single-Leader / Master-Slave Replication**
  - [Understanding Leader-Follower Replication](https://fedianin.com/2025/01/06/understanding-leader-follower-replication/) (reading, 10 min) **OR** [Leader Follower Database Replication](https://www.youtube.com/watch?v=uq4kb7gLrPQ) (video, 16 min) -  *Covers: Leader-Based Replication Common Idea, Modes and Topologies, Discovery and Routing, Common Issues*

- **Multi-Leader Replication**
  - [Mastering Multi-Leader Replication: Topologies & Conflicts Scenarios Explained](https://medium.com/@satyavarssheni/mastering-multi-leader-replication-topologies-conflicts-scenarios-explained-0fedf8f5ed7d) (reading, 8 min) **OR** [Learn in 5 Minutes: Multi-Leader Replication](https://www.youtube.com/watch?v=AnG1vr5nj80) (video, 5 min) - *Covers: Multi-Leader Replication Concept, Use-Cases, Conflict Resolution, Topologies*

- **Leaderless / Quorum-Based Replication**
  - [Leaderless Replication Topology](https://www.geeksforgeeks.org/system-design/leaderless-replication-topology/) (reading, 6 min)  
  - [Replication: Maintaining a Quorum](https://distributed-computing-musings.com/2022/01/replication-maintaining-a-quorum/) (reading, 7 min)
  - [Sloppy Quorum and Hinted handoff: Quorum in the times of failure](https://distributed-computing-musings.com/2022/05/sloppy-quorum-and-hinted-handoff-quorum-in-the-times-of-failure/) (reading, 7 min)
  - [Read Repair and Anti-Entropy : Two Ways To Remedy Replication Lag in Leaderless Replication](https://efficientcodeblog.wordpress.com/2017/12/26/read-repair-and-anti-entropy-two-ways-to-remedy-replication-lag-in-dynamo-style-datastores-leaderless-replication/#:~:text=Menu-,Read%20Repair%20and%20Anti%2DEntropy%20:%20Two%20Ways%20To%20Remedy%20Replication,before%20the%20data%20is%20copied.) (reading, 3 min)
  
  *Covers: Leaderless Replication Concept, Use-Cases, Quorum, Sloppy Quorum, Hinted handoff, Read Repair and Anti-Entropy*

- **Addressing Replication Lag Issues**: [Designing Data-Intensive Applications: Solutions for Replication Lag](https://sayedalesawy.hashnode.dev/designing-data-intensive-applications-ch5-replication-part-2-problems-with-replication-lag#heading-solutions-for-replication-lag) (reading, 12 min) — *Covers: Read-your-writes, Monotonic Reads, Consistent Prefix Reads*

### .NET-Specific Replication Implementation

- **Entity Framework Core with Read/Write Replicas**: [How to implement Read/Write Replicas with Entity Framework Core?](https://oneuptime.com/blog/post/2026-02-16-read-replicas-entity-framework-core-azure-sql/view) (reading, 10 min) — *Covers: EF Core, Azure SQL, Read Replicas, Connection String Management, DbContext Configuration*

- **CQRS Pattern in C#**
  - [CQRS pattern (Basics)](https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs) (reading, 8 min) 
  - [Implement CQRS Design Pattern with MediatR in ASP.NET Core 6 (C#)](https://medium.com/@1nderj1t/implement-cqrs-design-pattern-with-mediatr-in-asp-net-core-6-c-dc192811694e) (reading, 5 min) **OR** [Implementing CQRS in ASP.NET Core with Dapper: A Practical Guide](https://medium.com/@mouhssine.gamal-etu/implementing-cqrs-in-asp-net-core-with-dapper-a-practical-guide-008be6f22a8e) (reading, 4 min)
  
  *Covers: CQRS Pattern, MediatR, WebAPI*

---

## Optional Module Materials

Optional module materials cover Replication configuration and practices in popular SQL and NoSQL databases and Cloud Services.  
Please, check **[02-optional-materials.md](./02-optional-materials.md)** for technology-specific implementations.