## Application Scaling - Batch and Stream Processing

This task focuses on designing and implementing batch and/or stream processing solutions for distributed system components. Building upon the Data Access Layer developed in Modules 04_02-04_04, you will analyze processing requirements, select appropriate processing models, design messaging infrastructure, and implement event-driven data processing pipelines.

---

## Home Task

**Prerequisites** (choose one of the following options):

1. Containerization Tools
- [Rancher Desktop](https://rancherdesktop.io/)
- Docker CLI (*Note: Docker Desktop requires a paid subscription*)
  - [How to use Docker without Docker Desktop on Windows 10 or 11](https://blog.novacare.no/how-to-use-docker-without-docker-desktop-on-windows-10-or-11/)
  - [Installing Docker on Windows without Docker Desktop](https://gist.github.com/rwcitek/f2b6fb5baddf16f6dc1f277a5d9f8366)

2. Cloud Subscription
- [AWS Free Tier](https://aws.amazon.com/free/)
- [Azure EPAM Subscription](https://kb.epam.com/spaces/EMSFTCC/pages/1333374937/Software+Packages+with+Azure+Subscriptions+50)

**Scope of work:**
- `UC 1.1: Create a New Job`
- `UC 2.1: Execute a Job At a Scheduled Time`

Please refer to [Online Job Scheduler Domain Entities](https://git.epam.com/epmc-msft/net-mentoring-programs/-/blob/main/advanced+/02_practical_cases_domain_intro/01-self-study-materials.md?ref_type=heads#3-online-job-scheduler-domain-entities) and [Conceptual Solution Components Diagram](https://git.epam.com/epmc-msft/net-mentoring-programs/-/blob/main/advanced+/02_practical_cases_domain_intro/01-self-study-materials.md?ref_type=heads#5-conceptual-solution-components-diagram:~:text=5.%20Conceptual%20Solution%20Components%20Diagram).

---

### 1. Batching/Streaming Strategy Decision

Prepare a comprehensive document describing your batch and/or stream processing strategy for each use case:
- What type of processing (real-time, batch, or hybrid) is most appropriate for this use case? Justify your choice.
- *For stream processing:* What events will be published? What are the event schemas and their purposes? *For batch processing:* What triggers will initiate processing? How frequently will batches be executed?
- What event/message consumer services are required to support this use case?
- What messaging infrastructure (message broker or event streaming platform) is required? Justify your selection and describe the configuration (topics/queues, partitions, consumer groups).

### 2. Setup Events/Messaging Infrastructure

Set up the message broker or event streaming platform based on your strategy decision:
- Deploy the selected messaging infrastructure (locally using containerization tools or in the cloud)
- Configure topics/queues for your events with appropriate settings
- Document the configuration and attach screenshots or configuration files to the project

### 3. Implement Batching/Streaming Processing

Implement the batching/streaming processing solution for both use cases:
- Develop the required consumer services to process events/messages for each use case
- Deploy the `JobManager` and `JobOrchestrator` services along with consumer services (locally using containerization tools or in the cloud)
- Implement HTTP/Time triggers (e.g. database triggers, scheduled triggers, etc.) using previously defined architecture (a diagram from Module 3);
- Implement communication between services (e.g., by message brokers) using previously defined architecture (a diagram from Module 3);
- *[Optional]* Implement the Dead Letter Queue pattern and retry mechanism for robust event/message processing

### 4. Demonstrate the Use-Cases Implementation

Demonstrate that your batching/streaming implementation works correctly for both use cases. Show how the system handles both use cases with your chosen processing model(s)

---

### Scoreboard Criteria:
- **1-59 points:** Batching/Streaming Strategy Decision document is complete with thorough requirements analysis, well-justified processing model selection (batch/stream/hybrid), appropriate messaging infrastructure choice, and comprehensive event-driven architecture design.
- **60-100 points:** Batching/Streaming processing is successfully implemented for both use cases with proper event publishing and consumption, robust error handling, and seamless integration with the existing DAL from previous modules. Complete demonstration of the batching/streaming implementation for both use cases.