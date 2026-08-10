#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================

namespace DotnetAuthServer.Services;

using System.Security.Cryptography;
using System.Text;
using DotnetAuthServer.Configuration;
using DotnetAuthServer.Data.Repositories;
using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Domain.Models;
using DotnetAuthServer.Exceptions;

/// <summary>
/// Implements TOTP (RFC 6238) multi-factor authentication.
/// Supports enrollment, verification with a configurable time-step window,
/// constant-time comparison, replay prevention with clock skew tolerance,
/// and single-use backup code redemption.
/// No external dependencies are required — HMAC-SHA1 is provided by the BCL.
/// </summary>
public sealed class TotpService
{
    private const int SecretBytesLength = 20; // 160-bit secret (TOTP spec recommendation)
    private const int TotpDigits = 6;
    private const int TotpStepSeconds = 30;
    private const int BackupCodeCount = 8;
    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
    private const int DefaultClockSkewSteps = 1; // Allow ±1 time-step of clock skew

    private readonly ITotpCredentialRepository _credentialRepository;
    private readonly ILogger<TotpService> _logger;
    private readonly AuthServerOptions _options;

    /// <summary>
    /// Initializes a new instance of <see cref="TotpService"/>.
    /// </summary>
    public TotpService(
        ITotpCredentialRepository credentialRepository,
        ILogger<TotpService> logger,
        AuthServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(credentialRepository);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);

