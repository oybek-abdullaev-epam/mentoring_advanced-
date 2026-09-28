## Questions for the self-check

**1. Replication Lag**  
- Q1.1: What is replication lag, and why does it occur?
- Q1.2: What is one way to detect replication lag in a production environment?
- Q1.3: Name one strategy to mitigate the negative impact of replication lag.
- Q1.4: What is a potential application-level consequence of replication lag?

**2. Leader-Follower (Master-Slave) Replication**  
- Q2.1: In a leader-follower replication setup, why might an application choose asynchronous replication over synchronous replication?  
- Q2.2: How is failover typically handled if the leader fails in a leader-follower system?  
- Q2.3: What is one major advantage of leader-follower replication?
- Q2.4: Give one reason an organization might still prefer a single-leader approach despite scalability concerns.  

**3. Multi-Leader Replication**  
- Q3.1: In multi-leader replication, why do conflicts occur more frequently than in leader-follower systems?  
- Q3.2: How can applications handle conflicting writes in a multi-leader setup?  
- Q3.3: What is a typical use case for multi-leader replication?
- Q3.4: What is the main reason some systems choose multi-leader replication despite the complexity of conflict resolution? 

**4. Leaderless Replication**  
- Q4.1: How does leaderless replication achieve consistency without a single leader node?  
- Q4.2: What role does "hinted handoff" play in leaderless replication systems?  
- Q4.3: What is read repair, and why is it important in leaderless replication?
- Q4.4: How does setting W = N (where N is the total number of replicas) impact write availability in a leaderless system?

5. **Practical Replication Considerations**  
- Q5.1: Which replication strategy (leader-follower, multi-leader, or leaderless) typically prioritizes availability over strong consistency? 