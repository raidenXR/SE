```

BenchmarkDotNet v0.15.2, Linux Fedora Linux 42 (Workstation Edition)
Intel Core i5-4300U CPU 1.90GHz (Haswell), 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.100
  [Host]     : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2 DEBUG
  DefaultJob : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2


```
| Method         | size  | Mean | Error |
|--------------- |------ |-----:|------:|
| **CreateEntities** | **100**   |   **NA** |    **NA** |
| RunSystems     | 100   |   NA |    NA |
| **CreateEntities** | **1000**  |   **NA** |    **NA** |
| RunSystems     | 1000  |   NA |    NA |
| **CreateEntities** | **10000** |   **NA** |    **NA** |
| RunSystems     | 10000 |   NA |    NA |

Benchmarks with issues:
  Benchmarks.CreateEntities: DefaultJob [size=100]
  Benchmarks.RunSystems: DefaultJob [size=100]
  Benchmarks.CreateEntities: DefaultJob [size=1000]
  Benchmarks.RunSystems: DefaultJob [size=1000]
  Benchmarks.CreateEntities: DefaultJob [size=10000]
  Benchmarks.RunSystems: DefaultJob [size=10000]
