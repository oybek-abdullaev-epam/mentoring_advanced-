_Questions for the self-check:_

# Domain understanding
1. In what ways is an Online Job Scheduler similar to a cron job, and what extra capabilities does it usually provide?

Online Job Scheduler is similar to cron job in a way that it is also timer-triggered with a declarative schedule.
But it also provides additional capabilities such as:
- reliability (retries, failure handling, persistence, concurrency control)
- availability (managed service which can run on multiple nodes)
- visibility (notifications, reporting / dashboard)

2. Give several examples of repetitive tasks that can be automated by a job scheduler.

- database backups
- reminder notifications
- polling external apis

3. Why is this module important before starting practical implementation tasks in the following modules?

As we are planning to build a highly available, scalable and reliable distributed system it is important to do a high level distributed system design.
And in order to do distributed system design we need to understand what are both functional and non-functional requirements of the system.

# Main entities and relationships
4. What is the difference between a Job and a JobExecution?

A Job is a definition of the work to be done, while JobExecution is one actual run of that job.

5. What important information should a Job contain so that the system can schedule and run it correctly?

A Job should define when to run, what to do as part of this job, some payload information, if it is standalone or not and so on.

6. How are Log records and Notification records different in purpose, and why does the system need both?

Log records are mainly used for debugging and monitoring purposes, observability.
Notification records are used to track which notifications have been sent to the users.
So they have different purposes, different audience and state.

# Use cases
7. How to test scheduling job is operating successfully on a staging environment? What is dry run?

On a staging env we can test the system using dry run technique.
Dry run is when you verify different scenarios of the system, such as scheduling, failure handling, success case and so on without actually causing any real side effects (like calling an external api, sending a real user notification and so on).
We can substitute those external apis with mock and verify against the mocks instead.

8. Why is it important for users to view execution logs and job history?

Being able to see execution logs and job history gives users observability of their jobs.
So they can confirm the success of the job, troubleshoot issues or audit the records later.

# Technical requirements and design thinking
9. Classify Technical Requirements from the materials to specific NFRs

Handle thousands of concurrent jobs and users, ensuring efficient execution and monitoring - Scalability
Ensure jobs are executed on time, even during peak loads or partial outages - Reliability
Provide real-time updates on job execution status and logs - Observability
Notify users immediately in case of job failures or errors - Observability
Support one-time jobs, recurring jobs, concurrent and singleton jobs, and test runs - Flexibility
Enable jobs to execute arbitrarily complex implementation via implementations bank - Extensibility
Ensure all data flows between applications are protected at transit and at rest (when required) - Security
Balance infrastructure costs while maintaining adequate performance - Cost efficiency

10. What does it mean for the system to support both concurrent and singleton jobs, and why can this distinction be important?

It is important for the system to support both concurrent and singleton jobs because some kind of tasks cannot overlap with each other.
This distinction is important because it adds additional requirements to our system, which will have to coordinate different runs of the singleton jobs across nodes and so on.

11. Why is real-time monitoring important for a scheduler platform, especially in high-load or business-critical scenarios?

It is especially important for a scheduler platform to have a real-time monitoring because without users will not get any information about jobs that hang or never fire or fail silently.
And it is crucial for monitoring to be real-time for high-load or business-critical scenarios because it guarantees fast detection of the problems and shorter time to recovery, which has a direct business impact.

12. Imagine that a scheduled API call fails because the target endpoint is temporarily unavailable. What information should be stored, what should be shown in logs, and what kind of notification should be sent?

We should store one log per attempt with an error message, status code and timestamp.
For a one-time transient failure that eventually succeeded we should probably not send a notification at all.
But if the number of retries are exhausted we should send a notification mentioning the time of the failure, error reason, number of retries and so on.
