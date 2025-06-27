# IUserRepository

The `IUserRepository` interface defines a contract for data access operations related to user entities in the `dotnet-auth-server` project. It abstracts persistence logic for user management, including CRUD operations, role-based queries, and search functionality.

## API

### `Task<User?> GetByIdAsync(Guid id)`

Retrieves a user by their unique identifier.

- **Parameters**: `id` – The unique identifier of the user.
- **Return value**: A `User` instance if found; otherwise, `null`.
- **Exceptions**: Throws if the identifier is invalid or the database operation fails.

### `Task<IEnumerable<User>> GetAllAsync()`

Retrieves all users in the system.

- **Return value**: An enumerable collection of `User` instances.
- **Exceptions**: Throws if the database operation fails.

### `Task<User> CreateAsync(User user)`

Creates a new user in the system.

- **Parameters**: `user` – The user instance to create.
- **Return value**: The created `User` instance, including any generated identifiers.
- **Exceptions**: Throws if the user is invalid, a duplicate exists, or the database operation fails.

### `Task<User> UpdateAsync(User user)`

Updates an existing user in the system.

- **Parameters**: `user` – The user instance with updated properties.
- **Return value**: The updated `User` instance.
- **Exceptions**: Throws if the user does not exist, is invalid, or the database operation fails.

### `Task DeleteAsync(User user)`

Deletes an existing user from the system.

- **Parameters**: `user` – The user instance to delete.
- **Exceptions**: Throws if the user does not exist or the database operation fails.

### `Task DeleteByIdAsync(Guid id)`

Deletes a user by their unique identifier.

- **Parameters**: `id` – The unique identifier of the user to delete.
- **Exceptions**: Throws if the identifier is invalid or the database operation fails.

### `Task<bool> ExistsAsync(Guid id)`

Checks whether a user with the given identifier exists.

- **Parameters**: `id` – The unique identifier of the user.
- **Return value**: `true` if the user exists; otherwise, `false`.
- **Exceptions**: Throws if the identifier is invalid or the database operation fails.

### `Task<User?> GetByUsernameAsync(string username)`

Retrieves a user by their username.

- **Parameters**: `username` – The username to search for.
- **Return value**: A `User` instance if found; otherwise, `null`.
- **Exceptions**: Throws if the username is invalid or the database operation fails.

### `Task<User?> GetByEmailAsync(string email)`

Retrieves a user by their email address.

- **Parameters**: `email` – The email address to search for.
- **Return value**: A `User` instance if found; otherwise, `null`.
- **Exceptions**: Throws if the email is invalid or the database operation fails.

### `Task<IEnumerable<User>> GetByRoleAsync(string role)`

Retrieves all users with the specified role.

- **Parameters**: `role` – The role to filter users by.
- **Return value**: An enumerable collection of `User` instances with the role.
- **Exceptions**: Throws if the role is invalid or the database operation fails.

### `Task<IEnumerable<User>> GetActiveUsersAsync()`

Retrieves all active users in the system.

- **Return value**: An enumerable collection of `User` instances marked as active.
- **Exceptions**: Throws if the database operation fails.

### `Task<IEnumerable<User>> SearchAsync(string query)`

Searches for users matching the given query.

- **Parameters**: `query` – The search term to match against user properties.
- **Return value**: An enumerable collection of `User` instances matching the query.
- **Exceptions**: Throws if the query is invalid or the database operation fails.

## Usage

### Example 1: Basic User Creation and Retrieval