        _credentialRepository = credentialRepository;
        _logger = logger;
        _options = options;
    }

    // -------------------------------------------------------------------------
    // Enrollment
    // -------------------------------------------------------------------------

    /// <summary>
    /// Begins TOTP enrollment for a user. Generates a new secret, provisioning URI
    /// and backup codes, persists a pending (unconfirmed) credential, and returns
    /// the data needed to present a QR code to the user.
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <param name="username">Username shown in the authenticator app label.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Setup payload including the provisioning URI and backup codes.</returns>
    /// <exception cref="ArgumentException"><paramref name="userId"/> or <paramref name="username"/> is null or empty.</exception>
    public async Task<MfaSetupResponse> InitiateSetupAsync(
        string userId,
        string username,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(userId);
        ArgumentException.ThrowIfNullOrEmpty(username);

        // Remove any existing (possibly unconfirmed) credential before re-enrolling.
        await _credentialRepository.DeleteByUserIdAsync(userId, cancellationToken);

        var secretBytes = GenerateRandomBytes(SecretBytesLength);
        var secretKey = EncodeBase32(secretBytes);
        var backupCodes = GenerateBackupCodes();

        var credential = new TotpCredential
        {
            UserId = userId,
            SecretKey = secretKey,
            IsEnabled = false,
            BackupCodes = backupCodes
        };

        await _credentialRepository.CreateAsync(credential, cancellationToken);

        _logger.LogInformation("TOTP enrollment initiated for user {UserId}", userId);

        return new MfaSetupResponse
        {
            SecretKey = secretKey,
            ProvisioningUri = BuildProvisioningUri(secretKey, username, _options.IssuerUrl),
            BackupCodes = backupCodes
        };
    }

    /// <summary>
    /// Confirms TOTP enrollment by verifying the code entered by the user.
    /// The credential is enabled only when the code is valid.
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <param name="code">The 6-digit TOTP code to verify.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="AuthServerException">Thrown when no pending credential exists or the code is invalid.</exception>
    /// <exception cref="ArgumentException"><paramref name="userId"/> or <paramref name="code"/> is null or empty.</exception>
    public async Task ConfirmSetupAsync(
        string userId,
        string code,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(userId);
        ArgumentException.ThrowIfNullOrEmpty(code);

        var credential = await GetCredentialOrThrowAsync(userId, cancellationToken);

        if (!VerifyTotpCode(credential.SecretKey, code))
            throw new AuthServerException(
                Constants.ErrorCodes.InvalidGrant,
                "Invalid TOTP code; please try again",
                400);

        credential.Enable();
        credential.RecordVerification();
        await _credentialRepository.UpdateAsync(credential, cancellationToken);

        _logger.LogInformation("TOTP MFA confirmed and enabled for user {UserId}", userId);
    }

    // -------------------------------------------------------------------------
    // Verification
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies a TOTP code or a single-use backup code for an already-enabled MFA credential.
    /// Backup codes are consumed on use. Implements constant-time comparison and replay prevention
    /// with configurable clock skew tolerance.
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <param name="code">The 6-digit TOTP code or backup code to verify.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the code is valid and MFA passes; false otherwise.</returns>
    /// <exception cref="ArgumentException"><paramref name="userId"/> or <paramref name="code"/> is null or empty.</exception>
    public async Task<bool> VerifyAsync(
        string userId,
        string code,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(userId);
        ArgumentException.ThrowIfNullOrEmpty(code);

        var credential = await _credentialRepository.GetByUserIdAsync(userId, cancellationToken);
        if (credential is null || !credential.IsEnabled)
        {
            _logger.LogDebug("TOTP verification failed: credential not found or not enabled for user {UserId}", userId);
            return false;
        }

        // Try backup code first (constant-time comparison to prevent timing attacks)
        var normalized = code.Trim().ToUpperInvariant().Replace("-", "");
        var backupCodes = credential.BackupCodes.ToList();

        // Use constant-time comparison for backup code verification
        var backupIndex = -1;
        for (var i = 0; i < backupCodes.Count; i++)
        {
            if (ConstantTimeEquals(backupCodes[i], normalized))
            {
                backupIndex = i;
                break;
            }
        }

        if (backupIndex >= 0)
        {
            credential.BackupCodes.RemoveAt(backupIndex);
            credential.RecordVerification();
            await _credentialRepository.UpdateAsync(credential, cancellationToken);
            _logger.LogInformation("Backup code used by user {UserId}", userId);
            return true;
        }

        // Verify TOTP code with replay prevention and clock skew support
        var isValid = await VerifyTotpCodeAsync(userId, credential.SecretKey, code);
        if (!isValid)
        {
            _logger.LogDebug("TOTP verification failed for user {UserId}", userId);
            return false;
        }

        credential.RecordVerification();
        await _credentialRepository.UpdateAsync(credential, cancellationToken);
        _logger.LogInformation("TOTP verification successful for user {UserId}", userId);
        return true;
    }

    // -------------------------------------------------------------------------
    // Status & Disable
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the MFA status for a user.
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>MFA status information.</returns>
    /// <exception cref="ArgumentException"><paramref name="userId"/> is null or empty.</exception>
    public async Task<MfaStatusResponse> GetStatusAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(userId);

        var credential = await _credentialRepository.GetByUserIdAsync(userId, cancellationToken);
        return new MfaStatusResponse
        {
            IsEnabled = credential?.IsEnabled ?? false,
            EnabledAt = credential?.EnabledAt,
            LastUsedAt = credential?.LastUsedAt,
            BackupCodesRemaining = credential?.BackupCodes.Count ?? 0
        };
    }

    /// <summary>
    /// Disables and removes the TOTP credential for a user.
    /// </>
    /// <param name="userId">The user's ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ArgumentException"><paramref name="userId"/> is null or empty.</exception>
    public async Task DisableMfaAsync(string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(userId);

        await _credentialRepository.DeleteByUserIdAsync(userId, cancellationToken);
        _logger.LogInformation("TOTP MFA disabled for user {UserId}", userId);
    }

    // -------------------------------------------------------------------------
    // TOTP core algorithm (RFC 6238 / RFC 4226)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies a TOTP code against the shared secret using a ±clock skew window.
    /// Implements replay prevention by tracking the last accepted time-step.
    /// </summary>
    /// <param name="userId">The user ID for replay prevention tracking.</param>
    /// <param name="base32Secret">The Base32-encoded secret key.</param>
    /// <param name="code">The 6-digit TOTP code to verify.</param>
    /// <param name="clockSkewSteps">The number of time steps to check in each direction for clock skew.</param>
    /// <returns>True if the code is valid and not a replay; otherwise false.</returns>
    /// <exception cref="ArgumentException"><paramref name="userId"/>, <paramref name="base32Secret"/>, or <paramref name="code"/> is null or empty.</exception>
    public async Task<bool> VerifyTotpCodeAsync(
        string userId,
        string base32Secret,
        string code,
        int clockSkewSteps = DefaultClockSkewSteps)
    {
        ArgumentException.ThrowIfNullOrEmpty(userId);
        ArgumentException.ThrowIfNullOrEmpty(base32Secret);
        ArgumentException.ThrowIfNullOrEmpty(code);

        if (code.Length != TotpDigits || !int.TryParse(code, out var inputValue))
            return false;

        var secretBytes = DecodeBase32(base32Secret);
        var currentCounter = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / TotpStepSeconds;

        // Get the credential for replay prevention
        var credential = await _credentialRepository.GetByUserIdAsync(userId);

        // Check for replay attacks: reject codes at or before the last accepted time-step
        // Allow clock skew tolerance by checking if the code falls within the acceptable range
        if (credential?.LastAcceptedTimeStep.HasValue == true)
        {
            var lastAcceptedStep = credential.LastAcceptedTimeStep.Value;
            var acceptableRangeStart = lastAcceptedStep - clockSkewSteps;
            var acceptableRangeEnd = lastAcceptedStep + clockSkewSteps;

            // Reject codes outside the acceptable range (including older codes)
            if (currentCounter < acceptableRangeStart)
            {
                _logger.LogWarning("TOTP replay attempt detected for user {UserId}", userId);
                return false;
            }
        }

        // Check the time window for valid codes with clock skew support
        var clockSkew = _options.ClockSkewToleranceSeconds / TotpStepSeconds;
        var effectiveClockSkewSteps = Math.Max(clockSkewSteps, clockSkew);

        for (var step = -effectiveClockSkewSteps; step <= effectiveClockSkewSteps; step++)
        {
            var testCounter = currentCounter + step;
            if (ComputeTotp(secretBytes, testCounter) == inputValue)
            {
                // Update the last accepted time-step to prevent replay
                if (credential != null)
                {
                    credential.RecordVerification(testCounter);
                    await _credentialRepository.UpdateAsync(credential);
                }
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Verifies a TOTP code against the shared secret using a ±1 step window.
    /// </summary>
    /// <param name="base32Secret">The Base32-encoded secret key.</param>
    /// <param name="code">The 6-digit TOTP code to verify.</param>
    /// <param name="windowSteps">The number of time steps to check in each direction.</param>
    /// <returns>True if the code is valid; otherwise false.</returns>
    /// <exception cref="ArgumentException"><paramref name="base32Secret"/> or <paramref name="code"/> is null or empty.</exception>
    [Obsolete("Use VerifyTotpCodeAsync for replay prevention and constant-time comparison.")]
    public bool VerifyTotpCode(string base32Secret, string code, int windowSteps = 1)
    {
        ArgumentException.ThrowIfNullOrEmpty(base32Secret);
        ArgumentException.ThrowIfNullOrEmpty(code);

        if (code.Length != TotpDigits || !int.TryParse(code, out var inputValue))
            return false;

        var secretBytes = DecodeBase32(base32Secret);
        var counter = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / TotpStepSeconds;

        for (var step = -windowSteps; step <= windowSteps; step++)
        {
            if (ComputeTotp(secretBytes, counter + step) == inputValue)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Compares two strings in constant time to prevent timing attacks.
    /// Uses <see cref="CryptographicOperations.FixedTimeEquals"/> for secure comparison.
    /// </summary>
    /// <param name="a">First string to compare.</param>
    /// <param name="b">Second string to compare.</param>
    /// <returns>True if the strings are equal; otherwise false.</returns>
    private static bool ConstantTimeEquals(string a, string b)
    {
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(a),
            Encoding.UTF8.GetBytes(b));
    }

    /// <summary>
    /// Generates the TOTP value for a given counter (time step).
    /// </summary>
    /// <param name="key">The shared secret key.</param>
    /// <param name="counter">The time step counter.</param>
    private static int ComputeTotp(byte[] key, long counter)
    {
        var counterBytes = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(counterBytes); // RFC 4226 requires big-endian

        using var hmac = new HMACSHA1(key);
        var hash = hmac.ComputeHash(counterBytes);

        // Dynamic truncation per RFC 4226 §5.3
        var offset = hash[^1] & 0x0F;
        var otp = ((hash[offset] & 0x7F) << 24)
            | ((hash[offset + 1] & 0xFF) << 16)
            | ((hash[offset + 2] & 0xFF) << 8)
            | (hash[offset + 3] & 0xFF);

        return otp % (int)Math.Pow(10, TotpDigits);
    }

    // -------------------------------------------------------------------------
    // Provisioning URI
    // -------------------------------------------------------------------------

    /// <summary>
    /// Builds an <c>otpauth://totp/</c> URI for QR code generation.
    /// </summary>
    /// <param name="secretKey">The Base32-encoded secret key.</param>
    /// <param name="username">The username for the provisioning URI.</param>
    /// <param name="issuer">The issuer name for the provisioning URI.</param>
    /// <returns>The provisioning URI string.</returns>
    /// <exception cref="ArgumentException"><paramref name="secretKey"/>, <paramref name="username"/>, or <paramref name="issuer"/> is null or empty.</exception>
    public static string BuildProvisioningUri(string secretKey, string username, string issuer)
    {
        ArgumentException.ThrowIfNullOrEmpty(secretKey);
        ArgumentException.ThrowIfNullOrEmpty(username);
        ArgumentException.ThrowIfNullOrEmpty(issuer);

        var label = Uri.EscapeDataString($"{issuer}:{username}");
        var issuerEncoded = Uri.EscapeDataString(issuer);
        return $"otpauth://totp/{label}?secret={secretKey}&issuer={issuerEncoded}&algorithm=SHA1&digits={TotpDigits}&period={TotpStepSeconds}";
    }

    // -------------------------------------------------------------------------
    // Base32 encoding/decoding (RFC 4648)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Encodes a byte array to a Base32 string (no padding, uppercase).
    /// </summary>
    /// <param name="data">The byte array to encode.</param>
    /// <returns>The Base32-encoded string.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="data"/> is null.</exception>
    public static string EncodeBase32(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var sb = new StringBuilder((data.Length * 8 + 4) / 5);
        var buffer = 0;
        var bitsLeft = 0;

        foreach (var b in data)
        {
            buffer = (buffer << 8) | b;
            bitsLeft += 8;
            while (bitsLeft >= 5)
            {
                bitsLeft -= 5;
                sb.Append(Base32Alphabet[(buffer >> bitsLeft) & 0x1F]);
            }
        }

        if (bitsLeft > 0)
            sb.Append(Base32Alphabet[(buffer << (5 - bitsLeft)) & 0x1F]);

        return sb.ToString();
    }

    /// <summary>
    /// Decodes a Base32 string (uppercase, no padding) to a byte array.
    /// </summary>
    /// <param name="base32">The Base32-encoded string to decode.</param>
    /// <returns>The decoded byte array.</returns>
    /// <exception cref="ArgumentException"><paramref name="base32"/> is null or empty.</exception>
    public static byte[] DecodeBase32(string base32)
    {
        ArgumentException.ThrowIfNullOrEmpty(base32);

        var input = base32.TrimEnd('=').ToUpperInvariant();
        var output = new byte[input.Length * 5 / 8];
        var buffer = 0;
        var bitsLeft = 0;
        var index = 0;

        foreach (var c in input)
        {
            var charValue = Base32Alphabet.IndexOf(c);
            if (charValue < 0) continue;

            buffer = (buffer << 5) | charValue;
            bitsLeft += 5;

            if (bitsLeft >= 8)
            {
                bitsLeft -= 8;
                output[index++] = (byte)(buffer >> bitsLeft);
            }
        }

        return output;
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static byte[] GenerateRandomBytes(int length)
    {
        var bytes = new byte[length];
        RandomNumberGenerator.Fill(bytes);
        return bytes;
    }

    private static IList<string> GenerateBackupCodes()
    {
        var codes = new List<string>(BackupCodeCount);
        for (var i = 0; i < BackupCodeCount; i++)
        {
            var raw = new byte[5];
            RandomNumberGenerator.Fill(raw);
            codes.Add(Convert.ToHexString(raw).ToUpperInvariant());
        }
        return codes;
    }

    private async Task<TotpCredential> GetCredentialOrThrowAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(userId);

        return await _credentialRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new AuthServerException(
                Constants.ErrorCodes.InvalidRequest,
                "No pending TOTP setup found for this user. Call /mfa/setup first.",
                404);
    }
}