# Home Task: Measure System Scalability with Load Testing

## Metrics Collected

| Concurrent Users | Total Requests | Avg Latency (ms) | p95 (ms) | p99 (ms) | RPS | Failed Requests |
|---|---|---|---|---|---|---|
| 1 | 14,471 | 2.32 | 0.41 | 0.99 | 482.40 | 0 |
| 10 | 14,992 | 20.71 | 1.52 | 3.50 | 499.70 | 0 |
| 50 | 12,272 | 131.64 | 5.89 | 4,784.13 | 409.10 | 0 |
| 100 | 12,240 | 243.09 | 3,061.76 | 4,468.73 | 408.00 | 0 |
| 500 | 13,472 | 1,111.13 | 4,562.94 | 4,612.10 | 449.10 | 0 |

## NFR Mapping

### Scalability
- RPS / Concurrent Users: we can observe that RPS did not grow, but instead even dropped slightly when the number of concurrent users went up. It means that our server can handle maximum 400-500 RPS regardless of the number of real requests, and is not able to scale depending on the load
- Avg Latency: increased steeply with the growing number of concurrent users, which means that our server was not able to proccess all the incoming requests and put them in queue, making users to wait for the response
- p95 (ms) and p99 (ms): also grew steeply, and with 500 concurrent users p95 was almost equal to p99, which means that not 1% but more than 5% of users experienced major delays in response

Overall, from these metrics we can see that our system is not scalable under the load

### Availability
- Failed Requests: we had 0 failed requests at all times, which indicates that the system was highly available

### Reliability
- Failed Requests: we had 0 failed requests, no loss of data observerd
- p95 (ms) vs p99 (ms): at some point (50 concurrent users) system started to be unpredictable, because most of the users were able to get the response in ~5ms, but small portion of them had to wait >4s

Overall, we could not observer any reliability issues in terms of data loss, but we could observe that system started to become unpredictable due to latency issues

## Desing recommendations

### Horizontal scaling
In order for the system to serve 500 concurrent users gracefully, we should have a horizontal scaling. The idea is that we are able to spin new identical servers automatically based on the number of requests, and balance the load across those instances so that requests do not sit in the queue.

### Caching
Another thing that might help is to add caching. This might be both distributed caching (in case we have many instances) and in-memory caching. This would help not to perform long-running operations such as i/o, database reads or in-memory computations for each request, but return data from cached immediately instead
