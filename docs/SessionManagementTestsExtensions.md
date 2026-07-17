# SessionManagementTestsExtensions

Extension methods for `SessionManagementTests` that provide test utilities for session management scenarios including session creation, retrieval, duration calculation, and expiration handling.

## API

### CreateTestSession

Creates a test session with the specified parameters.

```csharp
public static UserSession CreateTestSession(
    this SessionManagementTests test,
    string userId,
    string clientId,
    string grantedScopes = "openid profile email",
    string? ipAddress = null,
    string? userAgent = null
)
```

**Purpose:** Creates a single test session with configurable properties for testing session management functionality.

**Parameters:**
- `userId`: The user identifier (required, non-empty)
- `clientId`: The client identifier (required, non-empty)
- `grantedScopes`: Space-separated string of granted scopes (default: "openid profile email")
- `ipAddress`: The IP address for the session (default: "127.0.0.1")
- `userAgent`: The user agent string (default: "TestAgent/1.0")

**Return Value:** The created `UserSession` instance with `CreatedAt` set to current UTC time and `ExpiresAt` set to one hour from creation.

**Exceptions:**
- Throws `ArgumentNullException` if `userId` is null
- Throws `ArgumentException` if `clientId` is null or empty

---

### CreateMultipleSessions

Creates multiple test sessions for the same user.

```csharp
public static IReadOnlyList<UserSession> CreateMultipleSessions(
    this SessionManagementTests test,
    string userId,
    int count,
    string clientIdPrefix = "client"
)
```

**Purpose:** Creates a collection of sessions for testing scenarios involving multiple concurrent sessions for a single user.

**Parameters:**
- `userId`: The user identifier (required, non-null)
- `count`: Number of sessions to create (must be 1 or greater)
- `clientIdPrefix`: Prefix for generated client identifiers (e.g., "client" produces "client1", "client2", etc.)

**Return Value:** Read-only list of created `UserSession` instances.

**Exceptions:**
- Throws `ArgumentNullException` if `userId` is null
- Throws `ArgumentOutOfRangeException` if `count` is less than 1

---

### ShouldContainSingleActiveSessionForUser

Verifies that a session collection contains exactly one active session for the specified user.

```csharp
public static bool ShouldContainSingleActiveSessionForUser(
    this SessionManagementTests test,
    IEnumerable<UserSession> sessions,
    string userId
)
```

**Purpose:** Validates that a collection of sessions contains exactly one active session belonging to the specified user.

**Parameters:**
- `sessions`: The collection of sessions to verify (required, non-null)
- `userId`: The expected user identifier (required, non-null)

**Return Value:** `true` if the collection contains exactly one active session for the specified user; otherwise `false`.

**Exceptions:**
- Throws `ArgumentNullException` if either `sessions` or `userId` is null

**Remarks:** Uses the `IsActive()` method on `UserSession` to determine active sessions.

---

### GetSessionById

Gets the session with the specified session ID from the collection.

```csharp
public static UserSession? GetSessionById(
    this SessionManagementTests test,
    IEnumerable<UserSession> sessions,
    string sessionId
)
```

**Purpose:** Retrieves a session by its unique identifier from a collection.

**Parameters:**
- `sessions`: The collection of sessions to search (required, non-null)
- `sessionId`: The session identifier to find (required, non-null)

**Return Value:** The found `UserSession` instance, or `null` if no session with the specified ID exists.

**Exceptions:**
- Throws `ArgumentNullException` if either `sessions` or `sessionId` is null

---

### GetSessionDurationSeconds

Calculates the total duration in seconds for a session based on its creation and expiration times.

```csharp
public static double GetSessionDurationSeconds(
    this SessionManagementTests test,
    UserSession? session
)
```

**Purpose:** Computes the duration between `CreatedAt` and `ExpiresAt` for a session.

**Parameters:**
- `session`: The session to analyze (may be null)

**Return Value:** The duration in seconds as a `double`, or 0 if `session` is null.

**Remarks:** Returns 0 for null sessions to avoid null reference exceptions while providing a safe default value.

---

