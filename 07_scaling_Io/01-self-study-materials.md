# Self-Study Materials

## Data Storage and Transmission Format

### Overview of Most Popular Storage Formats

1. [Introduction to Avro (reading)](https://avro.apache.org/docs/current/)  
   Avro is a row-based binary serialization format designed for Hadoop and event streaming. Covers schema evolution, compact binary encoding, and when row-oriented storage outperforms columnar alternatives.

2. [Mastering Protocol Buffers (reading)](https://protobuf.dev/overview/)  
   Official Protocol Buffers documentation covering schema definition in `.proto` files, field numbering, backward compatibility guarantees, and code generation. Essential reading before implementing Protobuf in the home task.

3. [Understanding Parquet (reading)](https://coralogix.com/blog/parquet-file-format/)  
   Explains columnar storage, predicate pushdown, and why Parquet is preferred for analytics workloads over row-based formats like JSON or Avro. Helps distinguish when to choose columnar vs. row storage.

4. [Apache Thrift Tutorial (reading)](https://thrift.apache.org/)  
   Introduction to Thrift's IDL-based approach to cross-language serialization. Useful for comparing the design tradeoffs between Thrift, Protobuf, and Avro in polyglot service environments.

5. [Understanding CSV and TSV Formats (reading)](https://github.com/eBay/tsv-utils/blob/master/docs/comparing-tsv-and-csv.md)  
   Practical comparison of CSV and TSV covering delimiter conflicts, quoting rules, and performance at scale. Relevant for understanding why text-based formats are unsuitable for high-throughput APIs.

## Network IO

### HTTP/2 HTTP/3
1. [HTTP/2 Overview (reading)](https://developers.google.com/web/fundamentals/performance/http2)  
   Covers multiplexing, header compression (HPACK), and server push — the key mechanisms that reduce latency compared to HTTP/1.1. Understanding these mechanisms is foundational for reasoning about network I/O throughput.

2. [HTTP/3 Overview (reading)](https://www.cloudflare.com/learning/performance/what-is-http3/)  
   Explains how HTTP/3 replaces TCP with QUIC to eliminate head-of-line blocking and reduce connection establishment overhead. Relevant when optimising high-concurrency API endpoints.


### gRPC
1. [Introduction to gRPC (reading)](https://grpc.io/docs/what-is-grpc/introduction/)  
   Introduces gRPC's service-definition model, bidirectional streaming, and its default use of Protocol Buffers for payload encoding. Provides the context needed to understand why Protobuf is the dominant binary format in modern microservice communication.

## Storage IO

### Storage Devices (HDD, SSD, NVMe)
1. [SSD vs. HDD: What's the Difference? (reading)](https://aws.amazon.com/compare/the-difference-between-ssd-hard-drive/)  
   Compares sequential vs. random read/write performance of HDD, SSD, and NVMe devices including latency orders of magnitude. Provides the hardware baseline for understanding why serialization payload size directly affects end-to-end I/O time.

### RAID Configurations
1. [RAID and RAIDZ (reading)](https://www.45drives.com/community/articles/RAID-and-RAIDZ/)  
   Explains RAID-0 through RAID-6 and the ZFS-native RAIDZ variant, covering redundancy, write amplification, and rebuild costs. Useful background for reasoning about storage I/O bottlenecks in distributed systems.

2. [XFS vs ZFS vs Linux Raid (reading)](https://blog.servermania.com/xfs-vs-zfs-linux-raid)  
   Practical comparison of filesystem choices from a performance and reliability standpoint. Helps contextualise storage-layer decisions that influence how efficiently serialized payloads are written and read.

## Serialization

### Serialization Formats
1. [JSON vs XML: Which One is Better? (reading)](https://aws.amazon.com/compare/the-difference-between-json-xml/)  
   Compares JSON and XML on verbosity, schema enforcement, parsing cost, and ecosystem support. Sets the baseline human-readable format context before evaluating binary alternatives like Protobuf.

### Protocol Buffers
1. [Protocol Buffers Language Guide (reading)](https://protobuf.dev/programming-guides/proto3/)  
   The definitive reference for proto3 syntax: field types, field numbers, reserved fields, and backward-compatible schema evolution rules. Required reading before implementing Protobuf serialization in the home task.

## Compression

### Compression Algorithms
1. [Understanding Compression: gzip, brotli, and More (reading)](https://developer.mozilla.org/en-US/docs/Web/HTTP/Compression)  
   MDN reference covering HTTP `Content-Encoding`, when the server applies gzip/Brotli/deflate, and the browser negotiation flow. Relevant for understanding the compression layer that operates on top of any serialization format.

2. [This is Brotli from Origin (reading)](https://blog.cloudflare.com/this-is-brotli-from-origin)  
   Cloudflare's explanation of how Brotli achieves better compression ratios than gzip for text payloads at equivalent CPU cost. Useful for deciding whether HTTP-layer compression supplements or replaces binary serialization.

3. [LZ4 Algorithm (reading)](https://github.com/lz4/lz4)  
   LZ4 prioritises decompression speed over compression ratio — typically 4–5× faster than gzip at similar compression levels. Relevant when real-time throughput matters more than wire-size reduction.
