# Application Scaling - Batch and Stream Processing

### Disclaimer

1) Multiple learning resources may cover similar topics from different perspectives. Where alternatives are provided, choose the format (reading/video) that best suits your learning style.
2) Please, keep in mind that some links (*for example Medium links*) may require site authorization.
3) Please, let your mentor know if any link is not working. This will help improve the learning materials.

---

## Mandatory Module Materials

Mandatory module materials cover fundamental application scaling concepts, batch and stream processing models, messaging systems, and event-driven architectures.

### Application Scaling Fundamentals

- **What is Application scaling?**: [Application scalability: key strategies and best practices](https://vfunction.com/blog/application-scalability/) (reading, 10 min) — *Covers: Application scalability definition, scaling strategies, performance optimization, capacity planning*
- **Stateful vs Stateless Applications and their Scaling**: [Stateful vs Stateless architecture for scalable systems explained](https://aerospike.com/blog/stateful-vs-stateless-architecture-guide/) (reading, 8 min) — *Covers: Stateful vs stateless architecture, session management, horizontal scaling impact, design trade-offs*
- **Scaling Strategies**: [Effective Strategies for Scaling an Application](https://dev.to/wallacefreitas/effective-strategies-for-scaling-an-application-161h) (reading, 7 min) — *Covers: Scaling strategies overview, load balancing, caching, database optimization, microservices*
- **Vertical vs Horizontal Scaling**: [Understanding Vertical and Horizontal Scaling in Depth](https://medium.com/@mrprince123/scaling-systems-the-right-way-understanding-vertical-and-horizontal-scaling-in-depth-40863534dec4) (reading, 9 min) **OR** [Vertical Vs Horizontal Scaling: Key Differences You Should Know](https://dev.to/somadevtoo/horizontal-scaling-vs-vertical-scaling-in-system-design-3n09) (reading, 6 min) — *Covers: Vertical vs horizontal scaling comparison, benefits, limitations, use cases, cost considerations*
- **Load Balancing**: [Load Balancing: System Design Fundamentals](https://medium.com/@abhirup.acharya009/load-balancing-system-design-fundamentals-d64674227c36) (reading, 7 min) — *Covers: Load balancing concepts, algorithms (round-robin, least connections, weighted), implementation patterns, health checks*
- **Fault Tolerance**: [Fault Tolerance in Distributed System](https://www.geeksforgeeks.org/fault-tolerance-in-distributed-system/) (reading, 6 min) — *Covers: Fault tolerance mechanisms, redundancy strategies, failure detection, recovery techniques*

### Data Processing Models

- **Batch and Stream Processing**: 
  - [Difference between batch and stream processing](https://www.geeksforgeeks.org/difference-between-batch-processing-and-stream-processing/) (reading, 5 min) — *Covers: Batch vs stream processing comparison, characteristics, use cases, advantages and disadvantages*
  - [Batch vs Streaming Data: Use Cases and Trade-offs in Data Engineering](https://datascienceafrica.medium.com/batch-vs-streaming-data-use-cases-and-trade-offs-in-data-engineering-12efda897e9a) (reading, 8 min) — *Covers: Real-world use cases, trade-offs analysis, latency vs throughput, data engineering perspectives*
- **Hybrid Approach**: [Data Pipeline Architecture Demystified: Batch, Streaming, or Hybrid? (read the 3rd section)](https://medium.com/@shivanireddyyy/data-pipeline-architecture-demystified-batch-streaming-or-hybrid-3d664fe8cd59) (reading, 6 min) — *Covers: Hybrid processing architecture, Lambda architecture, combining batch and stream, when to use hybrid approach*
- **MapReduce Approach**: [A Comprehensive Guide to MapReduce: Distributed Data Processing](https://dev.to/leodalcegio/a-comprehensive-guide-to-mapreduce-distributed-data-processing-3lj8) (reading, 10 min) — *Covers: MapReduce fundamentals, Map and Reduce phases, distributed processing, use cases, examples*

### Message- and Event-Driven Architectures

- **Event Brokers**
  - **Publish-Subscribe vs Point-to-point Messaging Models**: [Point-to-point and Publish-Subscribe Messaging Model](https://dev.to/tranthanhdeveloper/point-to-point-and-publish-subscribe-messaging-model-41j0) (reading, 4 min) — *Covers: Point-to-point vs pub-sub comparison, message delivery patterns, use cases*
  - **Publisher-Subscriber vs Producer-Consumer Patterns**: [Do you understand Publisher-Subscriber Vs Producer-Consumer Pattern Differences](https://dev.to/dsysd_dev/do-you-understand-publisher-subscriber-vs-producer-consumer-pattern-differences-2nh3) (reading, 5 min) — *Covers: Pub-Sub vs Producer-Consumer patterns, key differences, when to use each, implementation examples*
  - **Event Brokers**: [Message Brokers in System Design](https://www.geeksforgeeks.org/system-design/what-are-message-brokers-in-system-design/) (reading, 8 min) **OR** [What is a message broker?](https://www.youtube.com/watch?v=sqlV8mHoils) (video, 4 min) — *Covers: Message broker concept, architecture, popular message brokers (RabbitMQ, Kafka, ActiveMQ), benefits, use cases*
  - **Message Broker vs. Message Queue**: [Message Broker vs. Message Queue: What's the Difference?](https://www.confluent.io/compare/message-broker-vs-queue/) (reading, 6 min) — *Covers: Message broker vs message queue comparison, capabilities, routing patterns, when to use each*
- **Event Stream Processing**: [Event stream basics — An overview](https://www.redpanda.com/guides/event-stream-processing-event-stream) (reading, 7 min) — *Covers: Event streaming fundamentals, stream processing concepts, event ordering, real-time processing*
- **Event-Driven Architecture**: [The Complete Guide to Event-Driven Architecture](https://medium.com/@seetharamugn/the-complete-guide-to-event-driven-architecture-b25226594227) (reading, 12 min) — *Covers: Event-driven architecture patterns, components, benefits, challenges, implementation strategies, real-world examples*

### Immutability and Event Sourcing

- **Data Immutability**: [Data Immutability](https://www.dremio.com/wiki/data-immutability/) (reading, 5 min) **OR** [Immutability concept overview](https://www.youtube.com/watch?v=jjf5nEmDjaE) (video, 6 min) — *Covers: Immutability principles, benefits in distributed systems, state management, concurrency advantages*
- **Event Sourcing Pattern**: [Event Sourcing Pattern](https://www.geeksforgeeks.org/system-design/event-sourcing-pattern/) (reading, 8 min) **OR** [Event Sourcing Explained](https://www.youtube.com/watch?v=yFjzGRb8NOk) (video, 5 min) — *Covers: Event sourcing concept, event store, rebuilding state, benefits, challenges, use cases*

*Note: "CQRS Pattern" was covered in the "DB: Replication" module. "Saga Pattern" was covered in the "DB: Consistency and Consensus" module.*

### Caching Strategies

- **Distributed Caching Overview**: [Overview of Caching Distributed Cache Caching Patterns Techniques](https://medium.com/geekculture/overview-of-caching-distributed-cache-caching-patterns-techniques-6130a116820) (reading, 10 min) — *Covers: Caching patterns, distributed cache, cache-aside, write-through, write-behind, TTL*
- **Distributed Caching for Scalability**: [Address Scalability Bottlenecks with Distributed Caching](https://learn.microsoft.com/en-us/archive/msdn-magazine/2010/june/msdn-magazine-soa-tips-address-scalability-bottlenecks-with-distributed-caching) (reading, 12 min) — *Covers: Distributed caching strategies, performance optimization, SOA patterns*

---

## Optional Module Materials

Optional module materials cover auto-scaling, stream processing technologies, performance optimization, and advanced techniques.
Please, check **[02-optional-materials.md](./02-optional-materials.md)** for technology-specific implementations.
