# ClientRegistrationRequest

Represents a client registration request as defined by RFC 7591 (OAuth 2.0 Dynamic Client Registration Protocol), encapsulating all metadata required to register a new OAuth 2.0 client with the authorization server.

## API

### ClientName
```csharp
public string? ClientName { get; set; }
```
Human-readable name of the client application.

### GrantTypes
```csharp
public ICollection<string> GrantTypes { get; set; } = ["authorization_code"];
```
Grant types the client intends to use. Defaults to `["authorization_code"]` when omitted.

### RedirectUris
```csharp
public ICollection<string> RedirectUris { get; set; } = [];
```
Redirect URIs for authorization_code and implicit flows. Required when grant_types contains "authorization_code" or "implicit".

### ResponseTypes
```csharp
public ICollection<string> ResponseTypes { get; set; } = ["code"];
```
Response types the client will use. Defaults to `["code"]` when omitted.

### Scope
```csharp
public string? Scope { get; set; }
```
Requested scopes as a space-delimited string.

### TokenEndpointAuthMethod
```csharp
public string TokenEndpointAuthMethod { get; set; } = "client_secret_basic";
```
Authentication method for the token endpoint. "none" for public clients; "client_secret_post" or "client_secret_basic" for confidential clients. Defaults to "client_secret_basic".

### LogoUri
```csharp
public string? LogoUri { get; set; }
```
URI of the client logo.

### PolicyUri
```csharp
public string? PolicyUri { get; set; }
```
URI of the client privacy policy.

### TosUri
```csharp
public string? TosUri { get; set; }
```
URI of the client terms of service.

### Contacts
```csharp
public ICollection<string> Contacts { get; set; } = [];
```
Array of e-mail addresses for the client contacts.

### ClientUri
```csharp
public string? ClientUri { get; set; }
```
URI of the client home page.

### IsValid
```csharp
public bool IsValid();
```
Returns true when the request carries the minimum required fields.

## Usage

### Registering a confidential web application
```csharp
var request = new ClientRegistrationRequest
{
    ClientName = "Acme Corp Dashboard",
    GrantTypes = new[] { "authorization_code", "refresh_token" },
    RedirectUris = new[] { "https://dashboard.acme.com/callback" },
    ResponseTypes = new[] { "code" },
    Scope = "openid profile email offline_access",
    TokenEndpointAuthMethod = "client_secret_basic",
    LogoUri = "https://dashboard.acme.com/logo.png",
    PolicyUri = "https://acme.com/privacy",
    TosUri = "https://acme.com/terms",
    Contacts = new[] { "security@acme.com", "devops@acme.com" },
    ClientUri = "https://dashboard.acme.com"
};

if (!request.IsValid())
{
    throw new InvalidOperationException("Invalid client registration request");
}

var response = await httpClient.PostAsJsonAsync("/connect/register", request);
```

### Registering a public single-page application with PKCE
```csharp
var request = new ClientRegistrationRequest
{
    ClientName = "Acme SPA",
    GrantTypes = new[] { "authorization_code" },
    RedirectUris = new[] { "https://app.acme.com/auth/callback" },
    ResponseTypes = new[] { "code" },
    Scope = "openid profile api.read",
    TokenEndpointAuthMethod = "none",
    Contacts = new[] { "frontend-team@acme.com" }
};

var registration = await client.RegisterAsync(request);
// registration.ClientId and registration.ClientSecret (if issued) are now available
```

## Notes

- All collection properties (`GrantTypes`, `RedirectUris`, `ResponseTypes`, `Contacts`) are mutable reference types. Callers should avoid modifying collections after passing the request to registration endpoints to prevent race conditions in multi-threaded scenarios.
- The `IsValid()` method performs validation: returns false if ClientName is null/whitespace, or if authorization_code/implicit grant types are specified without any redirect URIs.
- Default values are applied automatically: GrantTypes = ["authorization_code"], ResponseTypes = ["code"], TokenEndpointAuthMethod = "client_secret_basic".
- URI properties accept any string; validation of URI syntax and scheme requirements is performed by the authorization server during registration.
- This type is not thread-safe. Instances should not be shared across threads while being mutated. Create a new instance per registration request.
