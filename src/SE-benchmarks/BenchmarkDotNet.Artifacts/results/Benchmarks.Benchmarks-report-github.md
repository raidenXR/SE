```

BenchmarkDotNet v0.15.2, Linux Fedora Linux 42 (Workstation Edition)
Intel Core i5-4300U CPU 1.90GHz (Haswell), 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.100
  [Host]     : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2 DEBUG
  DefaultJob : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2


```
| Method              | Mean     | Error    | StdDev   |
|-------------------- |---------:|---------:|---------:|
| components_as_span  | 144.7 ns |  2.01 ns |  1.68 ns |
| components_contains | 127.8 ns |  1.44 ns |  1.28 ns |
| components_get_item | 132.1 ns |  0.92 ns |  0.82 ns |
| relation_has_out    | 481.6 ns |  8.05 ns |  7.53 ns |
| relation_has_in     | 467.9 ns |  3.87 ns |  3.43 ns |
| relation_get_item   | 765.1 ns | 15.28 ns | 42.59 ns |
