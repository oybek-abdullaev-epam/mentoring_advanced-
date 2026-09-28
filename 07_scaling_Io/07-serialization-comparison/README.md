# Benchmark Tool: Serialization Format Comparison (JSON vs Protobuf)

**Module**: 07 – IO Layer Scaling  
**Purpose**: Measurement reference for the home task — not a standalone deliverable  
**Time to run**: ~5 seconds (quick mode) or ~2 minutes (full BenchmarkDotNet)

---

## Overview

This tool provides pre-built JSON and Protobuf implementations for the `UC 2.3: View Job Execution History` payload schema. Run it to get concrete payload size and serialization performance numbers, then use those numbers as a **reference baseline** when:
- Choosing the serialization format for your own UC 2.3 implementation
- Filling in `docs/comparison-report.md` with measured values
- Justifying your format decision in the home task

> **This is a measurement helper, not the deliverable.** Your home task requires implementing JSON and Protobuf in your own application code. Use the output of this tool to cross-check and support your measurements.

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

---

## Running the Comparison

```bash
cd 07-serialization-comparison/src

# Quick comparison (plain console output, ~5 seconds)
dotnet run --configuration Release

# Full BenchmarkDotNet analysis (production-grade, ~2 minutes)
dotnet run --configuration Release -- --benchmark
```

---

## Example Output

```
=== Serialization Format Comparison ===

| Format    | Records | Payload (bytes) | Serialize (ms) | Deserialize (ms) | Size Ratio        |
|-----------|---------|-----------------|----------------|------------------|-------------------|
| JSON      |     100 |          14,823 |           0.48 |             1.12 | 1.0x (baseline)   |
| Protobuf  |     100 |           3,941 |           0.21 |             0.38 | 3.8x larger JSON  |
|           |         |                 |                |                  |                   |
| JSON      |   1,000 |         148,412 |           4.82 |            11.23 | 1.0x (baseline)   |
| Protobuf  |   1,000 |          39,287 |           2.14 |             3.87 | 3.8x larger JSON  |
```

---

## Analysis: Format Choice for 1,000 Concurrent Downloaders

| Factor | JSON | Protobuf |
|--------|------|----------|
| Payload size | ~148 KB / 1K records | ~39 KB / 1K records (~74% smaller) |
| Serialization time | ~4.8 ms | ~2.1 ms (~56% faster) |
| Deserialization time | ~11.2 ms | ~3.9 ms (~65% faster) |
| Human readability | ✓ Easy (debug-friendly) | ✗ Binary (need schema) |
| Schema requirement | None (flexible) | `.proto` schema required |
| Language support | Universal | Major languages only |
| Bandwidth at 1K users (1K records each) | ~148 MB/s peak | ~39 MB/s peak (~74% reduction) |

### Decision Framework

| Use Case | Recommended Format |
|----------|--------------------|
| Internal API, performance-critical | **Protobuf** |
| Public API, developer-friendly | JSON |
| Logs / analytics / storage | Parquet or Avro |
| Mobile clients, bandwidth-constrained | **Protobuf** |
| Debugging / monitoring endpoints | JSON |

For `UC 2.3: View Job Execution History` with **1,000 concurrent downloaders**:  
→ **Protobuf** — 74% bandwidth reduction directly reduces infrastructure cost and improves latency under concurrency.

---

## Adapting to Your Schema

If your UC 2.3 implementation uses different fields than the default `JobHistoryRecord`, modify `src/Models.cs` to match your schema and re-run. The benchmark engine will automatically adapt and you can use the updated numbers in your comparison report.

---

## Files in this Lab

```
07-serialization-comparison/
├── README.md                           # This file
├── docs/
│   └── comparison-report.md           # Template for student report
└── src/
    ├── SerializationBenchmark.csproj
    ├── Models.cs                       # JobHistoryRecord + TestDataGenerator
    ├── JsonSerializer.cs               # System.Text.Json baseline
    ├── ProtobufSerializer.cs           # protobuf-net implementation
    └── Benchmark.cs                    # BenchmarkDotNet + quick comparison
```
