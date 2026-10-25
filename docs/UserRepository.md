# UserRepository

`UserRepository` is the in-memory implementation of `IUserRepository`. It stores `User` entities in a `ConcurrentDictionary<string, User>` keyed by `UserId`, and it is the only persistence for users in this server. The class is `public sealed` and lives in `src/Data/Repositories/UserRepository.cs`, namespace `DotnetAuthServer.Data.Repositories`.

The repository is registered as a singleton (see [CLAUDE.md](../CLAUDE.md) → Conventions). All data is lost on process restart. `IUserRepository` is the seam for a persistent store.

## Storage and comparison rules

- **Key:** `User.UserId`. The dictionary uses `StringComparer.OrdinalIgnoreCase`, so `UserId` lookups are case-insensitive. `"ABC"` and `"abc"` refer to the same user.
- **Values:** the stored `User` instances are the same references the caller passed in. Changing a returned or stored `User` object changes the repository's copy without calling `UpdateAsync`.
- **Snapshots:** collection-returning methods build a new `List<User>` from the dictionary values. The list is a snapshot, but the `User` objects in it are shared references.
- **Cancellation:** every method accepts a `CancellationToken`, but none of them observes it. Cancelling has no effect.

## Constructor

The class declares no constructor. The compiler-generated parameterless constructor runs, and the `_users` field is initialised inline.

## Members

The class implements the members of `IUserRepository` (which extends `IRepository<User, string>`). Each member is listed once below.

### `Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default)`

Returns the user with the given `UserId`, or `null` if none exists.

- **Parameters:** `id` – the user ID. Compared case-insensitively.
- **Returns:** the stored `User`, or `null`.
- **Throws:** `ArgumentNullException` if `id` is `null`; `ArgumentException` if it is empty. Because the method is `async`, the exception is delivered through the returned `Task`, not thrown synchronously.

### `Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default)`

Returns every stored user.

- **Returns:** a new `List<User>` snapshot. Empty if the repository has no users.

### `Task<User> CreateAsync(User entity, CancellationToken cancellationToken = default)`

Adds a user.

- **Parameters:** `entity` – the user to add. Its `UserId` is the key.
- **Returns:** the same `entity` instance. `UpdatedAt` is not modified.
- **Throws:**
  - `ArgumentNullException` if `entity` is `null`.
  - `InvalidOperationException` with the message `User with ID {UserId} already exists` if a user with the same `UserId` (case-insensitive) is already stored. The existing user is not changed.

### `Task<User> UpdateAsync(User entity, CancellationToken cancellationToken = default)`

Replaces the stored user that has the same `UserId`.

- **Parameters:** `entity` – the replacement user.
- **Returns:** the same `entity` instance, after `UpdatedAt` is set to `DateTime.UtcNow`. The stamp is written onto the caller's object before it is stored.
- **Throws:**
  - `ArgumentNullException` if `entity` is `null`.
  - `InvalidOperationException` with the message `User with ID {UserId} not found` if no user with that `UserId` exists. Nothing is stored and `UpdatedAt` is not changed.

The replacement is a plain indexer write. It is not a compare-and-swap, so a concurrent update can overwrite another one.

### `Task DeleteAsync(User entity, CancellationToken cancellationToken = default)`

Deletes the user with the same `UserId` as `entity`.

- **Parameters:** `entity` – the user to delete. Only `entity.UserId` is used. The stored instance is removed, not the object passed in.
- **Returns:** a completed `Task`.
- **Throws:** `ArgumentNullException` if `entity` is `null`.

Delegates to `DeleteByIdAsync`.

### `Task DeleteByIdAsync(string id, CancellationToken cancellationToken = default)`

Removes the user with the given `UserId`, if present.

- **Parameters:** `id` – the user ID, compared case-insensitively.
- **Returns:** a completed `Task`. Deleting a missing user is not an error.
- **Throws:** `ArgumentNullException` if `id` is `null`; `ArgumentException` if it is empty. This method is not `async`, so the exception is thrown synchronously.

