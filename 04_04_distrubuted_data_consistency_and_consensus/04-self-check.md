## Questions for the self-check

**1. Consistency Models**
- Q1.1: What are the trade-offs between strong and weak consistency in distributed systems?
- Q1.2: What is causal consistency, and how does it differ from eventual consistency?
- Q1.3: Which consistency model ensures that any read returns the result of the most recent write across the system (i.e., no stale data)?
- Q1.4: Which consistency model provides only eventual convergence of replicas without guaranteeing immediacy of writes on reads?
- Q1.5: What is the Read-Your-Writes (RYOW) session guarantee, and in what scenarios is it essential?

**2. Linearizability and Serializability**  
- Q2.1: What is the difference between linearizability and serializability?

**3. Consensus Algorithms**  
- Q3.1: What is consensus in distributed systems, and why is it important for high-load applications?
- Q3.2: What are the main challenges in achieving consensus in a distributed system?
- Q3.3: What is a quorum in distributed consensus, and why is the R+W>N formula important?
- Q3.4: What is the Raft consensus algorithm, and why was it developed as an alternative to Paxos?

**4. Distributed Transactions**  
- Q4.1: What is a distributed transaction, and why do such transactions require special protocols?
- Q4.2: How does the Two-Phase Commit (2PC) protocol work to achieve atomic commit across multiple nodes? What's the difference with Three-Phase Commit (3PC) protocol?
- Q4.3: What is the difference between Choreography-based and Orchestration-based Saga implementations?
- Q4.4: Why is Correlation ID a good practice in distributed transactions, and how does it help with debugging and tracing?  