## Distributed Data: Consistency and Consensus

The goal of this task is to enhance the Data Access Layer from Modules 04_02 and 04_03 with consistency-aware features, and implement Presentation Layer (PL) and Business Logic (BL) that reflect consistency requirements derived from use case needs. The consistency model for each component should be justified by its requirements — not set as an arbitrary configuration parameter.

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

### 1. Consistency Requirements Analysis

Before updating the DAL, analyze and document the consistency requirements for each use case:

- How critical is data consistency for a concrete use-case? Are we ok with short replication lag? Why?
- What type of consistency is required for a concrete use-case: Strong, Eventual or ReadAfterWrite?

> **Key principle:** The consistency model is a consequence of requirements - it should not be an arbitrary setting. Strong consistency is applied where data correctness and freshness are business-critical. Eventual consistency is applied where availability and read performance take priority. ReadAfterWrite consistency can be applied where a user must immediately see the results of their own writes, but global strong consistency is not required.

---

### 2. Update DAL to Support Consistency-Aware Data Access

Enhance your existing Data Access Layer from *'Module 04_03'* to implement consistency models as derived from the requirements analysis above:

**1) Define consistency level enum:**

```csharp
public enum ConsistencyLevel
{
    Strong,
    Eventual,
    ReadAfterWrite // Optional for implementing
}
```
- **Strong**: Route to primary/leader connection string or use database-native strong consistency
- **Eventual**: Round-robin or random selection from replica connection strings, or use database-native eventual consistency
- **ReadAfterWrite**: Use sticky connection or track last write timestamp

**2) Update DAL repository methods to use the appropriate consistency level based on requirements:**

```csharp
// Example for Job repository

Task<Job> GetJobByIdAsync(Guid id, ConsistencyLevel consistencyLevel);
Task<IEnumerable<Job>> GetJobsAsync(ConsistencyLevel consistencyLevel);
```

**IMPORTANT:** Use database-native consistency possibilities where available.

<details>
<summary><i>Note</i>: For <i>ReadAfterWrite</i> one can use the <i>'Caching + Cool Down'</i> approach. Expand for an example.</summary>

```csharp
public class ConsistencyManager
{
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cooldownPeriod = TimeSpan.FromSeconds(5);
    
    private const string CacheKeyPrefix = "UserLastWrite_";

    public ConsistencyManager(IMemoryCache cache)
    {
        _cache = cache;
    }

    /// <summary>
    /// Records that a write occurred for the user and starts the cooldown.
    /// </summary>
    public void TrackWrite(string userId)
    {
        var cacheKey = GetCacheKey(userId);
        _cache.Set(cacheKey, DateTime.UtcNow, _cooldownPeriod);
    }

    /// <summary>
    /// Determines if the user is within the cooldown period and needs a consistent read.
    /// </summary>
    public bool IsReadAfterWriteApplicable(string userId)
    {
        if (_cache.TryGetValue(GetCacheKey(userId), out DateTime lastWriteTime))
        {
            return (DateTime.UtcNow - lastWriteTime) < _cooldownPeriod;
        }

        return false;
    }

    private string GetCacheKey(string userId) => $"{CacheKeyPrefix}{userId}";
}
```
</details>

### 3. Implement Business Logic and Presentation Layer

Create Business Logic (BL) and Presentation Layer (PL) that reflect the consistency requirements identified in Section 1. The BL layer should encapsulate the consistency decisions - API consumers should not need to specify or be aware of consistency levels.

**IMPORTANT:** In the scope of the current module a mentee is to implement **only APIs/Lambdas/Functions** of the JobManager (UC1.1) and JobOrchestrator (UC2.1). **NO** HTTP/Time triggers and **NO** communication between services (e.g., message brokers, database triggers, etc.) are to be implemented. There are out-of-scope and to be implemented in the next module.
For Presentation Layer (PL) one is to use either Swagger or direct API calls (via curl, Postman, etc.). **NO** UI implementation is required.

The implementation is to be tested locally or with low-effort deploy:
- Local API Testing: [Swagger](https://learn.microsoft.com/en-us/aspnet/core/tutorials/getting-started-with-swashbuckle?view=aspnetcore-8.0&tabs=visual-studio)
- Local AWS Lambdas Testing: [Testing .NET AWS Lambda locally](https://medium.com/@f.sazanavets/the-easiest-way-to-run-and-debug-net-aws-lambda-locally-a161285bc3b4)
- Local Azure Functions Testing: [Testing .NET Azure Functions locally](https://learn.microsoft.com/en-us/azure/azure-functions/functions-develop-local?pivots=programming-language-csharp)

### 4. Demonstrate the Implementation

Demonstrate that the implementation works correctly:
- Add debug logs tracking which database replica was used for each operation/endpoint;
- Show that different use-cases use the defined consistency type;
- Attach execution logs to the final solution demonstrating consistency behavior;

---

### Scoreboard Criteria:
- **1-30 points:** Consistency requirements are analyzed and documented per use case with clear justification. 
- **31-60 points:** DAL is updated with consistency-aware routing logic derived from those requirements. Business Logic and Presentation Layer are implemented with consistency decisions encapsulated in the BL layer. 
- **61-100 points:** Implementation is demonstrated with debug logs tracking database replica usage per operation/endpoint. Execution logs are attached showing different consistency behavior.