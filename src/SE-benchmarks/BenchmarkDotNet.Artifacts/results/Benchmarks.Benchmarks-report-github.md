```

BenchmarkDotNet v0.15.2, Linux Fedora Linux 42 (Workstation Edition)
Intel Core i5-4300U CPU 1.90GHz (Haswell), 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.100
  [Host]     : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2 DEBUG
  DefaultJob : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2


```
| Method         | size | Mean         | Error      | StdDev     |
|--------------- |----- |-------------:|-----------:|-----------:|
| **CreateEntities** | **10**   |     **9.798 μs** |  **0.0998 μs** |  **0.0885 μs** |
| SetEntities    | 10   |    27.276 μs |  0.4289 μs |  0.3802 μs |
| Queries        | 10   | 1,235.486 μs | 15.9481 μs | 14.9178 μs |
| **CreateEntities** | **100**  |    **87.342 μs** |  **1.6833 μs** |  **2.0038 μs** |
| SetEntities    | 100  |   259.536 μs |  4.8254 μs |  7.6535 μs |
| Queries        | 100  | 1,277.798 μs | 11.5529 μs | 15.4228 μs |
| **CreateEntities** | **1000** |   **860.531 μs** | **16.7782 μs** | **20.6051 μs** |
| SetEntities    | 1000 | 2,562.333 μs | 45.3661 μs | 58.9888 μs |
| Queries        | 1000 | 1,265.296 μs | 10.4206 μs |  9.2376 μs |
