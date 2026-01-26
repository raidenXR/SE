```

BenchmarkDotNet v0.15.2, Linux Fedora Linux 42 (Workstation Edition)
Intel Core i5-4300U CPU 1.90GHz (Haswell), 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.100
  [Host]     : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2 DEBUG
  DefaultJob : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2


```
| Method              | Mean     | Error   | StdDev  |
|-------------------- |---------:|--------:|--------:|
| components_as_span  | 141.7 ns | 0.46 ns | 0.39 ns |
| components_contains | 129.9 ns | 1.65 ns | 1.46 ns |
| components_get_item | 128.0 ns | 1.52 ns | 1.42 ns |
| relation_has_out    | 247.8 ns | 2.27 ns | 2.13 ns |
| relation_has_in     | 245.1 ns | 1.67 ns | 1.39 ns |
| relation_get_item   | 292.3 ns | 3.34 ns | 2.79 ns |
