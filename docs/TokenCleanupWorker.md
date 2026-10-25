# TokenCleanupWorker

The `TokenCleanupWorker` class is a `BackgroundService` that periodically removes expired data from the in-memory stores. It runs for the lifetime of the host, registered in `Program.cs` with `AddHostedService<TokenCleanupWorker>()`. The storage is in-memory only, so without this worker expired refresh tokens, authorization grants, and revocation entries would accumulate for as long as the process runs.

The class is `sealed` and has no public members beyond its constructor and the inherited `BackgroundService` lifecycle. Callers never invoke it directly.

## Timing

| Setting | Value | Where it is set |
|---|---|---|
| Startup delay | 5 minutes | Hard-coded `Task.Delay` at the start of `ExecuteAsync` |
| Cleanup interval | 1 hour | `_cleanupInterval = TimeSpan.FromHours(1)` in the constructor |

The first cleanup pass runs 5 minutes after the worker starts. This gives the server time to finish startup before the first sweep. After each pass the worker waits one hour before the next one. The interval is measured from the end of a pass, so a slow pass delays the next one rather than overlapping with it.

Neither value is read from configuration. `DotnetAuthServerOptions` has no cleanup setting, and the constructor does not accept an interval, so changing either value requires a code change.

## What It Purges

Each pass runs three steps in this order:

1. **Expired refresh tokens.** Calls `IRefreshTokenRepository.DeleteExpiredAsync`, which removes every refresh token whose `ExpiresAt` is at or before the current UTC time. Revoked tokens that have not yet expired are kept.
2. **Expired authorization grants.** Calls `IAuthorizationGrantRepository.DeleteExpiredAsync`, which removes every grant whose `ExpiresAt` is at or before the current UTC time.
3. **Expired revocation entries.** Calls `RevokedTokenStore.PurgeExpired()`, which removes revoked `jti` entries and their token metadata once the original token's expiry has passed. The store also purges on its own when it grows past 10 000 entries.

Steps 1 and 2 each open a new DI scope and resolve the repository from it. The repositories are singletons, so every scope sees the same data. Step 3 uses the `RevokedTokenStore` singleton that the worker receives in its constructor.

The worker does not purge anything else. In particular:

- User sessions are not touched. `UserSessionRepository.DeleteExpiredAsync` is called from `UserSessionService`, not from this worker.
- Consents are not touched, including those with an `ExpiresAt` in the past.
- Clients and users are not touched.

## Behavior

The loop runs until the host signals shutdown:

- Each pass is wrapped in a single `try`/`catch`. If any step throws, the error is logged with `LogError` ("Error during token cleanup"), the remaining steps of that pass are skipped, and the worker waits for the next interval. The next pass starts from step 1 again.
- Shutdown during the wait ends the loop cleanly. Cancellation during the startup delay propagates out of `ExecuteAsync`, which `BackgroundService` treats as a normal stop.
- Cancellation during a pass is caught by the same generic `catch`, so an in-progress sweep at shutdown is logged as an error rather than as a clean stop.
- Logging: an information message at startup and shutdown, and a start and completion message around each repository step.

## Usage

The worker has no public API to call. Register it in the composition root, as `Program.cs` already does:

```csharp
builder.Services.AddHostedService<TokenCleanupWorker>();
```

The constructor requires the following services, all of which must be registered:

- `ILogger<TokenCleanupWorker>`
- `IServiceProvider`
- `RevokedTokenStore`

To observe the worker, watch the log output for the "Token cleanup worker started" message and the per-step messages that follow the 5-minute delay.

## Remarks

- **Expiry is checked against the server clock.** Each repository compares `ExpiresAt` with `DateTime.UtcNow`, so the sweep removes records as soon as they expire, up to one interval late.
- **Memory is reclaimed only on a pass.** Expired records are invisible to the callers that check expiry themselves, but they remain in memory until the next sweep. Between passes, the set of stored records can be up to an hour out of date.
- **Shutdown does not persist anything.** The stores are in-memory, so a restart discards all data, expired or not. The worker only manages what is in memory during a run.
- **The class summary is out of date.** The XML summary on the class says the interval "can be configured via dependency injection". No such option exists; the interval is fixed at one hour.
