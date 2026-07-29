// SPDX-License-Identifier: MIT
// Tests for MemoryCacheServiceExtensions
// ---------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotnetAuthServer.Caching;
using Xunit;

namespace DotnetAuthServer.Tests;

public sealed class MemoryCacheServiceExtensionsTests
{
    private readonly MemoryCacheService _cache;

    public MemoryCacheServiceExtensionsTests()
    {
        // MemoryCacheService has a public parameter‑less constructor in the current code base.
        // If the constructor changes, adjust this initialization accordingly.
        _cache = new MemoryCacheService();
    }

    #region TryGetValueAsync / SetValueAsync

    [Fact]
    public async Task TryGetValueAsync_ReturnsNull_WhenKeyMissing()
    {
        var result = await _cache.TryGetValueAsync<string>("nonexistent");
        Assert.Null(result);
    }

    [Fact]
    public async Task SetValueAsync_Then_TryGetValueAsync_ReturnsStoredValue()
    {
        const string key = "greeting";
        const string value = "hello";

        await _cache.SetValueAsync(key, value);
        var retrieved = await _cache.TryGetValueAsync<string>(key);

        Assert.Equal(value, retrieved);
    }

    [Fact]
    public async Task TryGetValueAsync_ThrowsArgumentNullException_WhenCacheIsNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await MemoryCacheServiceExtensions.TryGetValueAsync<string>(null!, "key"));
    }

    [Fact]
    public async Task SetValueAsync_ThrowsArgumentException_WhenKeyIsWhiteSpace()
    {
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _cache.SetValueAsync("   ", "value"));
    }

    #endregion

    #region SetMultipleAsync / GetMultipleAsync

    [Fact]
    public async Task SetMultipleAsync_Then_GetMultipleAsync_ReturnsAllValues()
    {
        var items = new Dictionary<string, string>
        {
            ["k1"] = "v1",
            ["k2"] = "v2",
            ["k3"] = "v3"
        };

        await _cache.SetMultipleAsync(items);
        var result = await _cache.GetMultipleAsync<string>(items.Keys);

        Assert.Equal(items.Count, result.Count);
        foreach (var kvp in items)
        {
            Assert.Equal(kvp.Value, result[kvp.Key]);
        }
    }

    [Fact]
    public async Task GetMultipleAsync_IgnoresNullOrWhiteSpaceKeys()
    {
        var items = new Dictionary<string, string>
        {
            ["valid"] = "ok"
        };
        await _cache.SetMultipleAsync(items);

        var keys = new[] { "valid", "", "   ", null! };
        var result = await _cache.GetMultipleAsync<string>(keys);

        Assert.Equal(4, result.Count);
        Assert.Equal("ok", result["valid"]);
        Assert.Null(result[""]);
        Assert.Null(result["   "]);
        Assert.Null(result[null!]);
    }

    #endregion

    #region GetOrSetMultipleAsync

    [Fact]
    public async Task GetOrSetMultipleAsync_ReturnsExistingValue_IfAnyKeyHasValue()
    {
        await _cache.SetValueAsync("first", "cached");

        var result = await _cache.GetOrSetMultipleAsync(
            new[] { "first", "second" },
            async (keys, ct) => "computed",
            expiration: null,
            cancellationToken: CancellationToken.None);

        Assert.Equal("cached", result);
    }

    [Fact]
    public async Task GetOrSetMultipleAsync_ComputesAndStoresValue_WhenAllKeysMissing()
    {
        var factoryCalled = false;
        async Task<string?> Factory(IReadOnlyList<string> ks, CancellationToken ct)
        {
            factoryCalled = true;
            return "factory";
        }

        var result = await _cache.GetOrSetMultipleAsync(
            new[] { "a", "b" },
            Factory,
            expiration: null,
            cancellationToken: CancellationToken.None);

        Assert.True(factoryCalled);
        Assert.Equal("factory", result);

        // Verify it was stored under the first key
        var stored = await _cache.TryGetValueAsync<string>("a");
        Assert.Equal("factory", stored);
    }

    #endregion

    #region GetExpirationAsync

    [Fact]
    public async Task GetExpirationAsync_ReturnsNull_WhenKeyMissing()
    {
        var expiration = await _cache.GetExpirationAsync("no-key");
        Assert.Null(expiration);
    }

    #endregion

    #region RemoveByPatternsAsync / GetKeysByPatternAsync

    [Fact]
    public async Task RemoveByPatternsAsync_DeletesMatchingKeys()
    {
        var items = new Dictionary<string, string>
        {
            ["user:1"] = "u1",
            ["user:2"] = "u2",
            ["order:1"] = "o1"
        };
        await _cache.SetMultipleAsync(items);

        await _cache.RemoveByPatternsAsync(new[] { "user:*" });

        var after = await _cache.GetMultipleAsync<string>(items.Keys);
        Assert.Null(after["user:1"]);
        Assert.Null(after["user:2"]);
        Assert.Equal("o1", after["order:1"]);
    }

    [Fact]
    public async Task GetKeysByPatternAsync_ReturnsMatchingKeys()
    {
        var items = new Dictionary<string, string>
        {
            ["session:abc"] = "s1",
            ["session:def"] = "s2",
            ["cache:xyz"]   = "c1"
        };
        await _cache.SetMultipleAsync(items);

        var keys = await _cache.GetKeysByPatternAsync("session:*");
        var expected = new[] { "session:abc", "session:def" };

        Assert.Equal(expected.Length, keys.Count);
        foreach (var exp in expected)
        {
            Assert.Contains(exp, keys);
        }
    }

    #endregion

    #region GetStatisticsAsync

    [Fact]
    public async Task GetStatisticsAsync_ReflectsCurrentCacheState()
    {
        await _cache.SetValueAsync("stat1", "value1");
        await _cache.SetValueAsync("stat2", "value2");

        var stats = await _cache.GetStatisticsAsync();

        Assert.True(stats.EntryCount >= 2);
        Assert.True(stats.LockCount >= 0);
        Assert.True(stats.TotalSizeBytes > 0);
        Assert.False(string.IsNullOrWhiteSpace(stats.ApproximateSize));
    }

    #endregion
}
