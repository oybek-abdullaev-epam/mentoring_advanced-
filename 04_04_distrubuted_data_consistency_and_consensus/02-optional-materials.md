# Distributed Systems - Consistency and Consensus

### Disclaimer

1) Multiple learning resources may cover similar topics from different perspectives. Where alternatives are provided, choose the format (reading/video) that best suits your learning style.
2) Please, keep in mind that some links (*for example [Medium](https://codexbook.medium.com) links*) may require site authorization.
3) Please, let your mentor know if any link is not working. This will help improve the learning materials.

---

## Optional Module Materials

Optional module materials cover advanced consistency theory and database-specific consistency configurations.

### Advanced Consistency Theory

- **Deep-Dive into Consistency Guarantees**: [Consistency Guarantees in Distributed Systems Explained Simply](https://kousiknath.medium.com/consistency-guarantees-in-distributed-systems-explained-simply-720caa034116) (reading, 34 min) — *Covers: Eventual, Consistent Prefix Read, Session Guarantees (RYOW, Monotonic Read/Write, Write-Follows-Read), Causal, Bounded Staleness, Sequential, Linearizability, Strict consistency with detailed examples*

### Database-Specific Consistency Configuration

**You're not expected to read all the links about all the databases!** Please, choose the databases required for your project and/or home task scope.

- **Apache Cassandra**: [Consistency Levels in Cassandra](https://docs.datastax.com/en/cassandra-oss/3.0/cassandra/dml/dmlConfigConsistency.html) (reading, 10 min) — *Covers: Write levels, Read levels, Quorum calculation formula, cqlsh CONSISTENCY command, Client driver configuration*

- **Amazon DynamoDB**: [DynamoDB Read Consistency](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/HowItWorks.ReadConsistency.html) (reading, 4 min) — *Covers: Eventually consistent reads (default, half cost), Strongly consistent reads (ConsistentRead parameter), Global tables multi-region consistency, Read-committed isolation, LSI vs GSI consistency*

- **Azure Cosmos DB**:[Consistency Levels in Azure Cosmos DB](https://learn.microsoft.com/en-us/azure/cosmos-db/consistency-levels) (reading, 15 min) — *Covers: 5 levels (Strong, Bounded Staleness, Session, Consistent Prefix, Eventual), Dynamic quorum, K & T staleness bounds, Session tokens, Per-request override, SLA guarantees, Throughput impact, RPO/RTO considerations*

- **MongoDB**:[Read Concern and Write Concern](https://www.mongodb.com/docs/manual/reference/read-concern/) (reading, 12 min) — *Covers: Read concern levels (local, available, majority, linearizable, snapshot), Causally consistent sessions, Transactions read concern, maxTimeMS timeout, afterClusterTime, Write concern coordination*

---

## Mandatory Module Materials

Mandatory module materials cover Consistency Models, Consensus Algorithms, Distributed Transactions, and .NET-Specific Implementation.  
Please, check **[01-self-study-materials.md](./01-self-study-materials.md)** for fundamental Consistency and Consensus info.