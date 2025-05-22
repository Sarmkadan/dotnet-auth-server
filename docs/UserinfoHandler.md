# UserinfoHandler

Represents a handler for processing OpenID Connect userinfo claims. It encapsulates user profile data retrieved from an identity provider and exposes it in a structured format suitable for downstream services. The type is designed to be used in authentication middleware pipelines where user claims need to be normalized and exposed to application logic.

## API

### `public UserinfoHandler`

Initializes a new instance of the `UserinfoHandler` with default or provided user information. This constructor is typically invoked by dependency injection or factory methods when constructing a response object for the userinfo endpoint.

### `public async Task<UserinfoResponse?> GetUserinfoAsync`

Asynchronously retrieves the userinfo response for the authenticated subject. This method coordinates with the underlying identity provider to fetch the latest user claims. Returns `null` if the user is not found or the request fails. May throw `InvalidOperationException` if the handler is not properly initialized or if required configuration is missing.

### `public string Sub`

Gets the subject identifier (`sub`) of the authenticated user. This value uniquely identifies the user at the authorization server and is required in all userinfo responses. Must not be `null` or empty.

### `public string? Name`

Gets the full name of the user. Optional; may be `null` if not provided by the identity provider.

### `public string? GivenName`

Gets the given (first) name of the user. Optional; may be `null` if not provided by the identity provider.

### `public string? FamilyName`

Gets the family (last) name of the user. Optional; may be `null` if not provided by the identity provider.

### `public long? UpdatedAt`

Gets the Unix timestamp (in seconds) indicating when the user's information was last updated. Optional; may be `null` if not provided by the identity provider.

### `public string? Email`

Gets the user's email address. Optional; may be `null` if not provided by the identity provider.

### `public bool? EmailVerified`

Gets a value indicating whether the user's email address has been verified. Optional; may be `null` if not provided by the identity provider.

### `public AddressInfo? Address`

Gets the user's physical address. Optional; may be `null` if not provided by the identity provider. Contains sub-properties such as `StreetAddress`, `Locality`, `Region`, `PostalCode`, and `Country`.

### `public string? PhoneNumber`

Gets the user's phone number. Optional; may be `null` if not provided by the identity provider.

### `public bool? PhoneNumberVerified`

Gets a value indicating whether the user's phone number has been verified. Optional; may be `null` if not provided by the identity provider.

### `public string? StreetAddress`

Gets the street address component of the user's address. Optional; may be `null` if not provided by the identity provider.

### `public string? Locality`

Gets the city or locality component of the user's address. Optional; may be `null` if not provided by the identity provider.

### `public string? Region`

Gets the state or region component of the user's address. Optional; may be `null` if not provided by the identity provider.

### `public string? PostalCode`

Gets the postal code component of the user's address. Optional; may be `null` if not provided by the identity provider.

### `public string? Country`

Gets the country component of the user's address. Optional; may be `null` if not provided by the identity provider.

## Usage
