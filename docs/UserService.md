# UserService
The `UserService` class is designed to manage user authentication and authorization in the `dotnet-auth-server` project. It provides methods for authenticating users, creating new users, updating existing users, changing passwords, and managing user roles. This class is a crucial component of the authentication system, allowing developers to interact with user data and perform various operations.

## API
The `UserService` class has the following public members:
* `public UserService`: The constructor for the `UserService` class.
* `public async Task<User> AuthenticateAsync`: Authenticates a user and returns the authenticated user object. This method throws an exception if authentication fails.
* `public async Task<User> CreateUserAsync`: Creates a new user and returns the newly created user object. This method throws an exception if user creation fails.
* `public async Task<User> UpdateUserAsync`: Updates an existing user and returns the updated user object. This method throws an exception if user update fails.
* `public async Task ChangePasswordAsync`: Changes the password of a user. This method throws an exception if password change fails.
* `public async Task AssignRoleAsync`: Assigns a role to a user. This method throws an exception if role assignment fails.
* `public async Task RemoveRoleAsync`: Removes a role from a user. This method throws an exception if role removal fails.

## Usage
Here are two examples of using the `UserService` class:
```csharp
// Example 1: Authenticating a user
var userService = new UserService();
var user = await userService.AuthenticateAsync("username", "password");
if (user != null)
{
    Console.WriteLine("User authenticated successfully");
}
else
{
    Console.WriteLine("Authentication failed");
}

// Example 2: Creating a new user and assigning a role
var userService = new UserService();
var newUser = await userService.CreateUserAsync("newusername", "newpassword", "newemail");
await userService.AssignRoleAsync(newUser, "admin");
Console.WriteLine("New user created and assigned admin role");
```

## Notes
When using the `UserService` class, note that all methods are asynchronous and may throw exceptions if operations fail. It is essential to handle these exceptions properly to ensure robust error handling. Additionally, since the `UserService` class interacts with user data, it is crucial to consider thread-safety when using this class in multi-threaded environments. The `UserService` class is designed to be thread-safe, but it is still important to follow best practices for concurrent programming to avoid potential issues. Edge cases, such as duplicate user creation or role assignment, should also be considered when implementing authentication and authorization logic using the `UserService` class.
