## Questions for the self-check

**1. Replication Lag**  
- Q1.1: What is replication lag, and why does it occur?

Replication lag is a time between a write is accepted on the main database and the moment the same change becomes visible on a replica.
It occurs because applying a change to a replica is not immediate, and can be delayed due to network problems, overloaded replica, and etc.

- Q1.2: What is one way to detect replication lag in a production environment?

One way to detect replication lag is to monitor metrics: replication lag time and replication freshness timestamp. Another way is to observe application-facing indicators, such as read-after-write misses, stale reads, conflicts.

- Q1.3: Name one strategy to mitigate the negative impact of replication lag.

One strategy to mitigate the negative impact of replication lag is to use freshness timestamp / version / last-seen sequence number. This allows to route requests that risk to come across replication lag to replicas that are already up-to-date or to the primary database.

- Q1.4: What is a potential application-level consequence of replication lag?

Application-level consequences of replication lag are:
- read-your-own write miss: (when a user does not see their own write during replication lag)
- monotic read issues: when one request from replica shows more stale data compared to data that a user has already seen from another more fresh replica
- write conflicts


**2. Leader-Follower (Master-Slave) Replication**  
- Q2.1: In a leader-follower replication setup, why might an application choose asynchronous replication over synchronous replication?

Because synchronous replication adds latency or may even discard writes when replica is not available. For this reason applications where availability is more important then consistency might prefer asynchronous replication over synchronous.

- Q2.2: How is failover typically handled if the leader fails in a leader-follower system?

There are two ways:
- manual failover: a human chooses the most up-to-date replica and promotes to a leader
- automatic failover: a leader is chosen automatically based on the pre-defined promotion rule

- Q2.3: What is one major advantage of leader-follower replication?

Is the main advantage of leader-follower replication that it has the simplest mental model and thus easist correctness understanding.

- Q2.4: Give one reason an organization might still prefer a single-leader approach despite scalability concerns.  

An organization might still prefer single-leader approach because the cost of transitioning to multi-leader or leaderless strategy, and then maintaining it may be too expensive.

**3. Multi-Leader Replication**  
- Q3.1: In multi-leader replication, why do conflicts occur more frequently than in leader-follower systems?

In multi-leader replication conflicts occur more often because with this approach multiple databases accept writes to the same data at the same time without yet acknowlegding each other, which can lead to concurrent conflicting writes.

- Q3.2: How can applications handle conflicting writes in a multi-leader setup?  

There are multiple ways:
- Last-write-wins: choose the write that occured last and drop the rest
- Version timestamps: store all the versions of the same data as a vector of writes
- Application specific logic: some applications may be able to merge changes safely, for example adding tags
- Manual resolution
- Using CRDTs: conflict-free replicated data types

- Q3.3: What is a typical use case for multi-leader replication?

Typical use cases are multi-region applications that need local writes and edge or mobile scenarios with intermittent connectivity

- Q3.4: What is the main reason some systems choose multi-leader replication despite the complexity of conflict resolution? 

Teams might prefer to use multi-leader replication because it gives local autonomy. A region can keep writing even if connectivity to another region is poor. This can reduce latency and improve regional resilience.

**4. Leaderless Replication**  
- Q4.1: How does leaderless replication achieve consistency without a single leader node?

Leaderless replication achieves consistency by making sure that the write is propagated to at least W nodes, and then reads take the latest version from R nodes. As a common rule W+R should be greater then total number of nodes. This is called quorum.

- Q4.2: What role does "hinted handoff" play in leaderless replication systems?

Hinted handoff helps the system to forward changes from up-to-date replicas to out-of-date replicas by sending "hints". This helps to make system available even in case some replicas our unavailable.

- Q4.3: What is read repair, and why is it important in leaderless replication?

Read repair is when stale data is identified during read queries and then the new version is propagated to all the stale replicas. This process helps to bring stale data on some stale replicas into the actual state.

- Q4.4: How does setting W = N (where N is the total number of replicas) impact write availability in a leaderless system?

If we set W = N, then in case if only 1 replica becomes unavailable, the whole system will not be able to accept writes.

5. **Practical Replication Considerations**  
- Q5.1: Which replication strategy (leader-follower, multi-leader, or leaderless) typically prioritizes availability over strong consistency? 

Leaderless replication strategy typically prioritizes high availability with no single bottleneck over strong consistency. Thus it is frequently used in large distributed key-value systems and other highly available data platforms.
