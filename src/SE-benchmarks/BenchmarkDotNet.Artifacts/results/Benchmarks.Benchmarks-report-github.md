```

BenchmarkDotNet v0.15.2, Linux Fedora Linux 42 (Workstation Edition)
Intel Core i5-4300U CPU 1.90GHz (Haswell), 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.100
  [Host]     : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2 DEBUG
  DefaultJob : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2


```
| Method                      | Mean           | Error        | StdDev       |
|---------------------------- |---------------:|-------------:|-------------:|
| RegularArray                |       946.2 ns |      1.28 ns |      1.07 ns |
| ComputePrimitive            |     7,483.1 ns |     41.99 ns |     39.28 ns |
| ComputePrimitiveiSequential |       615.5 ns |      0.85 ns |      0.75 ns |
| ComputeRecord               |     7,528.6 ns |     14.69 ns |     13.02 ns |
| ComputeRecordSequential     |       619.6 ns |      0.59 ns |      0.49 ns |
| ComputeDURead               |     7,482.9 ns |     36.26 ns |     33.92 ns |
| ComputeDU                   |     7,468.7 ns |     12.53 ns |     11.10 ns |
| ComputeDUSequential         |       621.9 ns |      4.87 ns |      4.55 ns |
| CreateEntities              |   945,515.5 ns |  6,182.81 ns |  5,162.92 ns |
| SetEntities                 | 2,722,765.7 ns | 38,735.14 ns | 30,241.85 ns |
