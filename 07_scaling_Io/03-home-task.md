### Home Task: Implement UC 2.3 with Optimized Serialization

#### Task Overview:
The goal of this task is to implement the functionality for viewing job execution history within the application. The solution should enable users to access and review historical job execution data efficiently — **and support 1,000 concurrent downloaders** by minimizing payload size.

---

### Steps to Complete the Task:

#### 1. **View job execution history use-case implementation (Optimized for Scale)**:
Create services and application logic for the following use cases:
- `UC 2.3: View Job Execution History`

**Constraint**: The API must support **1,000 concurrent downloaders** without degrading performance significantly. Minimize payload size using an appropriate serialization format.

**Requirements**:
1. Implement the history API in **JSON** (baseline) inside your JobOrchestrator service/API
2. Implement the same API endpoint/service in **Protobuf**
3. Measure payload size and serialization performance for both formats in your implementation
4. Choose the format for production use and justify your decision with concrete numbers. Provide the screenshot of measurements or `docs/comparison-report.md` document

> **Measurement hint**: A pre-built benchmark tool is available at [07-serialization-comparison/](./07-serialization-comparison/).  
> Run it (`dotnet run --configuration Release` inside `07-serialization-comparison/src`) to get format comparison numbers for `JobHistoryRecord` payloads at 100 / 500 / 1,000 records.  
> Use those numbers as a **reference baseline** when measuring your own implementation and filling in `07-serialization-comparison/docs/comparison-report.md`.

---

### Scoreboard Criteria:

1. **1-59:** Written self-check answers were provided; the answers are concise but complete.
2. **60-89:** Both JSON and Protobuf implementations are complete in your application code and `docs/comparison-report.md` is filled in with measured payload sizes and serialization timings.
3. **90-100:** Format choice is explicitly justified for the 1,000 concurrent downloaders constraint using concrete numbers from your measurements (not opinion).