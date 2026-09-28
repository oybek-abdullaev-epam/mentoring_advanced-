_Questions for the self-check:_

1. **ACID Transactions**
   1.  What does each component of ACID (Atomicity, Consistency, Isolation, Durability) ensure in a transaction?
   2. How does atomicity guarantee that a transaction is treated as an indivisible unit?
   3. What are the key differences between single-object and multi-object transactions, and why do these differences matter in system design?
   4. List and explain the various isolation levels available in DBMS and their impact on concurrent transactions.
   5. How do different isolation levels affect the occurrence of issues such as dirty reads, non-repeatable reads, and phantom reads?
   6. In a distributed system, what trade-offs might you face when choosing a higher isolation level versus a lower one?
   

2. **Conceptual Understanding (Basics)**  
   1. What do each of the three letters in CAP (Consistency, Availability, Partition Tolerance) stand for, and what does each term mean?  
   2. In your own words, state the CAP theorem. What does it assert about the ability of a distributed system to provide consistency, availability, and partition tolerance simultaneously?
   3. According to the CAP theorem, what trade-off must a system make when a network partition occurs?  
   4. Why is Partition Tolerance often considered mandatory in real distributed systems?  
   5. The “choose any two” phrasing of CAP is sometimes seen as oversimplified. Why?  


3. **Trade-Offs and Real-World Design Considerations**   
   1. If a distributed system guarantees consistency and availability, what happens during a network partition?    
   2. Give an example of a real-world CP system. Why does it choose consistency over availability?  
   3. Give an example of a real-world AP system. Why does it prioritize availability over consistency?   
   4. How does a CP system differ from an AP system during a partition in terms of user experience?  
   5. What factors should you consider when deciding between CP and AP for a distributed application?  


4. **Practical Application Scenarios**  
   1. Online banking system — prioritize consistency or availability under a partition?  
   2. Social media feed — is it better to show stale data or disable the feed during a partition?  
   3. E-commerce shopping cart replication under partition — allow updates or block them?  
   4. Configuration service scenario — continue with stale config or block usage until updated?  


5. **Advanced Topics: PACELC and Dynamic Adjustments**  
   1. What is the PACELC theorem, and how does it extend CAP?  
   2. What is the latency vs. consistency trade-off in PACELC?  
   3. What is tunable consistency, and how does it help adjust CAP trade-offs?  
   4. How might a system dynamically change CAP preferences when network conditions change?  