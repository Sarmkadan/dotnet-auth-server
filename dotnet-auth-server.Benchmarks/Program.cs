using BenchmarkDotNet.Running;
using DotnetAuthServer.Benchmarks;

Console.WriteLine("dotnet-auth-server Benchmarks");
Console.WriteLine("============================");
Console.WriteLine();

Console.WriteLine("Available benchmarks:");
Console.WriteLine("1. TokenBenchmarks - Token issuance and validation operations");
Console.WriteLine("2. PkceBenchmarks - PKCE code challenge validation");
Console.WriteLine("3. TokenIntrospectionBenchmarks - Token introspection operations");
Console.WriteLine("4. TokenRevocationBenchmarks - Token revocation operations");
Console.WriteLine("5. ClientValidationBenchmarks - Client validation operations");
Console.WriteLine("6. ScopeValidationBenchmarks - Scope validation operations");
Console.WriteLine();

Console.WriteLine("To run all benchmarks:");
Console.WriteLine("  dotnet run -c Release");
Console.WriteLine();

Console.WriteLine("To run specific benchmark class:");
Console.WriteLine("  dotnet run -c Release -- --filter *TokenBenchmarks*");
Console.WriteLine();

Console.WriteLine("To run benchmarks with memory diagnostics:");
Console.WriteLine("  dotnet run -c Release -- --memory");
Console.WriteLine();

Console.WriteLine("Starting benchmarks...");
Console.WriteLine();

// Run all benchmarks
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);