# CreateUserRequest
The `CreateUserRequest` type in the `dotnet-auth-server` project represents a request to create a new user. It encapsulates the necessary information to create a user account, including username, email, password, and other optional attributes. This type is used to standardize the data required for user creation and to provide a clear structure for handling user registration requests.

## API
The `CreateUserRequest` type has the following public members:
* `Username`: a string representing the desired username for the new user.
* `Email`: a string representing the email address of the new user.
* `Password`: a string representing the password for the new user.
* `FullName`: a nullable string representing the full name of the new user.
* `Roles`: an ICollection of strings representing the roles assigned to the new user.
* `IsActive`: a nullable boolean indicating whether the new user is active.
* `Attributes`: a nullable Dictionary of strings to objects representing additional attributes for the new user.
* `Role`: a string representing a single role assigned to the new user.
* `CurrentPassword`: a string representing the current password of an existing user.
* `NewPassword`: a string representing the new password for an existing user.
* `UserId`: a string representing the ID of an existing user.
* `EmailVerified`: a boolean indicating whether the email address of the new user is verified.
* `CreatedAt`: a DateTime representing the date and time when the user was created.

## Usage
Here are two examples of using the `CreateUserRequest` type:
```csharp
// Example 1: Creating a new user with basic information
var request = new CreateUserRequest
{
    Username = "johnDoe",
    Email = "johndoe@example.com",
    Password = "password123",
    FullName = "John Doe",
    Roles = new List<string> { "user" }
};

// Example 2: Creating a new user with additional attributes
var request2 = new CreateUserRequest
{
    Username = "janeDoe",
    Email = "janedoe@example.com",
    Password = "password123",
    FullName = "Jane Doe",
    Roles = new List<string> { "admin" },
    Attributes = new Dictionary<string, object>
    {
        { "phoneNumber", "123-456-7890" },
        { "address", "123 Main St" }
    }
};
```

## Notes
When using the `CreateUserRequest` type, consider the following edge cases:
* The `Username` and `Email` properties must be unique among all users.
* The `Password` property must meet the password strength requirements defined by the authentication system.
* The `Roles` property can contain multiple roles, but each role must be a valid and existing role in the system.
* The `Attributes` property can contain any additional information about the user, but the keys and values must be serializable.
* The `IsActive` property determines whether the user account is active or inactive. Inactive users may not be able to log in or access certain features.
* The `EmailVerified` property indicates whether the email address of the user has been verified. Unverified email addresses may not be used for certain features, such as password recovery.
* The `CreatedAt` property is automatically set when the user is created and represents the date and time of creation.
Regarding thread-safety, the `CreateUserRequest` type is designed to be immutable, and its properties are not intended to be modified after creation. However, the `Roles` and `Attributes` properties are collections, which can be modified. To ensure thread-safety, it is recommended to create a new instance of the `CreateUserRequest` type for each user creation request, rather than reusing an existing instance.
