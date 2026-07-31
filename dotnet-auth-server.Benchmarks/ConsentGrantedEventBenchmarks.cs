using BenchmarkDotNet.Attributes;
using DotnetAuthServer.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace dotnet_auth_server.Benchmarks
{
    [MemoryDiagnoser]
    public class ConsentGrantedEventBenchmarks
    {
        // Number of scopes to include in the GrantedScopes collection
        [Params(10, 100, 1000)]
        public int ScopeCount;

        private List<string> _scopes = null!;

        [GlobalSetup]
        public void Setup()
        {
            // Create a realistic list of scope strings (e.g., "scope1", "scope2", ...)
            _scopes = Enumerable.Range(1, ScopeCount)
                                .Select(i => $"scope{i}")
                                .ToList();
        }

        /// <summary>
        /// Benchmark the creation of a ConsentGrantedEvent with a variable number of granted scopes.
        /// </summary>
        [Benchmark]
        public ConsentGrantedEvent CreateEvent()
        {
            var @event = new ConsentGrantedEvent
            {
                UserId = Guid.NewGuid().ToString("N"),
                ClientId = Guid.NewGuid().ToString("N"),
                GrantedScopes = _scopes,
                IsPermanent = false,
                ClientIpAddress = "127.0.0.1"
            };

            return @event;
        }

        /// <summary>
        /// Benchmark the creation of a ConsentGrantedEvent where the consent is marked as permanent.
        /// </summary>
        [Benchmark]
        public ConsentGrantedEvent CreateEventPermanent()
        {
            var @event = new ConsentGrantedEvent
            {
                UserId = Guid.NewGuid().ToString("N"),
                ClientId = Guid.NewGuid().ToString("N"),
                GrantedScopes = _scopes,
                IsPermanent = true,
                ClientIpAddress = "192.168.0.1"
            };

            return @event;
        }

        /// <summary>
        /// Benchmark the creation of a ConsentGrantedEvent without an IP address (null) to simulate missing data.
        /// </summary>
        [Benchmark]
        public ConsentGrantedEvent CreateEventNoIp()
        {
            var @event = new ConsentGrantedEvent
            {
                UserId = Guid.NewGuid().ToString("N"),
                ClientId = Guid.NewGuid().ToString("N"),
                GrantedScopes = _scopes,
                IsPermanent = false,
                ClientIpAddress = null
            };

            return @event;
        }
    }
}
