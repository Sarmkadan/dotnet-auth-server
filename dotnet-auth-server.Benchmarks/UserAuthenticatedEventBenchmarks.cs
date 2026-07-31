using BenchmarkDotNet.Core.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Math;
using BenchmarkDotNet.Math.Statistics;
using BenchmarkDotNet.Engines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace dotnet_auth_server.Benchmarks
{
    [MemoryDiagnoser]
    public class UserAuthenticatedEventBenchmarks
    {
        [GlobalSetup]
        public void Setup()
        {
            // TODO: Set up test data here
        }

        [Benchmark]
        public void Benchmark_UserAuthenticatedEvent_Create()
        {
            // TODO: Create test data
            var userAuthenticatedEvent = new UserAuthenticatedEvent();
            // TODO: Call the method to benchmark
        }

        [Benchmark]
        [Params(10, 100, 1000)]
        public void Benchmark_UserAuthenticatedEvent_Create_WithParams()
        {
            // TODO: Create test data
            for (int i = 0; i < 1000; i++)
            {
                var userAuthenticatedEvent = new UserAuthenticatedEvent();
                // TODO: Call the method to benchmark
            }
        }

        [Benchmark]
        public void Benchmark_UserAuthenticatedEvent_Create_WithParams_MultipleTimes()
        {
            // TODO: Create test data
            for (int i = 0; i < 100; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    var userAuthenticatedEvent = new UserAuthenticatedEvent();
                    // TODO: Call the method to benchmark
                }
            }
        }
    }
}