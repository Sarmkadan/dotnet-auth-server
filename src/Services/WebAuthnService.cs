// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DotnetAuthServer.Caching;
using DotnetAuthServer.Configuration;
using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Exceptions;
using Microsoft.Extensions.Logging;

namespace DotnetAuthServer.Services;

// ── DTOs / response models ─────────────────────────────────────────────────

/// <summary>Describes a public-key credential algorithm offered during a registration ceremony.</summary>
/// <param name="Type">Credential type. Always <c>"public-key"</c>.</param>
/// <param name="Alg">COSE algorithm identifier (e.g., <c>-7</c> for ES256, <c>-257</c> for RS256).</param>
public sealed record WebAuthnPublicKeyParam(string Type, int Alg);

/// <summary>Options returned to the client to begin a WebAuthn registration ceremony.</summary>
/// <param name="Challenge">Base64URL-encoded cryptographically random challenge (32 bytes).</param>
/// <param name="RpId">Relying Party effective domain.</param>
/// <param name="RpName">Human-readable Relying Party name.</param>
/// <param name="UserId">Base64URL-encoded opaque user handle.</param>
/// <param name="Username">Login username, displayed by the browser UI.</param>
/// <param name="DisplayName">Human-readable user display name.</param>
/// <param name="PubKeyCredParams">Ordered list of acceptable public-key algorithms.</param>
/// <param name="TimeoutMs">Ceremony timeout in milliseconds.</param>
/// <param name="UserVerification">UV requirement: <c>"required"</c>, <c>"preferred"</c>, or <c>"discouraged"</c>.</param>
public sealed record WebAuthnRegistrationOptions(
    string Challenge, string RpId, string RpName,
    string UserId, string Username, string DisplayName,
    IReadOnlyList<WebAuthnPublicKeyParam> PubKeyCredParams,
    int TimeoutMs, string UserVerification);

/// <summary>Options returned to the client to begin a WebAuthn authentication ceremony.</summary>
/// <param name="Challenge">Base64URL-encoded cryptographically random challenge (32 bytes).</param>
/// <param name="RpId">Relying Party effective domain.</param>
/// <param name="AllowCredentials">Base64URL-encoded credential IDs accepted by the server; empty for discoverable-credential flow.</param>
/// <param name="TimeoutMs">Ceremony timeout in milliseconds.</param>
/// <param name="UserVerification">UV requirement.</param>
public sealed record WebAuthnAuthenticationOptions(
    string Challenge, string RpId,
    IReadOnlyList<string> AllowCredentials,
    int TimeoutMs, string UserVerification);

// ── Credential store abstraction ───────────────────────────────────────────

/// <summary>
/// Provides persistent storage for <see cref="WebAuthnCredential"/> records.
/// Implement this interface to back the WebAuthn service with a real data store.
/// </summary>
public interface IWebAuthnCredentialStore
{
    /// <summary>Returns the active credential matching <paramref name="credentialId"/>, or <see langword="null"/>.</summary>
    Task<WebAuthnCredential?> FindByCredentialIdAsync(string credentialId, CancellationToken cancellationToken = default);

    /// <summary>Returns all active credentials registered for <paramref name="userId"/>.</summary>
    Task<IReadOnlyList<WebAuthnCredential>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Persists a newly registered credential.</summary>
    Task AddAsync(WebAuthnCredential credential, CancellationToken cancellationToken = default);

    /// <summary>Persists updates to an existing credential (e.g., counter, last-used timestamp).</summary>
    Task UpdateAsync(WebAuthnCredential credential, CancellationToken cancellationToken = default);
}

// ── Service interface ──────────────────────────────────────────────────────

