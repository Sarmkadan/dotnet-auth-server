# ResourceServerStartupExample

The `ResourceServerStartupExample` class provides a foundational implementation template for configuring an ASP.NET Core-based resource server, demonstrating the setup of middleware, dependency injection, and controller-based request handling within an authentication-centric architecture. It illustrates the typical lifecycle of an application startup, including service registration and pipeline configuration, alongside functional implementations for user profile management, token validation, and secure content operations.

## API

### Configuration Methods

*   **`void ConfigureServices(IServiceCollection services)`**
    Configures the application's dependency injection container, registering necessary services such as controllers, authentication handlers, and middleware components.
*   **`void Configure(IApplicationBuilder app, IWebHostEnvironment env)`**
    Configures the HTTP request pipeline, defining how incoming requests are processed by registering middleware components like routing, authentication, and authorization in the required order.

### UserController

*   **`UserController`**
    A controller class responsible for handling requests related to user information, profile access, and content management.

*   **`IActionResult GetPublicData()`**
    Returns publicly accessible data that does not require authentication.
*   **`IActionResult GetProfile()`**
    Retrieves the authenticated user's profile information; typically requires an authorized session.
*   **`IActionResult GetAllUsers()`**
    Returns a collection of user information; usually restricted to administrative roles or specific authorization scopes.
*   **`IActionResult GetData()`**
    Fetches generic data resources; implementation should verify appropriate user authorization.
*   **`IActionResult CreateContent()`**
    Processes a request to create new content; requires authorization to perform write operations.
*   **`async Task<IActionResult> DeleteContentAsync()`**
    Asynchronously handles the deletion of existing content, ensuring appropriate authorization checks are performed before the operation.

### Token Handling

*   **`IActionResult ValidateToken(string token)`**
    Validates the integrity and claims of a provided token, returning the outcome of the validation process.

*   **`TokenValidationMiddleware`**
    Custom middleware component designed to inspect incoming requests for valid authentication tokens and enforce access policies.
*   **`async Task InvokeAsync(HttpContext context)`**
    The main execution method for the `TokenValidationMiddleware`, intercepting HTTP requests to perform token validation before passing control to the next component in the pipeline.

### Properties

*   **`string Title`**
    Gets or sets the title associated with the resource server or its specific configuration instance.
*   **`string Description`**
    Gets or sets a detailed description of the resource server's purpose or configuration.
*   **`long MaxAgeSeconds`**
    Gets or sets the maximum allowed age for tokens or cache entries in seconds.
*   **`TokenAgeRequirement`**
    Represents the configured policy requirements regarding the allowed age of authentication tokens.

## Usage

### Configuring Startup

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Register application services
    services.AddControllers();
    services.AddAuthentication("Bearer").AddJwtBearer();
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app.UseRouting();
    app.UseMiddleware<TokenValidationMiddleware>();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseEndpoints(endpoints => endpoints.MapControllers());
}
```

### Implementing Secure Controller Logic

```csharp
[Authorize]
public class UserController : ControllerBase
{
    [HttpGet("profile")]
    public IActionResult GetProfile()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Ok(new { UserId = userId, Status = "Authenticated" });
    }

    [HttpDelete("content/{id}")]
    public async Task<IActionResult> DeleteContentAsync(int id)
    {
        // Asynchronous deletion logic
        await _contentService.DeleteAsync(id);
        return NoContent();
    }
}
```

## Notes

*   **Thread Safety**: The `ConfigureServices` and `Configure` methods are invoked during application startup by the ASP.NET Core runtime and are generally not subject to concurrent execution within the same request lifecycle. Controllers such as `UserController` are transient by default; ensure that any shared services injected into controllers are either thread-safe or properly scoped to prevent concurrency issues.
*   **Async Operations**: Methods suffixed with `Async` (e.g., `DeleteContentAsync`) must be awaited to ensure non-blocking I/O operations and proper exception handling.
*   **Middleware Ordering**: The order in which `TokenValidationMiddleware` is registered in `Configure` is critical. It must be placed early in the pipeline if it is intended to block unauthorized requests before they reach authentication or MVC routing middleware.
