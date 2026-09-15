# TokenType

`TokenType` represents token formats or authentication schemes in the domain model. It is declared in `src/Domain/Enums/TokenType.cs` in the `DotnetAuthServer.Domain.Enums` namespace.

## Values

| Enum value | Numeric value | Meaning |
| --- | ---: | --- |
| `Bearer` | `0` | A bearer token used to authorize API access. Possession of the token is sufficient to use it, so it must be protected in storage and transit. |
| `Mac` | `1` | A Message Authentication Code (MAC) token. Requests using this token type prove possession of key material by including a message authentication code rather than relying only on possession of the token string. |
| `SAML` | `2` | A token represented by a Security Assertion Markup Language (SAML) assertion. |

## Usage notes

- The numeric values are implicit and follow declaration order. Persisted numeric values therefore depend on the member order remaining stable.
- The enum is not marked with `Flags`; each value represents one token type and values are not intended to be combined.
- Enum member names are case-sensitive CLR identifiers. If a protocol or serialized representation requires a particular string, use the representation defined for that boundary rather than assuming every enum name is the required wire value.
- `DotnetAuthServer.Configuration.Constants.TokenTypes` contains token-type strings used elsewhere by the server and is a separate API. Its supported values do not necessarily match this enum.
- Do not confuse this type with `DotnetAuthServer.Domain.Entities.TokenType`, whose `Access` and `Refresh` values select client token lifetimes.

## Example

```csharp
using DotnetAuthServer.Domain.Enums;

TokenType tokenType = TokenType.Bearer;

if (tokenType == TokenType.Bearer)
{
    // Handle a bearer access token.
}
```