### `Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)`

Checks whether a user with the given `UserId` is stored.

- **Parameters:** `id` – the user ID, compared case-insensitively.
- **Returns:** `true` if a matching user exists; otherwise `false`.
- **Throws:** `ArgumentNullException` if `id` is `null`; `ArgumentException` if it is empty (delivered through the returned `Task`).

### `Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)`

Finds a user by username.

- **Parameters:** `username` – the username to match, case-insensitively (`StringComparison.OrdinalIgnoreCase`).
- **Returns:** the first stored user whose `Username` matches, or `null`. The repository does not enforce username uniqueness, so if several users share a name the result depends on dictionary enumeration order.
- **Throws:** `ArgumentNullException` / `ArgumentException` for a `null` or empty `username`, delivered through the returned `Task`.

The scan is linear over all users.

### `Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)`

Finds a user by email address.

- **Parameters:** `email` – the address to match, case-insensitively.
- **Returns:** the first stored user whose `Email` matches, or `null`. Like `GetByUsernameAsync`, uniqueness is not enforced and the scan is linear.
- **Throws:** `ArgumentNullException` / `ArgumentException` for a `null` or empty `email`, delivered through the returned `Task`.

### `Task<IEnumerable<User>> GetByRoleAsync(string role, CancellationToken cancellationToken = default)`

Returns every user that has the given role.

- **Parameters:** `role` – the role name, matched case-insensitively against each entry of `User.Roles`.
- **Returns:** a new `List<User>` of matching users. Empty if none match.
- **Throws:** `ArgumentNullException` / `ArgumentException` for a `null` or empty `role`, delivered through the returned `Task`.

### `Task<IEnumerable<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default)`

Returns every user whose `IsActive` is `true`.

- **Returns:** a new `List<User>` snapshot. Empty if no user is active.

This method takes no argument, so it has no argument-validation exceptions.

### `Task<IEnumerable<User>> SearchAsync(string query, CancellationToken cancellationToken = default)`

Returns users whose username, email, or full name contains `query`.

- **Parameters:** `query` – the search text.
- **Returns:** a new `List<User>` of matches. Empty if none match.
- **Throws:** `ArgumentNullException` / `ArgumentException` for a `null` or empty `query`, delivered through the returned `Task`.

Matching details:

- A user matches if `Username`, `Email`, or `FullName` contains the query as a substring. A `null` `FullName` is treated as no match on that field.
- The query and the fields are lowered with `ToLower()`, which uses the current culture, and then compared with `string.Contains`. This is **not** the `OrdinalIgnoreCase` comparison used by the other lookups, so results can differ under some cultures (for example, Turkish `I`/`ı`).
- The search is a linear scan with no index.

## Method summary

| Method | Lookup key | Comparison | Not found |
|---|---|---|---|
| `GetByIdAsync` | `UserId` | ordinal, ignore case | `null` |
| `ExistsAsync` | `UserId` | ordinal, ignore case | `false` |
| `DeleteByIdAsync` | `UserId` | ordinal, ignore case | no-op |
| `GetByUsernameAsync` | `Username` | ordinal, ignore case | `null` |
| `GetByEmailAsync` | `Email` | ordinal, ignore case | `null` |
| `GetByRoleAsync` | `Roles` entry | ordinal, ignore case | empty list |
| `SearchAsync` | substring of `Username`, `Email`, `FullName` | culture `ToLower()` + `Contains` | empty list |
| `GetActiveUsersAsync` | `IsActive == true` | n/a | empty list |
| `GetAllAsync` | all | n/a | empty list |

## Related

- [IUserRepository](IUserRepository.md) – the interface this class implements.
- [IRepository](../src/Data/Repositories/IRepository.cs) – the generic base contract.
- `User` entity – `src/Domain/Entities/User.cs`.
