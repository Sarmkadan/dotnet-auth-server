#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

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
/// and single-use backup code redemption.
/// No external dependencies are required — HMAC-SHA1 is provided by the BCL.
/// </summary>
public sealed class TotpService
{
    private const int SecretBytesLength = 20;   // 160-bit secret (TOTP spec recommendation)
    private const int TotpDigits = 6;
    private const int TotpStepSeconds = 30;
    private const int BackupCodeCount = 8;
    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

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
    public async Task<MfaSetupResponse> InitiateSetupAsync(
        string userId,
        string username,
        CancellationToken cancellationToken = default)
    {
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
    /// <exception cref="AuthServerException">Thrown when no pending credential exists or the code is invalid.</exception>
    public async Task ConfirmSetupAsync(
        string userId,
        string code,
        CancellationToken cancellationToken = default)
    {
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
    /// Backup codes are consumed on use.
    /// </summary>
    /// <returns>True if the code is valid and MFA passes; false otherwise.</returns>
    public async Task<bool> VerifyAsync(
        string userId,
        string code,
        CancellationToken cancellationToken = default)
    {
        var credential = await _credentialRepository.GetByUserIdAsync(userId, cancellationToken);
        if (credential is null || !credential.IsEnabled) return false;

        // Try backup code first (exact match, case-insensitive).
        var normalized = code.Trim().ToUpperInvariant().Replace("-", "");
        var backupIndex = credential.BackupCodes.ToList()
            .FindIndex(c => c.Equals(normalized, StringComparison.OrdinalIgnoreCase));

        if (backupIndex >= 0)
        {
            credential.BackupCodes.RemoveAt(backupIndex);
            credential.RecordVerification();
            await _credentialRepository.UpdateAsync(credential, cancellationToken);
            _logger.LogInformation("Backup code used by user {UserId}", userId);
            return true;
        }

        if (!VerifyTotpCode(credential.SecretKey, code)) return false;

        credential.RecordVerification();
        await _credentialRepository.UpdateAsync(credential, cancellationToken);
        return true;
    }

    // -------------------------------------------------------------------------
    // Status & Disable
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the MFA status for a user.
    /// </summary>
    public async Task<MfaStatusResponse> GetStatusAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
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
    /// </summary>
    public async Task DisableMfaAsync(string userId, CancellationToken cancellationToken = default)
    {
        await _credentialRepository.DeleteByUserIdAsync(userId, cancellationToken);
        _logger.LogInformation("TOTP MFA disabled for user {UserId}", userId);
    }

    // -------------------------------------------------------------------------
    // TOTP core algorithm (RFC 6238 / RFC 4226)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies a TOTP code against the shared secret using a ±1 step window.
    /// </summary>
    public bool VerifyTotpCode(string base32Secret, string code, int windowSteps = 1)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != TotpDigits) return false;
        if (!int.TryParse(code, out var inputValue)) return false;

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
    /// Generates the TOTP value for a given counter (time step).
    /// </summary>
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
    public static string BuildProvisioningUri(string secretKey, string username, string issuer)
    {
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
    public static string EncodeBase32(byte[] data)
    {
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
    public static byte[] DecodeBase32(string base32)
    {
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
        string userId, CancellationToken cancellationToken)
    {
        return await _credentialRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new AuthServerException(
                Constants.ErrorCodes.InvalidRequest,
                "No pending TOTP setup found for this user. Call /mfa/setup first.",
                404);
    }
}
