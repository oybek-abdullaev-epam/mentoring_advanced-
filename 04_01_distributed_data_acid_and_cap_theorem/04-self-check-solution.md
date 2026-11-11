_Questions for the self-check:_

1. **ACID Transactions**
   1.  What does each component of ACID (Atomicity, Consistency, Isolation, Durability) ensure in a transaction?

   Atomicity ensures that either every operation in the transaction is applied or none.

   Consistency ensures a transaction takes the database from one valid state to another, e.g. all the defined rules (constraints, keys and so on) hold after each transaction commits.

   Isolation ensures that concurrent transactions do not interfere with each other, and each behaves as if they are applied one after another.
   
   Durability ensures once a transaction commits, its changes survive crashes, power loss, restarts and so on.


   2. How does atomicity guarantee that a transaction is treated as an indivisible unit?

   This is ensured by first writing all the steps in a sequential log (Write-ahead log) before they are applied to the data files.

   If all the steps were applied successfully, then only these changes are applied to the data files.

   In case if something fails in-between, than these changes are not persisted in the data files or rolled back from the same sequential log.


   3. What are the key differences between single-object and multi-object transactions, and why do these differences matter in system design?

   The key difference between single-object and multi-object transactions is that single-object's scope is only one row or document, while multi-object transactions apply to many related rows or documents.

   This difference matters a lot in system design because multi-object transactions have only limited support in NoSQL systems, so if a system does a lot of multi-object transactions it might be a good idea to stick to relation databases.


   4. List and explain the various isolation levels available in DBMS and their impact on concurrent transactions.

   Read uncommited - even changes that are not yet commited by another transaction are visible.

   Read commited - only changes that are commited by another transaction are visible.

   Repeatable read - on top of read commited also ensures that if we read the same row twice during the same transaction result will be the same.

   Snapshot isolation - gives each transaction its own snapshot of the database state.

   Serialazable - equivalent of serial execution.


   5. How do different isolation levels affect the occurrence of issues such as dirty reads, non-repeatable reads, and phantom reads?

   Read uncommited - dirty read, non-repeatable read, phantom read, lost update possible.

   Read commited - dirty read prevented; non-repeatable read, phantom read, lost update possible.

   Repeatable read - dirty read and non-repeatable read prevented; phantom read, lost update possible (depends on implementation).

   Snapshot isolation - dirty read, non-repeatable read, phantom read prevented; lost update depends on implementation; write skew possible.

   Serialazable - all anomalies are prevented, equivalent of serial execution.


   6. In a distributed system, what trade-offs might you face when choosing a higher isolation level versus a lower one?
   
   Choosing higher isolation levels may result in some transaction blocking other transactions, which leads to lower throughput and higher latency.


2. **Conceptual Understanding (Basics)**  
   1. What do each of the three letters in CAP (Consistency, Availability, Partition Tolerance) stand for, and what does each term mean?

   Consistency means that every read has to return only the most recent write, not stale data, regardless of which replica that read comes from.

   Availability means that every request to a non-failing node must return a response, even if it is a stale data.

   Partition tolerance means that the system keeps working even when the network drops or delays messages between nodes, causing the cluster split into groups that cannot talk to each other.


   2. In your own words, state the CAP theorem. What does it assert about the ability of a distributed system to provide consistency, availability, and partition tolerance simultaneously?

   In general, it means that you cannot reach all 3 at the same time. Moreover, you cannot completely avoid network partitions, so you choose either C or A in case of a partition.


   3. According to the CAP theorem, what trade-off must a system make when a network partition occurs?

   The trade-off is that you can never reach both Consistency and Availability in a distributed system when a network partition occurs. So you choose between CP (reject requests in case of network partitions) or AP (continue serving requests with possibly stale data).
   

   4. Why is Partition Tolerance often considered mandatory in real distributed systems?

   Because in real distributed systems you can not guarantee that network is always stable, so you have to account for the cases when partitions happen.


   5. The “choose any two” phrasing of CAP is sometimes seen as oversimplified. Why?  

   Because in reality P is not optional, so you always choose between CP or AP.


3. **Trade-Offs and Real-World Design Considerations**   
   1. If a distributed system guarantees consistency and availability, what happens during a network partition?

   A system can guarantee consistency and availability when the netwrok is stable, but it can no more guarantee both during a network partition, so it will either serve stale data or start rejecting requests due to network partitions.


   2. Give an example of a real-world CP system. Why does it choose consistency over availability?

   One example of CP system is ZooKeeper. It choose to be CP system because it is mainly used for coordination, such as leader election, distributed locks, service discovery and so on. And for such use cases correctness of the response is much more important than receiving responses 100% with a possibility of stale data.


   3. Give an example of a real-world AP system. Why does it prioritize availability over consistency?

   DynamoDB is an example of AP system. It chooses to be AP because its use cases are for example shopping cart and so on, where being able to give a response 100% of the time is much more important, even with possibly some stale data, than consistency in every response.


   4. How does a CP system differ from an AP system during a partition in terms of user experience?  

   In terms of user experience, CP system starts rejecting requests in case of network partition, which means that user has to wait until the system is back to normal. In AP system user always get a response, even when partition occurs, but sometimes user may see stale data.


   5. What factors should you consider when deciding between CP and AP for a distributed application?  

   I think the main factor when deciding between CP and AP for a distributed system are the main use cases. An engineer should decide if in the most frequent scenarios users want to get responses as fast as possible, even if stale, or they prefer always getting the latest data, even if sometimes their requests are rejected.


4. **Practical Application Scenarios**  
   1. Online banking system — prioritize consistency or availability under a partition?

   For online banking system, especially for use cases such as ledger and transactions, consistency should always be prioritized.
   However for reports availability might be a better choice.


   2. Social media feed — is it better to show stale data or disable the feed during a partition?

   For a social media feed it is much better to show at least something, even stale data, than just showing nothing.


   3. E-commerce shopping cart replication under partition — allow updates or block them?

   We should allow updates to shopping carts under partition without blocking, and then try to sync somehow after system gets back to normal.


   4. Configuration service scenario — continue with stale config or block usage until updated?  

   Most of the times for configuration service we should choose consistency, e. g. block usage until updates because clients of the service rely on it as a point of truth.


5. **Advanced Topics: PACELC and Dynamic Adjustments**  
   1. What is the PACELC theorem, and how does it extend CAP?  

   PACELC extends CAP by also bringing latency into the table.
   It says that in case of partition tolerance you should choose between availability and consistency; else choose between latency or consistency.


   2. What is the latency vs. consistency trade-off in PACELC?

   Latency vs consistency tradeoff in PACELC arises when network is stable. It says that you should choose between waiting for stronger coordination, ensuring strong consistency, or return faster, increasing latency, but with a possibility to loose data.


   3. What is tunable consistency, and how does it help adjust CAP trade-offs?  

   In Cassandra for example, you can tune consistency by choosing replication factor (One, quorum, all). One is used for lower latency, but stale data is acceptable. Quorum or all require stronger guarantees and ensure better freshness, but still no guarantee.


   4. How might a system dynamically change CAP preferences when network conditions change?  

   A system might dynamically change based on network conditions by applying different policies to different operations, for example. It might reject writes that must be correct, such as transactions or configuration changes; and it might keep accepting writes that tolerate conflicts, such as carts or likes, and try to recover the state later.

   If using Cassandra or DynamoDB, system might tune the replication factor based on the network conditions, for example fallback to QUORUM or ONE in case if nodes are not available. 

