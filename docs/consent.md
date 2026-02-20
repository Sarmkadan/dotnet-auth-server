# Consent Screen: Customization & Flow Reference

This document explains how user consent works in dotnet-auth-server, how consent decisions
are stored, how they interact with `prompt=consent`, and how to replace the default consent
UI with your own.

---

## Table of Contents

1. [Consent Storage Model](#consent-storage-model)
2. [How Consent Is Checked During Authorization](#how-consent-is-checked-during-authorization)
3. [The `prompt` Parameter](#the-prompt-parameter)
4. [Revoking Consent](#revoking-consent)
5. [Customizing the Consent UI](#customizing-the-consent-ui)
6. [Session-Based vs. Remembered Consent](#session-based-vs-remembered-consent)

---

## Consent Storage Model

Consent is stored **per-user per-client** as a single `Consent` record that contains the
complete set of scopes the user approved.  There is **no separate record per scope** —
granting `openid profile` produces one record listing both scopes.

| Field | Description |
|---|---|
| `ConsentId` | Unique identifier (UUID) |
| `UserId` | The authenticated user |
| `ClientId` | The OAuth2 client |
| `GrantedScopes` | Space-separated list of approved scopes |
| `Status` | `Pending`, `Granted`, or `Denied` |
| `ExpiresAt` | `null` for remembered consent; set for session-based consent |
| `CreatedAt` / `UpdatedAt` | Audit timestamps |

When a new authorization request arrives, `ConsentService.HasConsentAsync` checks whether
an active (`Status == Granted`, not expired) consent record exists for the
`(userId, clientId)` pair **and** that every requested scope is covered by the stored
`GrantedScopes`.  If any scope is missing the user is sent to the consent screen again.

---

## How Consent Is Checked During Authorization

```
GET /oauth/authorize
  → AuthorizationService.ValidateAuthorizationRequestAsync
      → ConsentService.HasConsentAsync(userId, clientId, requestedScopes)
          → true  → skip consent screen, issue authorization code
          → false → redirect user to consent screen
```

After the user approves on the consent screen:

```
POST /oauth/authorize/consent
  → ConsentService.RecordConsentAsync(ConsentRequest)
      → creates or updates Consent record
      → issues authorization code
```

---

## The `prompt` Parameter

| Value | Behaviour |
|---|---|
| *(not set)* | Show consent screen only when no valid consent exists |
| `none` | Never show consent screen; fail with `interaction_required` if consent is missing |
| `consent` | Always show consent screen, even if valid consent already exists |
| `login` | Force re-authentication; does not directly affect consent |

`prompt=consent` is handled in `AuthorizationService` by bypassing the
`ConsentService.HasConsentAsync` check so the screen always appears.  The previously stored
consent record is **not deleted** — it is overwritten when the user submits the form.

---

## Revoking Consent

### Per client (user action)

```csharp
await consentService.RevokeConsentAsync(userId, clientId, cancellationToken);
```

The consent record's `Status` is set to `Revoked`.  Subsequent authorization requests for
the same `(userId, clientId)` will show the consent screen again.

### All consents for a user (account closure / GDPR)

```csharp
await consentService.RevokeAllUserConsentsAsync(userId, cancellationToken);
```

### Via the repository directly

```csharp
var consent = await consentRepository.GetByUserAndClientAsync(userId, clientId);
consent?.Revoke("reason");
await consentRepository.UpdateAsync(consent!);
```

---

## Customizing the Consent UI

The authorization endpoint at `GET /oauth/authorize/consent` returns a JSON payload by
default.  To replace it with a custom HTML/Razor view:

### 1. Return an HTML view from the controller

```csharp
// src/Controllers/AuthorizationController.cs
[HttpGet("consent")]
public async Task<IActionResult> GetConsentPromptAsync(
    [FromQuery] string? client_id,
    [FromQuery] string? user_id,
    [FromQuery] string? scope,
    CancellationToken cancellationToken)
{
    var client = await _clientRepository.GetActiveClientAsync(client_id!, cancellationToken);
    var model = new ConsentViewModel
    {
        ClientId    = client_id!,
        ClientName  = client?.ClientName ?? client_id!,
        LogoUri     = client?.LogoUri,
        UserId      = user_id!,
        Scopes      = scope?.Split(' ') ?? [],
        PolicyUri   = client?.PolicyUri,
        TermsUri    = client?.TermsOfServiceUri
    };
    return View("Consent", model); // Razor view in Views/Authorization/Consent.cshtml
}
```

### 2. Create a `ConsentViewModel`

```csharp
public record ConsentViewModel(
    string ClientId,
    string ClientName,
    string? LogoUri,
    string UserId,
    string[] Scopes,
    string? PolicyUri,
    string? TermsUri);
```

### 3. Add the Razor view (`Views/Authorization/Consent.cshtml`)

```html
@model ConsentViewModel
<form method="post" action="/oauth/authorize/consent">
    <input type="hidden" name="client_id" value="@Model.ClientId" />
    <input type="hidden" name="user_id"   value="@Model.UserId" />

    <h1>@Model.ClientName is requesting access</h1>

    @foreach (var scope in Model.Scopes)
    {
        <label>
            <input type="checkbox" name="granted_scopes" value="@scope" checked />
            @scope
        </label>
    }

    <label>
        <input type="checkbox" name="remember_consent" value="true" />
        Remember my decision
    </label>

    <button name="approved" value="true">Allow</button>
    <button name="approved" value="false">Deny</button>
</form>
```

### 4. Enable MVC views in `Program.cs`

```csharp
builder.Services.AddControllersWithViews(); // instead of AddControllers()
```

### 5. Brand the screen with CSS / JavaScript

Place static assets in `wwwroot/` and reference them from your Razor layout.  The consent
controller passes `LogoUri`, `PolicyUri`, and `TermsOfServiceUri` from the registered
client record so those can be displayed without hard-coding anything.

---

## Session-Based vs. Remembered Consent

When `ConsentRequest.RememberConsent == false` (the default) the stored `Consent` record
expires after **1 hour** (`ExpiresAt = DateTime.UtcNow.AddHours(1)`).  When the user ticks
"Remember my decision" set `RememberConsent = true`; the record has no expiry and persists
until explicitly revoked.

To change the session-consent lifetime, update `ConsentService.RecordConsentAsync`:

```csharp
if (!request.RememberConsent)
{
    consent.ExpiresAt = DateTime.UtcNow.AddHours(8); // extend to a workday
}
```

---

For related topics see:
- [Getting Started](./getting-started.md)
- [FAQ – Can I customize the consent UI?](./faq.md#can-i-customize-the-consent-ui)
- `src/Services/ConsentService.cs` — `ConsentService` and `ConsentRepository`
- `src/Domain/Entities/Consent.cs` — `Consent` entity
