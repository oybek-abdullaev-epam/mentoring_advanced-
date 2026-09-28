# Distributed Data - ACID and CAP Theorem

### Disclaimer

1) Multiple learning resources may cover similar topics from different perspectives. Where alternatives are provided, choose the format (reading/video) that best suits your learning style.
2) Please, keep in mind that some links (*for example Medium links*) may require site authorization.
3) Please, let your mentor know if any link is not working. This will help improve the learning materials.

---

## Mandatory Module Materials

Mandatory module materials cover fundamental ACID and CAP concepts, isolation levels, recovery, and consistency trade-offs.

### ACID Fundamentals

- **What is Transaction?**: [Transaction in DBMS](https://www.geeksforgeeks.org/dbms/transaction-in-dbms/) (reading, 7 min) — *Covers: Transaction definition, ACID basics, lifecycle*
- **Single- and Multi-object Transactions**: [Single-Object and Multi-Object Operations](https://timilearning.com/posts/ddia/part-two/chapter-7/#single-object-and-multi-object-operations) (reading, 5 min) — *Covers: Single- vs Multi-object Transactions, distributed context*
- **What is ACID?**: [ACID Properties in DBMS](https://www.geeksforgeeks.org/dbms/acid-properties-in-dbms/) (reading, 6 min) **OR** [ACID Properties in Databases With Examples](https://www.youtube.com/watch?v=GAe5oB742dw) (video, 5 min) — *Covers: Atomicity, Consistency, Isolation, Durability, examples*
- **Data Phenomena and Transaction Isolation Levels**: [Understanding Database Phenomena and Isolation Levels](https://medium.com/@ali75mnf/understanding-database-phenomena-and-isolation-levels-6f1fc340b2e5) (reading, 8 min) **OR** [Relational Database ACID Transactions (from 9:30 till 27:45)](https://www.youtube.com/watch?v=pomxJOFVcQs&t=570s) (video, 18 min) — *Covers: Isolation levels, phenomena (dirty, non-repeatable, phantom reads), implementation, best practices*
- **Transactions Recovery Techniques**:
    - [Database Recovery Techniques in DBMS](https://www.geeksforgeeks.org/dbms/database-recovery-techniques-in-dbms/) (reading, 7 min) — *Covers: Recovery types, log-based recovery, checkpointing*
    - [Write-Ahead Logging (WAL) in Database Engines & Recovery](https://medium.com/@jatinumamtora/a-deep-dive-into-write-ahead-logging-wal-in-database-engines-recovery-71f6d98f0e23) (reading, 9 min) **OR** [Write-Ahead Logging (WAL) Explained](https://www.youtube.com/watch?v=MHAzvg3uEDA) (video, 3 min) — *Covers: WAL concept, durability, crash recovery*

*Note: "Distributed Transactions" and "Atomic Commit Protocols" topics are covered in the Module "04_04 DB: Consistency and Consensus"*

### CAP Theorem & PACELC Theorem

- **CAP Theorem and its Trade-Offs**: [The CAP Theorem in DBMS](https://www.geeksforgeeks.org/dbms/the-cap-theorem-in-dbms/) (reading, 6 min) **OR** [Friendly Intro To the CAP Theorem](https://www.youtube.com/watch?v=gkg-FAEXIkY) (video, 7 min) — *Covers: CAP definition, trade-offs, distributed context*
- **Database Decision Making with CAP Theorem**: [CAP Theorem in Real-World Scenarios](https://dev.to/codemasheen/cap-theorem-in-real-world-scenarios-559f) (reading, 8 min) — *Covers: CAP in practice, database selection, real-world examples*
- **CAP Theorem Critics**: [What are the limits of the CAP theorem? (read first 2 sections)](https://www.cockroachlabs.com/blog/limits-of-the-cap-theorem/) (reading, 5 min) — *Covers: CAP limitations, criticisms, practical relevance*
- **PACELC Theorem**: [The PACELC Theorem - extending CAP with Latency Trade-Offs](https://www.thecoder.cafe/p/pacelc) (reading, 7 min) — *Covers: PACELC extension, latency trade-offs, distributed systems*
- **Tunable Consistency Model**: [Cassandra’s Tunable Consistency Model: A Game-Changer for Distributed Systems](https://medium.com/@preethikcs01/cassandras-tunable-consistency-model-a-game-changer-for-distributed-systems-%EF%B8%8F-132a295749ce) (reading, 8 min) — *Covers: Tunable consistency, Cassandra, distributed trade-offs*

*Note:*
- *"Consistency Modules/Levels" topic is covered in the Module "04_04 DB: Consistency and Consensus"*
- *"Partition Tolerance techniques (replication, sharding)" topic is covered in the Module "04_03 DB: Partitioning and Sharding"*

---

## Optional Module Materials

Optional module materials cover ACID/CAP configuration, isolation, and practices in popular SQL and NoSQL databases.
Please, check **[02-optional-materials.md](./02-optional-materials.md)**.