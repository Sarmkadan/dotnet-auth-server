# GrantType

`GrantType` defines the OAuth 2.0 and OpenID Connect grant flows represented by the domain model. It is declared in `src/Domain/Enums/GrantType.cs` in the `DotnetAuthServer.Domain.Enums` namespace.

## Values

| Enum value | Numeric value | Protocol identifier | Meaning |
| --- | ---: | --- | --- |
| `AuthorizationCode` | `0` | `authorization_code` | An interactive user flow in which the authorization endpoint returns a short-lived code to the client. The client exchanges that code at the token endpoint. This keeps tokens out of the browser redirect and supports confidential clients as well as public clients using PKCE. |
| `ClientCredentials` | `1` | `client_credentials` | A non-interactive machine-to-machine flow. The client authenticates as itself and receives an access token for its own permissions; there is no end-user authorization step. |
| `ResourceOwnerPasswordCredentials` | `2` | `password` | A legacy flow in which the client collects the resource owner's username and password and sends them to the token endpoint. It requires a highly trusted client and should be avoided for new applications because it exposes user credentials to the client and does not fit modern MFA or federated sign-in. |
| `RefreshToken` | `3` | `refresh_token` | A renewal flow in which a previously issued refresh token is exchanged for a new access token, usually without prompting the user to sign in again. The server must validate the refresh token and its scope before issuing replacement tokens. |
| `Implicit` | `4` | `implicit` | A legacy browser-oriented flow in which tokens are returned directly from the authorization endpoint instead of through a code exchange. New clients should use Authorization Code with PKCE because direct token delivery through the browser has weaker security properties. |
| `Hybrid` | `5` | `hybrid` | An OpenID Connect flow combining authorization-code and implicit-style responses. Some tokens are returned from the authorization endpoint while a code is also returned for token-endpoint exchange. It is primarily relevant to legacy or specialized OpenID Connect clients. |

## Usage notes

- The numeric values are implicit and follow declaration order. Persisted numeric enum values therefore depend on that order remaining stable.
- The enum member names are CLR identifiers, not values sent in OAuth requests. Protocol messages and client configuration use the string identifiers shown above, exposed by `DotnetAuthServer.Configuration.Constants.GrantTypes`.
- Authorization Code, Implicit, and Hybrid are user-facing authorization flows. Client Credentials represents the client itself, Refresh Token extends an existing authorization, and Resource Owner Password Credentials directly handles a user's credentials.
- Supporting a flow in configuration does not by itself authorize every client to use it. A client must also be registered or configured for the corresponding grant type.
