```

BenchmarkDotNet v0.15.2, Linux Fedora Linux 42 (Workstation Edition)
Intel Core i5-4300U CPU 1.90GHz (Haswell), 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.100
  [Host]     : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2 DEBUG
  DefaultJob : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2


```
| Method              | Mean     | Error   | StdDev   | Median   |
|-------------------- |---------:|--------:|---------:|---------:|
| components_as_span  | 146.0 ns | 2.96 ns |  6.68 ns | 143.1 ns |
| components_contains | 130.5 ns | 1.81 ns |  1.60 ns | 130.2 ns |
| components_get_item | 130.9 ns | 1.39 ns |  1.30 ns | 130.7 ns |
| relation_has_out    | 248.0 ns | 1.39 ns |  1.08 ns | 247.9 ns |
| relation_has_in     | 250.5 ns | 4.82 ns |  4.73 ns | 248.8 ns |
| relation_get_item   | 297.6 ns | 6.00 ns | 12.26 ns | 294.5 ns |
