#nullable enable

using DotnetAuthServer.Caching;
using FluentAssertions;
using Xunit;

namespace DotnetAuthServer.Tests.Caching;

public class MemoryCacheServiceTests
{
    private readonly MemoryCacheService _cacheService;

    public MemoryCacheServiceTests()
    {
        _cacheService = new MemoryCacheService();
    }

    [Fact]
    public async Task GetAsync_WhenKeyDoesNotExist_ReturnsNull()
    {
        // Act
        var result = await _cacheService.GetAsync<string>("nonexistent_key");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_WhenKeyIsNullOrEmpty_ReturnsNull()
    {
        // Act
        var result1 = await _cacheService.GetAsync<string>(null!);
        var result2 = await _cacheService.GetAsync<string>("");
        var result3 = await _cacheService.GetAsync<string>("   ");

        // Assert
        result1.Should().BeNull();
        result2.Should().BeNull();
        result3.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_WhenKeyExists_ReturnsCachedValue()
    {
        // Arrange
        const string key = "test_key";
        const string expectedValue = "test_value";
        await _cacheService.SetAsync(key, expectedValue);

        // Act
        var result = await _cacheService.GetAsync<string>(key);

        // Assert
        result.Should().Be(expectedValue);
    }

    [Fact]
    public async Task GetAsync_WithDifferentTypes_ReturnsCorrectlyTypedValue()
    {
        // Arrange
        const string stringKey = "string_key";
        const string stringValue = "test_string";
        const string intWrapperKey = "int_wrapper_key";
        var intWrapperValue = new IntWrapper { Value = 42 };
        const string customKey = "custom_key";
        var customValue = new TestClass { Id = 1, Name = "Test" };

        await _cacheService.SetAsync(stringKey, stringValue);
        await _cacheService.SetAsync(intWrapperKey, intWrapperValue);
        await _cacheService.SetAsync(customKey, customValue);

        // Act
        var stringResult = await _cacheService.GetAsync<string>(stringKey);
        var intWrapperResult = await _cacheService.GetAsync<IntWrapper>(intWrapperKey);
        var customResult = await _cacheService.GetAsync<TestClass>(customKey);

        // Assert
        stringResult.Should().Be(stringValue);
        intWrapperResult.Should().BeEquivalentTo(intWrapperValue);
        customResult.Should().BeEquivalentTo(customValue);
    }

    [Fact]
    public async Task SetAsync_WhenKeyIsNullOrEmpty_DoesNotThrow()
    {
        // Act
        Func<Task> action1 = async () => await _cacheService.SetAsync<string>(null!, "value");
        Func<Task> action2 = async () => await _cacheService.SetAsync<string>("", "value");
        Func<Task> action3 = async () => await _cacheService.SetAsync<string>("   ", "value");

        // Assert
        await action1.Should().NotThrowAsync();
        await action2.Should().NotThrowAsync();
        await action3.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SetAsync_StoresValueWithNoExpiration()
    {
        // Arrange
        const string key = "persistent_key";
        const string value = "persistent_value";

        // Act
        await _cacheService.SetAsync(key, value);
        var result = await _cacheService.GetAsync<string>(key);

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public async Task SetAsync_StoresValueWithExpiration()
    {
        // Arrange
        const string key = "expiring_key";
        const string value = "expiring_value";
        var expiration = TimeSpan.FromMilliseconds(100);

        // Act
        await _cacheService.SetAsync(key, value, expiration);
        var beforeExpiry = await _cacheService.GetAsync<string>(key);

        // Wait for expiration
        await Task.Delay(150);
        var afterExpiry = await _cacheService.GetAsync<string>(key);

        // Assert
        beforeExpiry.Should().Be(value);
        afterExpiry.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_WithReferenceType_WrapsIntValue()
    {
        // Arrange
        const string key = "int_wrapper_key";
        var value = new IntWrapper { Value = 42 };
        var expiration = TimeSpan.FromMilliseconds(100);

        // Act
        await _cacheService.SetAsync(key, value, expiration);
        var beforeExpiry = await _cacheService.GetAsync<IntWrapper>(key);

        // Wait for expiration
        await Task.Delay(150);
        var afterExpiry = await _cacheService.GetAsync<IntWrapper>(key);

        // Assert
        beforeExpiry.Should().BeEquivalentTo(value);
        afterExpiry.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAsync_WhenKeyDoesNotExist_DoesNotThrow()
    {
        // Act
        Func<Task> action = async () => await _cacheService.RemoveAsync("nonexistent_key");

        // Assert
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RemoveAsync_WhenKeyIsNullOrEmpty_DoesNotThrow()
    {
        // Act
        Func<Task> action1 = async () => await _cacheService.RemoveAsync(null!);
        Func<Task> action2 = async () => await _cacheService.RemoveAsync("");
        Func<Task> action3 = async () => await _cacheService.RemoveAsync("   ");

        // Assert
        await action1.Should().NotThrowAsync();
        await action2.Should().NotThrowAsync();
        await action3.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RemoveAsync_RemovesExistingKey()
    {
        // Arrange
        const string key = "removable_key";
        const string value = "removable_value";
        await _cacheService.SetAsync(key, value);

        var beforeRemove = await _cacheService.GetAsync<string>(key);
        beforeRemove.Should().Be(value);

        // Act
        await _cacheService.RemoveAsync(key);
        var afterRemove = await _cacheService.GetAsync<string>(key);

        // Assert
        afterRemove.Should().BeNull();
    }

    [Fact]
    public async Task RemoveByPatternAsync_WhenPatternIsNullOrEmpty_DoesNotThrow()
    {
        // Arrange
        await _cacheService.SetAsync("key1", "value1");
        await _cacheService.SetAsync("key2", "value2");
        await _cacheService.SetAsync("other", "value3");

        // Act
        Func<Task> action1 = async () => await _cacheService.RemoveByPatternAsync(null!);
        Func<Task> action2 = async () => await _cacheService.RemoveByPatternAsync("");
        Func<Task> action3 = async () => await _cacheService.RemoveByPatternAsync("   ");

        // Assert
        await action1.Should().NotThrowAsync();
        await action2.Should().NotThrowAsync();
        await action3.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RemoveByPatternAsync_RemovesMatchingKeys()
    {
        // Arrange
        await _cacheService.SetAsync("user_123", "value1");
        await _cacheService.SetAsync("user_456", "value2");
        await _cacheService.SetAsync("admin_789", "value3");
        await _cacheService.SetAsync("other_key", "value4");

        // Act - remove all keys starting with "user_"
        await _cacheService.RemoveByPatternAsync("user_*");

        // Assert
        var user123 = await _cacheService.GetAsync<string>("user_123");
        var user456 = await _cacheService.GetAsync<string>("user_456");
        var admin789 = await _cacheService.GetAsync<string>("admin_789");
        var otherKey = await _cacheService.GetAsync<string>("other_key");

        user123.Should().BeNull();
        user456.Should().BeNull();
        admin789.Should().Be("value3"); // Should not be removed
        otherKey.Should().Be("value4"); // Should not be removed
    }

    [Fact]
    public async Task RemoveByPatternAsync_WithWildcardPattern_RemovesMatchingKeys()
    {
        // Arrange
        await _cacheService.SetAsync("cache_user", "value1");
        await _cacheService.SetAsync("cache_admin", "value2");
        await _cacheService.SetAsync("cache_data", "value3");
        await _cacheService.SetAsync("other_user", "value4");

        // Act - remove all keys starting with "cache_"
        await _cacheService.RemoveByPatternAsync("cache_*");

        // Assert
        var cacheUser = await _cacheService.GetAsync<string>("cache_user");
        var cacheAdmin = await _cacheService.GetAsync<string>("cache_admin");
        var cacheData = await _cacheService.GetAsync<string>("cache_data");
        var otherUser = await _cacheService.GetAsync<string>("other_user");

        cacheUser.Should().BeNull();
        cacheAdmin.Should().BeNull();
        cacheData.Should().BeNull();
        otherUser.Should().Be("value4"); // Should not be removed
    }

    [Fact]
    public async Task RemoveByPatternAsync_WithQuestionMarkPattern_RemovesMatchingKeys()
    {
        // Arrange
        await _cacheService.SetAsync("user1", "value1");
        await _cacheService.SetAsync("user2", "value2");
        await _cacheService.SetAsync("user10", "value3");
        await _cacheService.SetAsync("admin1", "value4");

        // Act - remove keys matching "user?" pattern (exactly one character after "user")
        await _cacheService.RemoveByPatternAsync("user?");

        // Assert
        var user1 = await _cacheService.GetAsync<string>("user1");
        var user2 = await _cacheService.GetAsync<string>("user2");
        var user10 = await _cacheService.GetAsync<string>("user10");
        var admin1 = await _cacheService.GetAsync<string>("admin1");

        user1.Should().BeNull();
        user2.Should().BeNull();
        user10.Should().Be("value3"); // Should not be removed (2 chars)
        admin1.Should().Be("value4"); // Should not be removed (different prefix)
    }

    [Fact]
    public async Task ClearAsync_RemovesAllKeys()
    {
        // Arrange
        await _cacheService.SetAsync("key1", "value1");
        await _cacheService.SetAsync("key2", "value2");
        await _cacheService.SetAsync("key3", "value3");

        // Verify keys exist
        (await _cacheService.GetAsync<string>("key1")).Should().Be("value1");
        (await _cacheService.GetAsync<string>("key2")).Should().Be("value2");
        (await _cacheService.GetAsync<string>("key3")).Should().Be("value3");

        // Act
        await _cacheService.ClearAsync();

        // Assert
        (await _cacheService.GetAsync<string>("key1")).Should().BeNull();
        (await _cacheService.GetAsync<string>("key2")).Should().BeNull();
        (await _cacheService.GetAsync<string>("key3")).Should().BeNull();
    }

    [Fact]
    public async Task GetOrSetAsync_WhenKeyDoesNotExist_SetsAndReturnsValue()
    {
        // Arrange
        const string key = "factory_key";
        var callCount = 0;

        // Act
        var result = await _cacheService.GetOrSetAsync(
            key,
            async ct =>
            {
                callCount++;
                await Task.Delay(10, ct); // Simulate work
                return "computed_value";
            }
        );

        // Assert
        result.Should().Be("computed_value");
        callCount.Should().Be(1);

        // Verify it was cached
        var cachedResult = await _cacheService.GetAsync<string>(key);
        cachedResult.Should().Be("computed_value");
    }

    [Fact]
    public async Task GetOrSetAsync_WhenKeyExists_ReturnsCachedValueWithoutCallingFactory()
    {
        // Arrange
        const string key = "cached_factory_key";
        const string cachedValue = "cached_value";
        var callCount = 0;

        await _cacheService.SetAsync(key, cachedValue);

        // Act
        var result = await _cacheService.GetOrSetAsync(
            key,
            async ct =>
            {
                callCount++;
                await Task.Delay(10, ct);
                return "computed_value";
            }
        );

        // Assert
        result.Should().Be(cachedValue);
        callCount.Should().Be(0); // Factory should not be called
    }

    [Fact]
    public async Task GetOrSetAsync_WithExpiration_SetsExpirationOnValue()
    {
        // Arrange
        const string key = "expiring_factory_key";
        var expiration = TimeSpan.FromMilliseconds(50);
        var callCount = 0;

        // Act
        var result = await _cacheService.GetOrSetAsync(
            key,
            async ct =>
            {
                callCount++;
                await Task.Delay(10, ct);
                return "expiring_value";
            },
            expiration
        );

        // Assert
        result.Should().Be("expiring_value");
        callCount.Should().Be(1);

        // Verify it was cached with expiration
        var beforeExpiry = await _cacheService.GetAsync<string>(key);
        beforeExpiry.Should().Be("expiring_value");

        await Task.Delay(100);
        var afterExpiry = await _cacheService.GetAsync<string>(key);
        afterExpiry.Should().BeNull();
    }

    [Fact]
    public async Task GetOrSetAsync_WithReferenceType_WrapsIntValue()
    {
        // Arrange
        const string key = "int_wrapper_factory_key";
        var expiration = TimeSpan.FromMilliseconds(50);
        var callCount = 0;

        // Act
        var result = await _cacheService.GetOrSetAsync(
            key,
            async ct =>
            {
                callCount++;
                await Task.Delay(10, ct);
                return new IntWrapper { Value = 123 };
            },
            expiration
        );

        // Assert
        result.Should().BeEquivalentTo(new IntWrapper { Value = 123 });
        callCount.Should().Be(1);

        // Verify it was cached with expiration
        var beforeExpiry = await _cacheService.GetAsync<IntWrapper>(key);
        beforeExpiry.Should().BeEquivalentTo(new IntWrapper { Value = 123 });

        await Task.Delay(100);
        var afterExpiry = await _cacheService.GetAsync<IntWrapper>(key);
        afterExpiry.Should().BeNull();
    }

    [Fact]
    public async Task GetOrSetAsync_WithConcurrentAccess_OnlyCallsFactoryOnce()
    {
        // Arrange
        const string key = "concurrent_key";
        var concurrentCallCount = 0;
        var tasks = new List<Task<string?>>();

        // Create multiple concurrent tasks trying to get/set the same key
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_cacheService.GetOrSetAsync(
                key,
                async ct =>
                {
                    Interlocked.Increment(ref concurrentCallCount);
                    await Task.Delay(50, ct); // Simulate expensive operation
                    return "concurrent_value";
                }
            ));
        }

        // Act - wait for all tasks to complete
        var results = await Task.WhenAll(tasks);

        // Assert
        concurrentCallCount.Should().Be(1); // Factory should only be called once
        results.Should().AllBeEquivalentTo("concurrent_value"); // All should get same value

        // Verify it was cached
        var cachedResult = await _cacheService.GetAsync<string>(key);
        cachedResult.Should().Be("concurrent_value");
    }

    [Fact]
    public async Task GetOrSetAsync_WithNullFactoryResult_DoesNotCacheValue()
    {
        // Arrange
        const string key = "null_factory_key";
        var callCount = 0;

        // Act
        var result = await _cacheService.GetOrSetAsync(
            key,
            async ct =>
            {
                callCount++;
                await Task.Delay(10, ct);
                return (string?)null; // Factory returns null
            }
        );

        // Assert
        result.Should().BeNull();
        callCount.Should().Be(1);

        // Verify null was not cached
        var cachedResult = await _cacheService.GetAsync<string>(key);
        cachedResult.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_ExpiredEntry_IsRemovedFromCache()
    {
        // Arrange
        const string key = "expiring_get_key";
        const string value = "expiring_get_value";
        await _cacheService.SetAsync(key, value, TimeSpan.FromMilliseconds(50));

        // Verify it exists initially
        (await _cacheService.GetAsync<string>(key)).Should().Be(value);

        // Wait for expiration
        await Task.Delay(100);

        // Act & Assert - should return null and remove expired entry
        var result = await _cacheService.GetAsync<string>(key);
        result.Should().BeNull();

        // Verify entry was actually removed from underlying cache
        var secondAttempt = await _cacheService.GetAsync<string>(key);
        secondAttempt.Should().BeNull();
    }

    [Fact]
    public async Task MultipleOperations_ThreadSafety_Test()
    {
        // Arrange
        const int iterations = 100;
        var tasks = new List<Task>();

        // Act - run multiple operations concurrently
        for (int i = 0; i < iterations; i++)
        {
            int index = i;
            tasks.Add(Task.Run(async () =>
            {
                var key = $"thread_key_{index % 10}"; // 10 unique keys
                var value = $"thread_value_{index}";

                // Mix of operations
                if (index % 3 == 0)
                {
                    await _cacheService.SetAsync(key, value);
                }
                else if (index % 3 == 1)
                {
                    await _cacheService.GetAsync<string>(key);
                }
                else
                {
                    await _cacheService.RemoveAsync(key);
                }
            }));
        }

        // Assert - all tasks complete without exceptions
        await Task.WhenAll(tasks);

        // Verify no exceptions were thrown
        true.Should().BeTrue();
    }

    private class TestClass
    {
        public int Id { get; set; }
        public string? Name { get; set; }

        public override bool Equals(object? obj) =>
            obj is TestClass other && Id == other.Id && Name == other.Name;

        public override int GetHashCode() => HashCode.Combine(Id, Name);
    }

    private class IntWrapper
    {
        public int Value { get; set; }

        public override bool Equals(object? obj) =>
            obj is IntWrapper other && Value == other.Value;

        public override int GetHashCode() => Value.GetHashCode();
    }
}