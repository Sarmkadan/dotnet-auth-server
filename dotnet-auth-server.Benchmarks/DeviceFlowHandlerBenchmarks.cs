[MemoryDiagnoser]
public class DeviceFlowHandlerBenchmarks
{
    [GlobalSetup]
    public void Setup()
    {
        // Set up realistic test data here
    }

    [Benchmark]
    public void Benchmark_Method1()
    {
        // Benchmark the first public method
    }

    [Benchmark]
    [Params(10)]
    public void Benchmark_Method2()
    {
        // Benchmark the second public method with input size 10
    }

    [Benchmark]
    [Params(100)]
    public void Benchmark_Method3()
    {
        // Benchmark the third public method with input size 100
    }
}