### CreateExpiringSession

Creates a session that is about to expire (within the next minute).

```csharp
public static UserSession CreateExpiringSession(
    this SessionManagementTests test,
    string userId,
    string clientId
)
```

**Purpose:** Creates a session with a short expiration time for testing session expiration and cleanup scenarios.

**Parameters:**
- `userId`: The user identifier (required, non-null)
- `clientId`: The client identifier (required, non-empty)

**Return Value:** A `UserSession` with `ExpiresAt` set to 30 seconds from creation time.

**Exceptions:**
- Throws `ArgumentNullException` if `userId` is null
- Throws `ArgumentException` if `clientId` is null or empty

---

### CreateSessionWithExpiration

Creates a session with custom expiration time specified in seconds.

```csharp
public static UserSession CreateSessionWithExpiration(
    this SessionManagementTests test,
    string userId,
    string clientId,
    int expirationSeconds
)
```

**Purpose:** Creates a session with a configurable expiration time for testing time-based session scenarios.

**Parameters:**
- `userId`: The user identifier (required, non-null)
- `clientId`: The client identifier (required, non-empty)
- `expirationSeconds`: Expiration time in seconds from now (must be positive)

**Return Value:** A `UserSession` with `ExpiresAt` set to `expirationSeconds` seconds from creation time.

**Exceptions:**
- Throws `ArgumentNullException` if `userId` is null
- Throws `ArgumentOutOfRangeException` if `expirationSeconds` is less than or equal to 0

## Usage

### Example 1: Creating and verifying a single session

```csharp
[Fact]
public void CreateTestSession_ShouldCreateValidSession()
{
    var test = new SessionManagementTests();
    
    var session = test.CreateTestSession(
        userId: "user123",
        clientId: "web-client",
        grantedScopes: "openid profile email offline_access",
        ipAddress: "192.168.1.100",
        userAgent: "Mozilla/5.0"
    );
    
    Assert.Equal("user123", session.UserId);
    Assert.Equal("web-client", session.ClientId);
    Assert.Equal("192.168.1.100", session.IpAddress);
    Assert.Equal("Mozilla/5.0", session.UserAgent);
    Assert.Equal("openid profile email offline_access", session.GrantedScopes);
    Assert.True(session.IsActive());
    Assert.Equal(3600, test.GetSessionDurationSeconds(session));
}
```

### Example 2: Testing session expiration scenarios

```csharp
[Fact]
public void SessionExpiration_ShouldHandleExpiringSessions()
{
    var test = new SessionManagementTests();
    
    // Create an expiring session
    var expiringSession = test.CreateExpiringSession("user456", "mobile-client");
    Assert.True(expiringSession.IsActive());
    
    // Create a session with custom expiration
    var customSession = test.CreateSessionWithExpiration("user789", "api-client", 7200);
    Assert.Equal(7200, test.GetSessionDurationSeconds(customSession));
    
    // Verify session retrieval
    var sessions = new List<UserSession> { expiringSession, customSession };
    var found = test.GetSessionById(sessions, expiringSession.SessionId);
    Assert.Same(expiringSession, found);
    
    // Verify single active session check
    var result = test.ShouldContainSingleActiveSessionForUser(sessions, "user456");
    Assert.True(result);
}
```

## Notes

**Thread Safety:** All methods are thread-safe for concurrent read operations. Session creation methods are not thread-safe for concurrent modifications to the same test instance since they rely on `DateTime.UtcNow` and mutable `UserSession` construction.

**Time Sensitivity:** Methods that create expiring sessions (`CreateExpiringSession`, `CreateSessionWithExpiration`) are sensitive to system clock changes during test execution. Tests using these methods should complete within the expiration window or account for time progression.

**Null Handling:** `GetSessionDurationSeconds` gracefully handles null sessions by returning 0, while other methods throw `ArgumentNullException` for null parameters as per .NET design guidelines.

**Default Values:** When optional parameters are not specified, methods use sensible defaults (e.g., "127.0.0.1" for IP address, "TestAgent/1.0" for user agent, 1 hour for session duration).