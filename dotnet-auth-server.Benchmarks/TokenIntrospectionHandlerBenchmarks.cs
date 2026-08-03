[MemoryDiagnoser]
public class TokenIntrospectionHandlerBenchmarks
{
    [GlobalSetup]
    public void Setup()
    {
        // setup test data
    }

    [Benchmark]
    public void BenchmarkMethod1()
    {
        // test method 1
    }

    [Benchmark]
    [Params(10, 100, 1000)]
    public void BenchmarkMethod2(int inputSize)
    {
        // test method 2 with input size
    }

    [Benchmark]
    public void BenchmarkMethod3()
    {
        // test method 3
    }
}