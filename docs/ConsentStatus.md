# ConsentStatus

`ConsentStatus` represents the current state of a user's consent to a client's requested scope permissions. It is declared in `src/Domain/Enums/ConsentStatus.cs` in the `DotnetAuthServer.Domain.Enums` namespace.

## Values

| Enum value | Numeric value | Meaning |
| --- | ---: | --- |
| `Pending` | `0` | The user has not yet granted or denied the requested consent. |
| `Approved` | `1` | The user has explicitly granted the requested consent. |
| `Rejected` | `2` | The user has explicitly denied the requested consent. |
| `Expired` | `3` | Previously granted consent is no longer valid because it has expired. |

## Usage notes

- New consent records should use `Pending` until the user makes a decision.
- Only `Approved` represents active permission to use the consented scopes. Callers should not treat `Pending`, `Rejected`, or `Expired` as authorization.
- The numeric values are implicit and follow declaration order. Persisted numeric values therefore depend on that order remaining stable.
