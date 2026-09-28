## Questions for the self-check

**1. Scaling Fundamentals**
- Q1.1: What is the difference between vertical and horizontal scaling? What are the trade-offs of each approach?
- Q1.2: What is load balancing, and why is it essential for horizontally scaled systems?
- Q1.3: How does fault tolerance contribute to system reliability in distributed architectures?

**2. Batch and Stream Processing**
- Q2.1: What is batch processing, and what are typical use cases where batch processing is most appropriate?
- Q2.2: What is stream processing, and how does it differ from batch processing in terms of data handling and latency?
- Q2.3: When would you choose a hybrid processing architecture that combines both batch and stream processing?
- Q2.4: What are the main challenges when implementing stream processing in high-load systems?

**3. MapReduce and Distributed Processing**
- Q3.1: What is the MapReduce programming model, and how does it enable distributed data processing?
- Q3.2: What are the Map and Reduce phases, and what happens in each phase?
- Q3.3: What are some real-world applications that benefit from MapReduce?

**4. Messaging and Event-Driven Architecture**
- Q4.1: What is the role of a message broker in a distributed system?
- Q4.2: What is the difference between the Publisher-Subscriber pattern and the Producer-Consumer pattern?
- Q4.3: What is an event stream, and how is it used in event-driven architectures?
- Q4.4: What are the benefits of using event-driven architecture compared to synchronous request-response patterns?

**5. Event Sourcing**
- Q5.1: What is event sourcing, and how does it differ from traditional state management?
- Q5.2: What are the main benefits and challenges of implementing event sourcing?
- Q5.3: How do event sourcing and CQRS complement each other in distributed systems?

*Note: CQRS pattern is covered in 'DB: Replication' module.*

**6. Advanced Patterns**
- Q6.1: Why is immutability important in event-driven systems, especially with respect to state management?

**7. Caching and Performance**
- Q7.1: What are common distributed caching patterns (cache-aside, write-through, write-behind)?
- Q7.2: How does distributed caching help address scalability bottlenecks?
- Q7.3: What factors should you consider when setting cache expiration (TTL) policies?

**8. Practical Application**
- Q8.1: For a job scheduling system, would you use batch processing, stream processing, or a hybrid approach for executing scheduled jobs? Justify your choice.
- Q8.2: What events would you publish for the use case "Create a New Job"? Design a sample event schema.
- Q8.3: How would you handle failure scenarios in an event-driven architecture (e.g., message broker downtime, consumer failures)?
- Q8.4: If you need to process 1 million jobs scheduled for execution at the same time, what processing model and architecture would you choose?