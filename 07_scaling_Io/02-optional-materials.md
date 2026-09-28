# Optional Materials

### Using Compression in .NET
- [Using Compression in .NET](https://learn.microsoft.com/en-us/dotnet/standard/io/how-to-compress-and-extract-files)  
  Step-by-step guide to `GZipStream`, `BrotliStream`, and `ZipArchive` in .NET. Useful for implementing response compression in your UC 2.3 API on top of the serialization format chosen in the home task.
- [Response compression in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/performance/response-compression?view=aspnetcore-10.0)
  This article provides guidance on implementing response compression in ASP.NET Core applications to enhance performance by reducing response sizes, which can significantly improve app responsiveness.

### ORC file format
- [ORC file format](https://medium.com/data-and-beyond/exploring-the-orc-file-format-advantages-use-cases-and-best-practices-for-data-storage-and-79c607ee9289)  
  Deep-dive into Apache ORC's columnar storage design, stripe-level statistics, and predicate pushdown. Complements the Parquet reading from self-study materials — together they explain when columnar formats outperform row-based alternatives for analytics on job execution history data.
