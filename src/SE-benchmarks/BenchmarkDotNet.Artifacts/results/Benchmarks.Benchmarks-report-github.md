```

BenchmarkDotNet v0.15.2, Linux Fedora Linux 42 (Workstation Edition)
Intel Core i5-4300U CPU 1.90GHz (Haswell), 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.100
  [Host]     : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2 DEBUG
  DefaultJob : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2


```
| Method                      | Mean           | Error        | StdDev       |
|---------------------------- |---------------:|-------------:|-------------:|
| RegularArray                |     1,077.6 ns |     19.62 ns |     16.39 ns |
| ComputePrimitive            |     8,827.7 ns |    170.81 ns |    250.37 ns |
| ComputePrimitiveiSequential |       701.8 ns |      5.88 ns |      5.21 ns |
| ComputeRecord               |     8,772.1 ns |    100.07 ns |     78.13 ns |
| ComputeRecordSequential     |       623.6 ns |      5.97 ns |     12.06 ns |
| ComputeDURead               |     7,452.9 ns |     11.55 ns |     10.24 ns |
| ComputeDU                   |     7,471.9 ns |     17.07 ns |     15.13 ns |
| ComputeDUSequential         |       618.5 ns |      0.90 ns |      0.75 ns |
| CreateEntities              |   954,514.2 ns |  4,996.53 ns |  4,172.32 ns |
| SetEntities                 | 2,736,552.1 ns | 52,639.16 ns | 43,956.08 ns |
| RunContains                 |    88,262.5 ns |    167.59 ns |    148.57 ns |
