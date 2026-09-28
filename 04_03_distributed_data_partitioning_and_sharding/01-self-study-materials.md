# Distributed Systems - Partitioning and Sharding

### Disclaimer

1) Multiple learning resources may cover similar topics from different perspectives. Where alternatives are provided, choose the format (reading/video) that best suits your learning style.
2) Please, keep in mind that some links (*for example [Medium](https://codexbook.medium.com) links*) may require site authorization.
3) Please, let your mentor know if any link is not working. This will help improve the learning materials.

---

## Mandatory Module Materials

Mandatory module materials cover fundamental Partitioning and Sharding concepts, strategies, practical aspects, and .NET-Specific Implementation.


### Partitioning and Sharding Fundamentals

- **Partitioning Basics**
  - [Data Partitioning in System Design: Why It Matters and How It Solves Real-World Problems](https://medium.com/@kumud.sharma.0206/data-partitioning-in-system-design-why-it-matters-and-how-it-solves-real-world-problems-4c504825e4dc) (reading, 4 min) — *Covers: Partitioning concept, Partitioning Benefits, Challenges & Trade-offs, Partitioning Strategies*
  - [Data Partitioning Strategies in System Design (More detailed)](https://www.geeksforgeeks.org/system-design/data-partitioning-techniques/) (reading, 5 min) — *Covers: Horizontal, Vertical, Key-Based, Hash-Based, Range, Round-Robin Partitioning strategies, their Pros & Cons*

- **Sharding Basics**
  - [Database Sharding - System Design](https://www.geeksforgeeks.org/system-design/database-sharding-a-system-design-concept/) (reading, 6 min) **OR** [What is Database Sharding?](https://www.youtube.com/watch?v=XP98YCr-iXQ) (video, 9 min)
  - How to implement complex queries across shards when retrieving data from a database?
    - [How to Avoid Cross-Shard Queries?](https://planetscale.com/docs/vitess/sharding/avoiding-cross-shard-queries) (reading, 3 min)
    - [How to Handle Cross-Shard Queries?](https://oneuptime.com/blog/post/2026-03-31-mysql-handle-cross-shard-queries/view) (reading, 4 min)
  
  *Covers: Sharding concept, Database Sharding Benefits, Sharding Strategies, Cross-Shard Queries, Distributed Queries, Query Aggregation, Transaction Management*

- **Partitioning vs Sharding vs Replication vs Caching**
  - [Scaling Databases: Replication vs. Partitioning vs. Sharding](https://medium.com/@Bhupi2508/replication-vs-partitioning-vs-sharding-understanding-database-scaling-made-simple-392c43cc4faa) (reading, 6 min)
  - [Scaling Databases: Replicas, Sharding, and Caching — Databases Made Practical](https://www.youtube.com/watch?v=6NLjNOo12Pk) (video, 7.5 min)
  
  *Covers: Comparison of scaling approaches with live examples*

- **Partitioning and Sharding Challenges**
    - [Disadvantages of Sharding/Partitioning](https://medium.com/@abdullah.tahir_45158/database-management-w-sharding-partitioning-30359797e0f2) (reading, 5 min) — *Covers: operational complexity, uneven data distribution, queries and cross-shard transactions issues, resharding and maintenance issues*
    - [High level approach of mitigating hot shard](https://medium.com/@ankitshah0205/high-level-approach-of-mitigating-hot-shard-ab8a42844364) (reading, 5 min) - *Covers: Sharding hotspot prevention*

### .NET-Specific Partitioning and Sharding Implementation

- **Custom Shards Routing in .NET**: [From Chaos to Control: Mastering Sharding & Partitioning in .NET Like a Pro](https://medium.com/@iamrks/from-chaos-to-control-mastering-sharding-partitioning-in-net-like-a-pro-be6e73a875ea) (reading, 3 min) — *Covers: Shards Map, Shards Resolver*

---

## Optional Module Materials

Optional module materials cover OLAP & OLTP, Sharding configuration and practices in popular SQL and NoSQL databases.  
Please, check **[02-optional-materials.md](./02-optional-materials.md)** for technology-specific implementations.