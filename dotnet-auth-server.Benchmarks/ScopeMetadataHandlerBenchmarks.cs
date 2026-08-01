using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using DotnetAuthServer.Handlers;
using DotnetAuthServer.Caching;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace dotnet_auth_server.Benchmarks
{
    /// <summary>
    /// Benchmarks for <see cref="ScopeMetadataHandler"/>.
    /// Covers the main public methods that contain logic, caching and collection handling.
    /// </summary>
    [MemoryDiagnoser]
    public class ScopeMetadataHandlerBenchmarks
    {
        private ScopeMetadataHandler _handler = null!;
        private InMemoryCacheService _cacheService = null!;
        private List<string> _scopeNames = null!;

        // Vary the number of scopes used for the collection‑based benchmarks.
        [Params(10, 100, 1000)]
        public int ScopeCount { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            // Simple in‑memory cache implementation used only for benchmarking.
            _cacheService = new InMemoryCacheService();

            // Use a null logger to avoid any logging overhead during the benchmarks.
            var logger = NullLogger<ScopeMetadataHandler>.Instance;

            _handler = new ScopeMetadataHandler(_cacheService, logger);

            // Prepare a mixed list of standard and custom scopes.
            var customScopes = new List<string>();
            for (int i = 0; i < ScopeCount; i++)
            {
                var name = $"custom{i}";
                var metadata = new ScopeMetadata
                {
                    Name = name,
                    DisplayName = $"Custom {i}",
                    Description = $"Custom scope number {i}",
                    RequiresConsent = i % 2 == 0 // alternate consent requirement
                };
                _handler.RegisterCustomScope(metadata);
                customScopes.Add(name);
            }

            // Combine standard scopes with the generated custom ones.
            _scopeNames = new List<string>
            {
                "openid",
                "profile",
                "email",
                "phone",
                "address",
                "offline_access"
            };
            _scopeNames.AddRange(customScopes);
        }

        // --------------------------------------------------------------------- //
        // Single scope retrieval (cached after first call)
        // --------------------------------------------------------------------- //
        [Benchmark]
        public async Task<ScopeMetadata?> GetScopeMetadataAsync_Single()
        {
            // The first call will populate the cache; subsequent iterations measure the cached path.
            return await _handler.GetScopeMetadataAsync("openid");
        }

        // --------------------------------------------------------------------- //
        // Multiple scopes retrieval – exercises the loop and async calls.
        // --------------------------------------------------------------------- //
        [Benchmark]
        public async Task<IEnumerable<ScopeMetadata>> GetScopesMetadataAsync_Multiple()
        {
            return await _handler.GetScopesMetadataAsync(_scopeNames);
        }

        // --------------------------------------------------------------------- //
        // Retrieve all known scopes – iterates over the static dictionary.
        // --------------------------------------------------------------------- //
        [Benchmark]
        public async Task<IEnumerable<ScopeMetadata>> GetAllScopesAsync()
        {
            return await _handler.GetAllScopesAsync();
        }

        // --------------------------------------------------------------------- //
        // Filter scopes that require consent – pure LINQ over the static map.
        // --------------------------------------------------------------------- //
        [Benchmark]
        public IEnumerable<ScopeMetadata> GetScopesRequiringConsent()
        {
            return _handler.GetScopesRequiringConsent(_scopeNames);
        }

        // --------------------------------------------------------------------- //
        // Register a new custom scope – simple dictionary write.
        // --------------------------------------------------------------------- //
        [Benchmark]
        public void RegisterCustomScope()
        {
            var meta = new ScopeMetadata
            {
                Name = "benchmark_new_scope",
                DisplayName = "Benchmark New Scope",
                Description = "Created during benchmark",
                RequiresConsent = true
            };
            _handler.RegisterCustomScope(meta);
        }

        // --------------------------------------------------------------------- //
        // Very small in‑memory cache used by the benchmarks.
        // --------------------------------------------------------------------- //
        private sealed class InMemoryCacheService : ICacheService
        {
            private readonly ConcurrentDictionary<string, (object Value, DateTimeOffset Expiration)> _store
                = new();

            public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
            {
                if (_store.TryGetValue(key, out var entry) && entry.Expiration > DateTimeOffset.UtcNow)
                {
                    return Task.FromResult((T?)entry.Value);
                }

                return Task.FromResult<T?>(default);
            }

            public Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpirationRelativeToNow = null,
                CancellationToken cancellationToken = default)
            {
                var expiration = absoluteExpirationRelativeToNow.HasValue
                    ? DateTimeOffset.UtcNow.Add(absoluteExpirationRelativeToNow.Value)
                    : DateTimeOffset.MaxValue;

                _store[key] = (value!, expiration);
                return Task.CompletedTask;
            }

            public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
            {
                _store.TryRemove(key, out _);
                return Task.CompletedTask;
            }
        }
    }
}