/// <summary>
/// Exposes the WebAuthn/FIDO2 Level 2 four-step ceremony pair:
/// registration (<c>navigator.credentials.create</c>) and
/// authentication (<c>navigator.credentials.get</c>).
/// </summary>
public interface IWebAuthnService
{
    /// <summary>
    /// Generates <see cref="WebAuthnRegistrationOptions"/> containing the server challenge and RP parameters
    /// for the browser's <c>navigator.credentials.create()</c> call.
    /// </summary>
    /// <param name="userId">Unique identifier of the user registering a credential.</param>
    /// <param name="username">Login username (used for display only).</param>
    /// <param name="displayName">Human-readable full name of the user.</param>
    /// <param name="cancellationToken">Propagates cancellation notifications.</param>
    Task<WebAuthnRegistrationOptions> GenerateRegistrationOptionsAsync(
        string userId, string username, string displayName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies the authenticator attestation response from <c>navigator.credentials.create()</c>,
    /// then stores the resulting <see cref="WebAuthnCredential"/> if all checks pass.
    /// </summary>
    /// <param name="userId">The user for whom the credential is being registered.</param>
    /// <param name="clientDataJsonB64">Base64URL-encoded <c>clientDataJSON</c> from the authenticator.</param>
    /// <param name="attestationObjectB64">Base64URL-encoded CBOR <c>attestationObject</c> from the authenticator.</param>
    /// <param name="friendlyName">Optional human-readable label (e.g., "Touch ID").</param>
    /// <param name="cancellationToken">Propagates cancellation notifications.</param>
    Task<WebAuthnCredential> CompleteRegistrationAsync(
        string userId, string clientDataJsonB64, string attestationObjectB64,
        string? friendlyName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates <see cref="WebAuthnAuthenticationOptions"/> containing the server challenge
    /// for the browser's <c>navigator.credentials.get()</c> call.
    /// Omit <paramref name="userId"/> to enable the discoverable-credential (usernameless) flow.
    /// </summary>
    /// <param name="userId">Optional user ID to restrict which credentials are offered.</param>
    /// <param name="cancellationToken">Propagates cancellation notifications.</param>
    Task<WebAuthnAuthenticationOptions> GenerateAuthenticationOptionsAsync(
        string? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies the authenticator assertion returned by <c>navigator.credentials.get()</c>.
    /// Returns the authenticated user's subject identifier on success.
    /// </summary>
    /// <param name="credentialId">Base64URL-encoded credential ID selected by the authenticator.</param>
    /// <param name="clientDataJsonB64">Base64URL-encoded <c>clientDataJSON</c>.</param>
    /// <param name="authenticatorDataB64">Base64URL-encoded <c>authenticatorData</c>.</param>
    /// <param name="signatureB64">Base64URL-encoded assertion signature.</param>
    /// <param name="userHandle">Base64URL-encoded user handle supplied by the authenticator; may be <see langword="null"/>.</param>
    /// <param name="cancellationToken">Propagates cancellation notifications.</param>
    /// <returns>The subject (user ID) of the authenticated principal.</returns>
    Task<string> CompleteAuthenticationAsync(
        string credentialId, string clientDataJsonB64, string authenticatorDataB64,
        string signatureB64, string? userHandle = null,
        CancellationToken cancellationToken = default);
}

// ── Service implementation ─────────────────────────────────────────────────

/// <summary>
/// Implements WebAuthn Level 2 registration and authentication ceremonies with support for
/// ES256 (ECDSA/P-256, COSE <c>-7</c>) and RS256 (RSASSA-PKCS1-v1_5, COSE <c>-257</c>) algorithms.
/// Challenge lifecycle is managed through <see cref="ICacheService"/>.
/// </summary>
public sealed class WebAuthnService : IWebAuthnService
{
    private const int ChallengeLengthBytes = 32;
    private const int RegChallengeTtlSeconds = 300;    // 5 min
    private const int AuthChallengeTtlSeconds = 180;   // 3 min
    private const int CoseAlgEs256 = -7;
    private const int CoseAlgRs256 = -257;

    private readonly IWebAuthnCredentialStore _store;
    private readonly ICacheService _cache;
    private readonly ILogger<WebAuthnService> _logger;
    private readonly string _rpId;
    private readonly string _origin;
    private readonly string _rpName;

    /// <summary>Initializes a new <see cref="WebAuthnService"/> instance.</summary>
    public WebAuthnService(
        IWebAuthnCredentialStore store,
        ICacheService cache,
        AuthServerOptions options,
        ILogger<WebAuthnService> logger)
    {
        _store = store;
        _cache = cache;
        _logger = logger;

        var issuerUri = new Uri(options.IssuerUrl);
        _rpId = issuerUri.Host;
        _origin = $"{issuerUri.Scheme}://{issuerUri.Authority}";
        _rpName = issuerUri.Host;
    }

    /// <inheritdoc/>
    public async Task<WebAuthnRegistrationOptions> GenerateRegistrationOptionsAsync(
        string userId, string username, string displayName,
        CancellationToken cancellationToken = default)
    {
        var challenge = NewChallenge();
        await _cache.SetAsync($"webauthn:reg:{userId}", new ChallengeEntry(challenge),
            TimeSpan.FromSeconds(RegChallengeTtlSeconds), cancellationToken);

        _logger.LogDebug("Generated WebAuthn registration challenge for user {UserId}", userId);

        return new WebAuthnRegistrationOptions(
            Challenge: challenge,
            RpId: _rpId,
            RpName: _rpName,
            UserId: Base64UrlEncode(Encoding.UTF8.GetBytes(userId)),
            Username: username,
            DisplayName: displayName,
            PubKeyCredParams: [
                new("public-key", CoseAlgEs256),
                new("public-key", CoseAlgRs256)
            ],
            TimeoutMs: RegChallengeTtlSeconds * 1_000,
            UserVerification: "preferred");
    }

    /// <inheritdoc/>
    public async Task<WebAuthnCredential> CompleteRegistrationAsync(
        string userId, string clientDataJsonB64, string attestationObjectB64,
        string? friendlyName = null, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"webauthn:reg:{userId}";
        var entry = await _cache.GetAsync<ChallengeEntry>(cacheKey, cancellationToken)
            ?? throw new InvalidGrantException("Registration challenge expired or not found.");
        await _cache.RemoveAsync(cacheKey, cancellationToken);

        var clientDataBytes = Base64UrlDecode(clientDataJsonB64);
        VerifyClientData(clientDataBytes, entry.Value, "webauthn.create");

        var (_, authDataBytes) = ParseAttestationObject(Base64UrlDecode(attestationObjectB64));
        var authData = ParseAuthenticatorData(authDataBytes, requireAttestedData: true);

        VerifyRpIdHash(authData.RpIdHash);
        if ((authData.Flags & 0x01) == 0)
            throw new InvalidGrantException("User Presence flag not set in authenticatorData.");

        var (alg, pubKeyBytes) = ExtractPublicKey(authData.CoseKey);

        var credential = new WebAuthnCredential
        {
            CredentialId     = Base64UrlEncode(authData.CredentialId),
            PublicKey        = pubKeyBytes,
            Algorithm        = alg,
            SignatureCounter = authData.SignCount,
            UserId           = userId,
            FriendlyName     = friendlyName ?? "Security Key",
            AaGuid           = authData.AaGuid.Length == 16 ? new Guid(authData.AaGuid).ToString() : string.Empty,
            BackupEligible   = (authData.Flags & 0x08) != 0,
            BackedUp         = (authData.Flags & 0x10) != 0,
        };

        await _store.AddAsync(credential, cancellationToken);
        _logger.LogInformation(
            "Registered WebAuthn credential {CredentialId} (alg={Alg}) for user {UserId}",
            credential.CredentialId, alg, userId);

        return credential;
    }

    /// <inheritdoc/>
    public async Task<WebAuthnAuthenticationOptions> GenerateAuthenticationOptionsAsync(
        string? userId = null, CancellationToken cancellationToken = default)
    {
        var challenge = NewChallenge();
        await _cache.SetAsync($"webauthn:auth:{challenge}", new ChallengeEntry(userId ?? string.Empty),
            TimeSpan.FromSeconds(AuthChallengeTtlSeconds), cancellationToken);

        IReadOnlyList<string> allowCredentials = [];
        if (userId is not null)
        {
            var creds = await _store.GetByUserIdAsync(userId, cancellationToken);
            allowCredentials = creds.Select(c => c.CredentialId).ToList();
        }

        return new WebAuthnAuthenticationOptions(
            Challenge: challenge,
            RpId: _rpId,
            AllowCredentials: allowCredentials,
            TimeoutMs: AuthChallengeTtlSeconds * 1_000,
            UserVerification: "preferred");
    }

    /// <inheritdoc/>
    public async Task<string> CompleteAuthenticationAsync(
        string credentialId, string clientDataJsonB64, string authenticatorDataB64,
        string signatureB64, string? userHandle = null,
        CancellationToken cancellationToken = default)
    {
        var clientDataBytes = Base64UrlDecode(clientDataJsonB64);
        using var clientDoc = JsonDocument.Parse(clientDataBytes);
        var challenge = clientDoc.RootElement.GetProperty("challenge").GetString()
            ?? throw new InvalidGrantException("Missing challenge field in clientDataJSON.");

        var entry = await _cache.GetAsync<ChallengeEntry>($"webauthn:auth:{challenge}", cancellationToken)
            ?? throw new InvalidGrantException("Authentication challenge expired or not found.");
        await _cache.RemoveAsync($"webauthn:auth:{challenge}", cancellationToken);

        VerifyClientData(clientDataBytes, challenge, "webauthn.get");

        var credential = await _store.FindByCredentialIdAsync(credentialId, cancellationToken)
            ?? throw new InvalidGrantException($"Credential '{credentialId}' is not registered.");
        if (!credential.IsActive)
            throw new InvalidGrantException("The requested credential has been revoked.");

        var authDataBytes = Base64UrlDecode(authenticatorDataB64);
        var authData = ParseAuthenticatorData(authDataBytes, requireAttestedData: false);

        VerifyRpIdHash(authData.RpIdHash);
        if ((authData.Flags & 0x01) == 0)
            throw new InvalidGrantException("User Presence flag not set in authenticatorData.");

        // Counter regression indicates a potentially cloned authenticator.
        if (authData.SignCount != 0 && authData.SignCount <= credential.SignatureCounter)
        {
            _logger.LogWarning(
                "Signature counter regression for credential {CredId}: stored={Stored} received={Received}",
                credentialId, credential.SignatureCounter, authData.SignCount);
            throw new InvalidGrantException("Signature counter regression detected; credential may be cloned.");
        }

        var signedData = authDataBytes.Concat(SHA256.HashData(clientDataBytes)).ToArray();
        VerifySignature(credential, signedData, Base64UrlDecode(signatureB64));

        credential.SignatureCounter = authData.SignCount;
        credential.LastUsedAt = DateTime.UtcNow;
        await _store.UpdateAsync(credential, cancellationToken);

        var subject = userHandle is not null
            ? Encoding.UTF8.GetString(Base64UrlDecode(userHandle))
            : credential.UserId;

        _logger.LogInformation(
            "WebAuthn authentication succeeded for user {UserId} with credential {CredId}", subject, credentialId);

        return subject;
    }

    // ── Verification helpers ───────────────────────────────────────────────

    private void VerifyClientData(byte[] clientDataBytes, string expectedChallenge, string expectedType)
    {
        using var doc = JsonDocument.Parse(clientDataBytes);
        var root = doc.RootElement;

        var type = root.GetProperty("type").GetString();
        if (type != expectedType)
            throw new InvalidGrantException($"clientDataJSON type mismatch: expected '{expectedType}', got '{type}'.");

        var challenge = root.GetProperty("challenge").GetString();
        if (challenge != expectedChallenge)
            throw new InvalidGrantException("clientDataJSON challenge does not match the server-issued challenge.");

        var origin = root.GetProperty("origin").GetString();
        if (!string.Equals(origin, _origin, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("WebAuthn origin mismatch: expected={Expected} got={Actual}", _origin, origin);
            throw new InvalidGrantException($"clientDataJSON origin '{origin}' does not match server origin.");
        }
    }

    private void VerifyRpIdHash(byte[] rpIdHash)
    {
        if (!SHA256.HashData(Encoding.UTF8.GetBytes(_rpId)).SequenceEqual(rpIdHash))
            throw new InvalidGrantException("RP ID hash in authenticatorData does not match server RP ID.");
    }

    // ── AuthenticatorData parser ───────────────────────────────────────────

    private sealed record AuthData(
        byte[] RpIdHash, byte Flags, uint SignCount,
        byte[] AaGuid, byte[] CredentialId, byte[] CoseKey);

    private static AuthData ParseAuthenticatorData(byte[] data, bool requireAttestedData)
    {
        if (data.Length < 37)
            throw new InvalidGrantException("authenticatorData is too short.");

        var rpIdHash = data[0..32];
        var flags    = data[32];
        var signCount = (uint)((data[33] << 24) | (data[34] << 16) | (data[35] << 8) | data[36]);

        var hasAt = (flags & 0x40) != 0;
        if (requireAttestedData && !hasAt)
            throw new InvalidGrantException("AT flag not set in authenticatorData.");

        byte[] aaguid = [], credId = [], coseKey = [];

        if (hasAt)
        {
            if (data.Length < 55)
                throw new InvalidGrantException("authenticatorData too short for attested credential data.");

            aaguid = data[37..53];
            int credIdLen = (data[53] << 8) | data[54];
            int coseStart = 55 + credIdLen;

            if (data.Length < coseStart)
                throw new InvalidGrantException("Credential ID length overflows authenticatorData buffer.");

            credId  = data[55..coseStart];
            coseKey = data[coseStart..];
        }

        return new AuthData(rpIdHash, flags, signCount, aaguid, credId, coseKey);
    }

    // ── Attestation object parser (CBOR) ───────────────────────────────────

    private static (string Fmt, byte[] AuthData) ParseAttestationObject(byte[] data)
    {
        int pos = 0;
        int count = Cbor.MapSize(data, ref pos);
        string? fmt = null;
        byte[]? authData = null;

        for (int i = 0; i < count; i++)
        {
            var key = Cbor.Text(data, ref pos);
            switch (key)
            {
                case "fmt":      fmt      = Cbor.Text(data, ref pos);  break;
                case "authData": authData = Cbor.Bytes(data, ref pos); break;
                default:         Cbor.Skip(data, ref pos);             break; // attStmt etc.
            }
        }

        if (fmt is null || authData is null)
            throw new InvalidGrantException("Attestation object is missing required fields.");

        return (fmt, authData);
    }

    // ── COSE key extraction ────────────────────────────────────────────────

    private static (int Alg, byte[] PubKeyBytes) ExtractPublicKey(byte[] coseKey)
    {
        int pos = 0;
        int count = Cbor.MapSize(coseKey, ref pos);

        // Collect all map entries before interpreting — CBOR maps have no guaranteed key ordering.
        var intFields  = new Dictionary<long, long>();
        var byteFields = new Dictionary<long, byte[]>();

        for (int i = 0; i < count; i++)
        {
            long k = Cbor.Int(coseKey, ref pos);
            if (Cbor.PeekIsInt(coseKey, pos))
                intFields[k]  = Cbor.Int(coseKey, ref pos);
            else if (Cbor.PeekIsBytes(coseKey, pos))
                byteFields[k] = Cbor.Bytes(coseKey, ref pos);
            else
                Cbor.Skip(coseKey, ref pos);
        }

        int alg = intFields.TryGetValue(3, out var a) ? (int)a : 0;

        if (alg == CoseAlgEs256)
        {
            var x = byteFields.GetValueOrDefault(-2) ?? throw new InvalidGrantException("ES256 key missing x coordinate.");
            var y = byteFields.GetValueOrDefault(-3) ?? throw new InvalidGrantException("ES256 key missing y coordinate.");
            return (alg, PackBytes(x, y));
        }

        if (alg == CoseAlgRs256)
        {
            var n = byteFields.GetValueOrDefault(-1) ?? throw new InvalidGrantException("RS256 key missing modulus.");
            var e = byteFields.GetValueOrDefault(-2) ?? throw new InvalidGrantException("RS256 key missing exponent.");
            return (alg, PackBytes(n, e));
        }

        throw new InvalidGrantException($"Unsupported COSE algorithm {alg}. Supported: ES256 (-7), RS256 (-257).");
    }

    // ── Signature verification ─────────────────────────────────────────────

    private static void VerifySignature(WebAuthnCredential credential, byte[] signedData, byte[] signature)
    {
        var (part1, part2) = UnpackBytes(credential.PublicKey);

        if (credential.Algorithm == CoseAlgEs256)
        {
            var ecParams = new ECParameters { Curve = ECCurve.NamedCurves.nistP256, Q = new ECPoint { X = part1, Y = part2 } };
            using var ecdsa = ECDsa.Create(ecParams);
            // WebAuthn ECDSA signatures are DER-encoded ASN.1 SEQUENCE(r, s).
            if (!ecdsa.VerifyData(signedData, signature, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence))
                throw new InvalidGrantException("ECDSA assertion signature verification failed.");
            return;
        }

        if (credential.Algorithm == CoseAlgRs256)
        {
            var rsaParams = new RSAParameters { Modulus = part1, Exponent = part2 };
            using var rsa = RSA.Create(rsaParams);
            if (!rsa.VerifyData(signedData, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
                throw new InvalidGrantException("RSA assertion signature verification failed.");
            return;
        }

        throw new InvalidGrantException($"Signature verification not supported for algorithm {credential.Algorithm}.");
    }

    // ── Byte packing helpers ───────────────────────────────────────────────

    /// <summary>Serialises two byte arrays with 4-byte big-endian length prefixes for <see cref="WebAuthnCredential.PublicKey"/>.</summary>
    private static byte[] PackBytes(byte[] a, byte[] b)
    {
        var buf = new byte[4 + a.Length + 4 + b.Length];
        WriteLen(buf, 0, a.Length);
        a.CopyTo(buf, 4);
        WriteLen(buf, 4 + a.Length, b.Length);
        b.CopyTo(buf, 8 + a.Length);
        return buf;
    }

    private static (byte[] A, byte[] B) UnpackBytes(byte[] packed)
    {
        int aLen = ReadLen(packed, 0);
        int bOff = 4 + aLen;
        int bLen = ReadLen(packed, bOff);
        return (packed[4..(4 + aLen)], packed[(bOff + 4)..(bOff + 4 + bLen)]);
    }

    private static void WriteLen(byte[] buf, int off, int len)
    {
        buf[off] = (byte)(len >> 24); buf[off + 1] = (byte)(len >> 16);
        buf[off + 2] = (byte)(len >> 8); buf[off + 3] = (byte)len;
    }

    private static int ReadLen(byte[] buf, int off) =>
        (buf[off] << 24) | (buf[off + 1] << 16) | (buf[off + 2] << 8) | buf[off + 3];

    // ── Encoding ──────────────────────────────────────────────────────────

    private static string NewChallenge()
    {
        Span<byte> buf = stackalloc byte[ChallengeLengthBytes];
        RandomNumberGenerator.Fill(buf);
        return Base64UrlEncode(buf.ToArray());
    }

    private static string Base64UrlEncode(byte[] input) =>
        Convert.ToBase64String(input).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string input)
    {
        var s = input.Replace('-', '+').Replace('_', '/');
        s += (s.Length % 4) switch { 2 => "==", 3 => "=", _ => "" };
        return Convert.FromBase64String(s);
    }

    // ── Cache wrapper (avoids ambiguity between null-not-found and stored null) ──

    private sealed class ChallengeEntry(string value)
    {
        public string Value { get; } = value;
    }

    // ── Minimal CBOR reader ────────────────────────────────────────────────

    /// <summary>
    /// Minimal CBOR (RFC 7049) reader covering the subset used by WebAuthn attestation objects
    /// and COSE_Key structures: definite-length maps, arrays, byte strings, text strings,
    /// unsigned/negative integers, and tagged items.
    /// </summary>
    private static class Cbor
    {
        public static bool PeekIsInt(byte[] d, int pos)   => (d[pos] >> 5) is 0 or 1;
        public static bool PeekIsBytes(byte[] d, int pos) => (d[pos] >> 5) == 2;

        public static int MapSize(byte[] d, ref int pos) => (int)ReadArg(d, ref pos, ExpectMajor(d, ref pos, 5));

        public static string Text(byte[] d, ref int pos)
        {
            ulong len = ReadArg(d, ref pos, ExpectMajor(d, ref pos, 3));
            var s = Encoding.UTF8.GetString(d, pos, (int)len);
            pos += (int)len;
            return s;
        }

        public static byte[] Bytes(byte[] d, ref int pos)
        {
            ulong len = ReadArg(d, ref pos, ExpectMajor(d, ref pos, 2));
            var buf = d[pos..(pos + (int)len)];
            pos += (int)len;
            return buf;
        }

        public static long Int(byte[] d, ref int pos)
        {
            int hdr = d[pos++];
            int mt = hdr >> 5;
            if (mt is not 0 and not 1) throw new InvalidDataException($"CBOR: expected integer, got major type {mt}.");
            ulong val = ReadArg(d, ref pos, hdr & 0x1F);
            return mt == 0 ? (long)val : -1L - (long)val;
        }

        public static void Skip(byte[] d, ref int pos)
        {
            int hdr = d[pos++];
            int mt = hdr >> 5;
            int ai = hdr & 0x1F;

            switch (mt)
            {
                case 0 or 1:                          // uint / nint
                    ConsumeArgBytes(d, ref pos, ai);
                    break;
                case 2 or 3:                          // bytes / text
                    pos += (int)ReadArg(d, ref pos, ai);
                    break;
                case 4:                               // array
                    for (ulong n = ReadArg(d, ref pos, ai); n > 0; n--) Skip(d, ref pos);
                    break;
                case 5:                               // map
                    for (ulong n = ReadArg(d, ref pos, ai); n > 0; n--) { Skip(d, ref pos); Skip(d, ref pos); }
                    break;
                case 6:                               // tag: consume tag number then skip tagged item
                    ConsumeArgBytes(d, ref pos, ai);
                    Skip(d, ref pos);
                    break;
                case 7:                               // float / simple
                    ConsumeArgBytes(d, ref pos, ai);
                    break;
            }
        }

        private static int ExpectMajor(byte[] d, ref int pos, int expected)
        {
            int hdr = d[pos++];
            int mt = hdr >> 5;
            if (mt != expected) throw new InvalidDataException($"CBOR: expected major type {expected}, got {mt}.");
            return hdr & 0x1F;
        }

        private static void ConsumeArgBytes(byte[] d, ref int pos, int ai)
        {
            if (ai >= 24) pos += ai switch { 24 => 1, 25 => 2, 26 => 4, 27 => 8, _ => 0 };
        }

        private static ulong ReadArg(byte[] d, ref int pos, int ai)
        {
            if (ai < 24)  return (ulong)ai;
            if (ai == 24) return d[pos++];
            if (ai == 25) { ulong v = (ulong)((d[pos] << 8) | d[pos + 1]); pos += 2; return v; }
            if (ai == 26) { ulong v = (uint)((d[pos] << 24) | (d[pos + 1] << 16) | (d[pos + 2] << 8) | d[pos + 3]); pos += 4; return v; }
            if (ai == 27) { ulong v = 0; for (int i = 0; i < 8; i++) v = (v << 8) | d[pos + i]; pos += 8; return v; }
            throw new InvalidDataException($"CBOR: unsupported additional info {ai}.");
        }
    }
}
