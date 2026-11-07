_Questions for the self-check:_

Data Layer Capabilities
1. What alternatives exist for row-based databases? What does the term NoSQL mean?

The term NoSQL usually refers to "Not only SQL" and means any non-relational database.
NoSQL databases are alternatives for row-based databases, such as:
- Columnar - store data column by column
- Key-value - store data as key-value pairs
- Document - store data as json documents with a flexible schema
- Wide-column - store data as column-families having different sets of columns
- Graph - store data as nodes and edges of a graph
and so on (time-series, vectors, etc.)

2. What are the main differences between a Database, Data Warehouse, and Data Lake?

- Database is what actually runs the application, meaning that it accepts many reads and writes from users, usually used in OLTP manner
- Data Warehouse is used for reporting and analytics over the history of events. Mainly used by business analysts, not application users, usuall in OLAP manner
- Data Lake is usually used for ML-like analytics. Compared to Data Warehouse, it accepts raw data that can be structured, semi-structured or even unstructured, thus writes are faster and schema not defined

3. How can you determine if your data is structured, semi-structured, or unstructured?
- Structured - if data has a fixed schema, all records have the same fields, all columns are strongly typed
- Semi-structured - no fixed schema, records can differ, but they carry their own structure (by having json keys for example)
- Unstructured - no structure a database can use, for example images, videos, free text, and so on

Application Layer Capabilities
1. What are the main characteristics of a Service-Oriented Architecture?

Main characteristics of SOA are:
- system is split into services that each provide a business capability with a well-defined contract, and designed to be shared across implementation
- Consumers depend only on the contract, not implementation
- Enterprise Service Bus usually handels routing the communication

2. Can you explain the difference between Message-Driven and Event-Driven architectures?

The main difference between message-driven and event-drive is that:
- in a message-driven architecture a message is sent to a specific recipient. It is usually a command or request sender expects that recipient to process. Senders and consumers are coupled
- in an event-driven architecture an event is a fact about something that happened, published without knowing who will react. So any number of consumers can subscribe to events. Senders and consumers are not coupled

3. What are the benefits of Asynchronous Message-Based Communication in a microservices architecture?

Benefits of async message-based communication in microservices are:
- Loose coupling in time - senders does not have to wait for the consumers reply
- Backpressure - queues absorb spikes
- Scaling - consumers can scale based on queue length
and so on

4. What is a Serverless Architecture and what are its advantages and disadvantages?

Serverless Architecture is when a cloud provider runs and scales the infrastructure, and you pay per execution or usage
Advantages: no servers to manage, automatic scaling, scaling to zero, pay for usage and so on
Disadvantages: cold starts, vendor lock-in, less control, and so on

5. How does a Hybrid Architecture leverage the strengths of different architectural styles?

Hybrid Architecture leverages the strengths of different architectural styles by using different styles for different parts of the system, each where it fits best.
For example, refactoring a monolith by gradually breaking down into microservices

Infrastructure Layer Capabilities
1. What are the key components of web infrastructure design?

Key components of web infrastructure design are:
DNS - resolve names, can do geo-routing
CDN - cache static content
WAF - filter malicious traffic
Load balancers and reverse proxies - spread traffic
Gateway - auth, rate limiting, routing
Caching - distributed or in-memory
Data tier - databases, object storage and etc
Messaging - queues and event buses
Observability - logs, metrics, traces
Security - networking, private endpoints and etc
and so on

2. What differs HTTP 3.0 from earlier versions?

Main difference of HTTP 3.0 is that it runs over QUIC on UDP instead of TCP in the earlier versions.
As a result, it gives no transport-level head-of-line blocking, faster connection setup, header compression and overall better performance.
But not all the infrastructure is ready for it yet.

3. What are the roles of a Load Balancer, a Reverse Proxy, and an API Gateway in a web infrastructure? How do they differ from each other?

- Load balancer - spreads incoming traffic across multiple instance of the same service
- Reverse proxy - routes requests to different backends, plus does caching, compression and so on
- API Gateway - handles auth, applies policies such as rate limiting and quotas, might do request and response transformations and so on

4. What is the purpose of DNS Load Balancing and how does it work?

DNS Load balancing spreads traffic across multiple endpoints at name resolution time.
DNS server returns different IP adresses for the same hostname using some policy (round-robin, nearest region and so on).
It works best for global distribution across regions.

5. What role does a CDN play in load balancing?

CDN helps to offload the origin by caching static content and cacheable API responses. These content is served from edge locations, so most requests never even reach the backend.

6. What is the function of a Web Application Firewall (WAF) in a web infrastructure?

WAF's main function in a web infrastructure is to inspect traffic and block malicious requests before they reach the applications.
It might help to protect from xss or sql injection attacks, apply some rules such as ip or geo filtering, perform bot protection and rate limiting and etc.

7. How does an API Gateway differ from a Load Balancer and a Reverse Proxy?

- Load balancer - spreads incoming traffic across multiple instance of the same service
- Reverse proxy - routes requests to different backends, plus does caching, compression and so on
- API Gateway - handles auth, applies policies such as rate limiting and quotas, might do request and response transformations and so on
