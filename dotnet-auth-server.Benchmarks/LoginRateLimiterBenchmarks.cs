using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Running;
using DotnetAuthServer.Security;
using Microsoft.Extensions.Logging;
using System;

namespace DotnetAuthServer.Benchmarks
{
    [MemoryDiagnoser]
    public class LoginRateLimiterBenchmarks
    {
        private LoginRateLimiter _loginRateLimiter;
        private string[] _usernames;
        private string[] _ipAddresses;

        [GlobalSetup]
        public void Setup()
        {
            var options = new AuthServerOptions
            {
                FailedLoginAttemptThreshold = 5,
                AccountLockoutDurationMinutes = 30
            };

            _loginRateLimiter = new LoginRateLimiter(options, new LoggerFactory().CreateLogger<LoginRateLimiter>());
            _usernames = new string[1000];
            _ipAddresses = new string[1000];

            for (int i = 0; i < 1000; i++)
            {
                _usernames[i] = $"username{i}";
                _ipAddresses[i] = $"ipAddress{i}";
            }
        }

        [Benchmark]
        [Params(10, 100, 1000)]
        public void RecordFailure(int count)
        {
            for (int i = 0; i < count; i++)
            {
                _loginRateLimiter.RecordFailure(_usernames[i % _usernames.Length], _ipAddresses[i % _ipAddresses.Length]);
            }
        }

        [Benchmark]
        [Params(10, 100, 1000)]
        public void ThrowIfBlocked(int count)
        {
            for (int i = 0; i < count; i++)
            {
                try
                {
                    _loginRateLimiter.ThrowIfBlocked(_usernames[i % _usernames.Length], _ipAddresses[i % _ipAddresses.Length]);
                }
                catch (Exception)
                {
                    // Ignore exception
                }
            }
        }

        [Benchmark]
        [Params(10, 100, 1000)]
        public void RecordSuccess(int count)
        {
            for (int i = 0; i < count; i++)
            {
                _loginRateLimiter.RecordSuccess(_usernames[i % _usernames.Length]);
            }
        }
    }
}
