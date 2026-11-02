```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26200.9711)
AMD Ryzen 7 9700X 3.80GHz, 1 CPU, 16 logical and 8 physical cores
  [Host]     : .NET Framework 4.8.1 (4.8.9363.0), X64 RyuJIT VectorSize=256
  DefaultJob : .NET Framework 4.8.1 (4.8.9363.0), X64 RyuJIT VectorSize=256


```
| Method         | Mean     | Error   | StdDev  |
|--------------- |---------:|--------:|--------:|
| Transform128   | 351.7 μs | 6.99 μs | 7.77 μs |
| TransformEx128 | 353.7 μs | 6.19 μs | 6.08 μs |
