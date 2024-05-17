using BenchmarkDotNet;
using BenchmarkDotNet.Attributes;
using System;

namespace BenchmarkSuite1
{
    [Config(typeof(BenchmarkConfig))]
    public class Benchmarks
    {
        [Benchmark]
        public void Scenario1()
        {
            // Implement your benchmark here
        }

        [Benchmark]
        public void Scenario2()
        {
            // Implement your benchmark here
        }
    }
}
