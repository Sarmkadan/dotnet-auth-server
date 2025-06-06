# ClientRegistrationRequest

Represents a client registration request as defined by RFC 7591 (OAuth 2.0 Dynamic Client Registration Protocol), encapsulating all metadata required to register a new OAuth 2.0 client with the authorization server.

## API

### ClientName
```csharp
public string? ClientName { get; set; }
```
Human-readable name of the client to be presented to the end-user during authorization. Optional; if omitted, the authorization server may derive a name from other fields or assign a default.

### GrantTypes
```csharp
public ICollection<string> GrantTypes { get; set; }
```
Collection of OAuth 2.0 grant types the client intends to use. Values must be valid grant type identifiers (e.g., `authorization_code`, `client_credentials`, `refresh_token`). If omitted, the server typically defaults to `authorization_code`.

### RedirectUris
```csharp
public ICollection<string> RedirectUris { get; set; }
```
Collection of redirection URIs for the client. Required for clients using the authorization code or implicit flows. Each URI must be an absolute URI and must not contain a fragment component.

### ResponseTypes
```csharp
public ICollection<string> ResponseTypes { get; set; }
```
Collection of OAuth 2.0 response type values the client will use. Common values include `code`, `token`, `id_token`, and combinations thereof. If omitted, the server typically defaults to `code`.

### Scope
```csharp
public string? Scope { get; set; }
```
Space-delimited list of scope values the client requests. Optional; if omitted, the server may assign default scopes or require explicit configuration.

### TokenEndpointAuthMethod
```csharp
public string TokenEndpointAuthMethod { get; set; }
```
Authentication method the client will use at the token endpoint. Valid values include `client_secret_basic`, `client_secret_post`, `client_secret_jwt`, `private_key_jwt`, `tls_client_auth`, `self_signed_tls_client_auth`, and `none`. Defaults to `client_secret_basic` if not specified.

### LogoUri
```csharp
public string? LogoUri { get; set; }
```
URI referencing a logo image for the client. Optional; used for display purposes in consent screens.

### PolicyUri
```csharp
public string? PolicyUri { get; set; }
```
URI referencing the client's privacy policy. Optional; displayed to end-users during consent.

### TosUri
```csharp
public string? TosUri { get; set; }
```
URI referencing the client's terms of service. Optional; displayed to end-users during consent.

### Contacts
```csharp
public ICollection<string> Contacts { get; set; }
```
Collection of email addresses or other contact information for the client's administrators. Optional; used by the authorization server for operational communications.

### ClientUri
```csharp
public string? ClientUri { get; set; }
```
URI of the client's home page. Optional; used for display purposes in consent screens.

### IsValid
```csharp
public bool IsValid { get; }
```
Indicates whether the request contains all required fields and passes basic validation rules. Returns `true` when `RedirectUris` is non-empty for flows requiring redirection, `GrantTypes` contains recognized values, and `TokenEndpointAuthMethod` is a supported method. Does not perform network validation or verify URI reachability.

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

if (!request.IsValid)
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
- `IsValid` performs only syntactic validation. It does not verify that redirect URIs are HTTPS (though the authorization server will enforce this), that scopes exist, or that the client name is unique.
- `TokenEndpointAuthMethod` defaults to `client_secret_basic` when not explicitly set. Public clients (SPAs, mobile apps) must explicitly set this to `none` and use PKCE.
- URI properties (`LogoUri`, `PolicyUri`, `TosUri`, `ClientUri`, and all `RedirectUris`) accept any string; the authorization server will validate URI syntax and scheme requirements during registration.
- This type is not thread-safe. Instances should not be shared across threads while being mutated. Create a new instance per registration request.
