# job-scheduler — conventions for Claude

## Code comments
Write comments in code only in these cases:
- **Non-obvious trade-offs and workarounds** — explain why a hack, workaround or non-idiomatic pattern was chosen (e.g. working around a third-party library bug or a legacy system constraint).
- **Complex algorithms and business rules** — document the mathematical logic, domain rules or edge cases that cannot be expressed through clean variable or function names alone.

Do not add XML doc comments (`/// <summary>`) to every public type or member, and do not write comments that restate what the code already says. Use short plain `//` comments.
