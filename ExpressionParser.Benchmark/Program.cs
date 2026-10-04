using BenchmarkDotNet.Running;

using ExpressionParser.Benchmark;

var summary = BenchmarkRunner.Run<CommonBenchmark>();