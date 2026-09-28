# Distributed Systems - Consistency and Consensus

### Disclaimer

1) Multiple learning resources may cover similar topics from different perspectives. Where alternatives are provided, choose the format (reading/video) that best suits your learning style.
2) Please, keep in mind that some links (*for example [Medium](https://codexbook.medium.com) links*) may require site authorization.
3) Please, let your mentor know if any link is not working. This will help improve the learning materials.

---

## Mandatory Module Materials

Mandatory module materials cover fundamental Consistency Models, Consensus Algorithms, Distributed Transactions, and .NET-Specific Implementation.

### Consistency Models and Types

- **Consistency Models vs Consistency Types**

A **Consistency Model** is a high-level theoretical guarantee of the system possibilities. It defines exactly what a programmer can expect from the system. It is a set of rules that governs the ordering of operations. 
A **Consistency Type** is a specific Consistency Model(s) implementation. These are often "tunable" to balance speed versus accuracy.

- **Data-Centric vs Client-Centric Consistency Models**

In distributed systems, consistency models and types are categorized by the perspective they prioritize: the data store's global state (data-centric) or the individual user's experience (client-centric).

**Data-Centric Consistency Models**
These focus on how updates are propagated across all replicas to provide a system-wide view of the data. They act as a "contract" between the data store and the processes: if processes follow certain rules, the store guarantees the data is correct.

*Usually include: Strict, Eventual, Causal and Weak Consistencies.*

**Client-Centric Consistency Models**

These provide guarantees for a single client interacting with different replicas over time, often used in systems where data is mostly read and updates are "lazy" (like a mobile user moving between network nodes).

*Usually include: Monotonic Reads, Monotonic Writes, Read-Your-Writes and Writes-Follow-Reads.*

**IMPORTANT!** These 2 big categories are two sides of the same coin: one defines the system's internal rules, while the other defines the user's observable guarantees. They are not mutually exclusive but rather complementary perspectives on how to handle distributed data. Please, keep this in mind when reading the following articles:

- **Consistency Model Types**
  - [Consistency in System Design](https://www.geeksforgeeks.org/system-design/consistency-in-system-design/) (reading, 8 min) — *Covers: Strong, Eventual, Causal, Weak, Read-your-Writes, Monotonic consistency types, Challenges, Strategies*
  - [Consistency Models in Distributed Systems: Strong vs Eventual vs Causal (A Practical Guide for Engineers)](https://medium.com/@dhruvsovasaria34/consistency-models-in-distributed-systems-strong-vs-eventual-vs-causal-a-practical-guide-for-a69ae1b97b92) (reading, 4 min) — *Covers: Strong, Eventual, Causal consistency with real-world examples (banking, social networks, dashboards), Trade-offs, How to choose*

- **Linearizability vs Serializability**
  - [Linearizability versus Serializability](https://systemdesignschool.io/blog/linearizability-vs-serializability/) (reading, 6 min) — *Covers: Linearizability concept (single-operation, real-time), Serializability concept (multi-operation transactions), Key differences, When to use each*

### Consensus Algorithms

- **Consensus Fundamentals**
  - [Consensus Algorithms in Distributed System](https://www.geeksforgeeks.org/consensus-algorithms-in-distributed-system/) (reading, 7 min) — *Covers: Paxos, Raft, PBFT, PoW, PoS, Quorum-based algorithms, Comparison table, Implementation challenges, How to choose*
  - [Quorum Consensus](https://medium.com/nerd-for-tech/quorum-consensus-56bc1bacb0d2) (reading, 5 min) — *Covers: R+W>N formula, Read/Write quorum mechanics, Examples with 3 replicas, High-Water Mark, Leader election, Atomicity guarantees*

- **Raft and Paxos Comparison**: [Paxos vs. Raft Algorithm in Distributed Systems](https://www.geeksforgeeks.org/system-design/paxos-vs-raft-algorithm-in-distributed-systems/) (reading, 6 min) — *Covers: Paxos phases (Prepare, Accept), Raft phases (Leader Election, Log Replication, Safety), Comparison, Use cases*

### Distributed Transactions

- **Two-Phase and Three-Phase Commit**
  - [What is a Distributed Transaction?](https://hazelcast.com/foundations/distributed-computing/distributed-transaction/) (reading, 5 min) — *Covers: ACID principles, Two-Phase Commit (2PC), XA transactions, Coordinator and Resource managers, Failure scenarios, When needed vs not needed*
  - [Understanding Two-Phase and Three-Phase Commit Protocols](https://daminibansal.medium.com/understanding-two-phase-and-three-phase-commit-protocols-key-differences-use-cases-and-practical-975e7c663c67) (reading, 4 min) — *Covers: 2PC (Prepare/Commit phases), 3PC (Pre-Commit phase), Blocking issues, Trade-offs, Use cases (Banking, E-commerce, Inventory)*

- **Saga Pattern**: [SAGA Pattern](https://microservices.io/patterns/data/saga.html) (reading, 8 min) — *Covers: Choreography-based saga, Orchestration-based saga, Compensating transactions, Lack of isolation handling, Event sourcing, Transactional Outbox*

### .NET-Specific Implementation

- **Saga Pattern in .NET**
  - [Implementing the Saga Pattern in .NET Microservices: A Comprehensive Guide](https://medium.com/@serhatalftkn/implementing-the-saga-pattern-in-net-microservices-a-comprehensive-guide-9b2b1d1366b6) (reading, 4 min) — *Covers: Choreography-based saga (Event-driven), Orchestration-based saga (NServiceBus), Idempotency patterns, Compensations, Production considerations (Timeout, Monitoring, Testing), Tools (MassTransit, NServiceBus, Dapr)*
  - [Demystifying Saga Design Pattern in C#: A Cure for Distributed Transactional Woes](https://shiftsync.tricentis.com/technical-discussion-38/demystifying-saga-design-pattern-in-c-a-cure-for-distributed-transactional-woes-324) (reading, 6 min) — *Covers: C# Saga implementation, State machine pattern, Error handling, Rollback strategies*

## Optional Module Materials

Optional module materials cover database-specific consistency configurations and advanced .NET patterns.  
Please, check **[02-optional-materials.md](./02-optional-materials.md)** for technology-specific implementations.