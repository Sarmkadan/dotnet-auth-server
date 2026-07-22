# TotpServiceTests_Rfc6238Compliant

## Overview
`TotpServiceTests_Rfc6238Compliant` is a test class that verifies the implementation of the TOTP algorithm and related helpers in the `dotnet-auth-server` project conforms to the specifications outlined in RFC 6238. The class contains a series of unit tests that validate HMAC‑SHA1 usage, code length and format, Base32 encoding/decoding, provisioning URI generation, and tolerance handling. Each test is self‑contained and can be executed with any standard .NET test runner (e.g., xUnit, NUnit, MSTest).

## API

### `public TotpServiceTests_Rfc6238Compliant()`
- **Purpose**: Parameter‑less constructor that creates an instance of the test class. No state is initialized; the class relies solely on static data and method‑local variables.
- **Parameters**: None.
- **Return value**: An instance of `TotpServiceTests_Rfc6238Compliant`.
- **Exceptions**: None thrown under normal circumstances.

### `public void TotpAlgorithm_UsesHmacSha1_AsRequiredByRfc6238()`
- **Purpose**: Asserts that the TOTP implementation uses HMAC‑SHA1 as the underlying hash function, as mandated by RFC 6238.
- **Parameters**: None.
- **Return value**: `void`.
- **Exceptions**: Throws an assertion failure (e.g., `Xunit.AssertException`) if the algorithm does not employ HMAC‑SHA1.

### `public void TotpAlgorithm_ProducesExactlySixDigits()`
- **Purpose**: Verifies that the generated TOTP code is always a six‑digit numeric string.
- **Parameters**: None.
- **Return value**: `void`.
- **Exceptions**: Throws an assertion failure if the code length differs from six or contains non‑digit characters.

### `public void TotpAlgorithm_ValidatesOnlyNumericCodes()`
- **Purpose**: Ensures that validation logic rejects any TOTP candidate containing non‑numeric characters.
- **Parameters**: None.
- **Return value**: `void`.
- **Exceptions**: Throws an assertion failure if a non‑numeric string is incorrectly accepted as valid.

### `public void TotpAlgorithm_HandlesEmptyOrNullCodes()`
- **Purpose**: Confirms that passing `null`, empty, or whitespace‑only strings to the verification method results in a rejection rather than an exception.
- **Parameters**: None.
- **Return value**: `void`.
- **Exceptions**: Throws an assertion failure if the method throws an exception or incorrectly validates an empty/null input.

### `public void TotpAlgorithm_Rfc6238_TestSecret_ProducesValidFormat()`
- **Purpose**: Uses the test secret defined in RFC 6238 Appendix A and checks that the produced TOTP values match the expected sequence at specific time steps.
- **Parameters**: None.
- **Return value**: `void`.
- **Exceptions**: Throws an assertion failure if any of the expected values do not match the algorithm’s output.

### `public void VerifyTotpCode_WindowTolerance_RespectsWindowParameter()`
- **Purpose**: Validates that the `VerifyTotpCode` method honors the configurable window size, accepting codes that fall within the allowed offset and rejecting those outside it.
- **Parameters**: None.
- **Return value**: `void`.
- **Exceptions**: Throws an assertion failure if the method’s behavior deviates from the specified window tolerance.

### `public void VerifyTotpCode_InvalidCodes_RejectedRegardlessOfWindowSize()`
- **Purpose**: Ensures that clearly invalid TOTP codes (e.g., wrong length, non‑numeric) are rejected even when a large window is supplied.
- **Parameters**: None.
- **Return value**: `void`.
- **Exceptions**: Throws an assertion failure if an invalid code is incorrectly accepted under any window setting.

### `public void Base32_EncodingDecoding_IsCorrect()`
- **Purpose**: Checks that the Base32 encode/decode pair is lossless for a variety of input byte arrays.
- **Parameters**: None.
- **Return value**: `void`.
- **Exceptions**: Throws an assertion failure if decoding does not yield the original input.

### `public void Base32_HandlesPaddingCorrectly()`
- **Purpose**: Verifies that the Base32 implementation correctly adds and strips padding characters (`=`) according to RFC 4648.
- **Parameters**: None.
- **Return value**: `void`.
- **Exceptions**: Throws an assertion failure if padding is mishandled.

### `public void Base32_IgnoresInvalidCharacters()`
- **Purpose**: Confirms that decoding ignores characters outside the Base32 alphabet (e.g., spaces, hyphens) and still produces the correct result.
- **Parameters**: None.
- **Return value**: `void`.
- **Exceptions**: Throws an assertion failure if invalid characters cause a decoding error or incorrect output.

### `public void BuildProvisioningUri_CreatesCorrectRfc6238Format()`
- **Purpose**: Asserts that the generated provisioning URI conforms to the `otpauth://totp/` format defined in RFC 6238, including proper encoding of parameters such as secret, issuer, and algorithm.
- **Parameters**: None.
- **Return value**: `void`.
- **Exceptions**: Throws an assertion failure if the URI deviates from the expected pattern.

### `public void TotpAlgorithm_DifferentSecrets_ProduceDifferentResults()`
- **Purpose**: Ensures that two distinct Base32 secrets yield different TOTP values for the same time step, confirming proper key separation.
- **Parameters**: None.
- **Return value**: `void`.
- **Exceptions**: Throws an assertion failure if the outputs are identical.

### `public void TotpAlgorithm_SameSecret_IsDeterministic()`
- **Purpose**: Validates that repeatedly invoking the TOTP generation with the same secret and time step returns the identical code each time.
- **Parameters**: None.
- **Return value**: `void`.
- **Exceptions**: Throws an assertion failure if non‑deterministic behavior is observed.

## Usage
The class is intended to be discovered and executed by a test runner. Below are two typical ways to run these tests in the `dotnet-auth-server` solution.

### Example 1: Using the .NET CLI
```bash
# From the repository root
dotnet test --filter FullyQualifiedName~TotpServiceTests_Rfc6238Compliant
```
This command builds the test project and executes only the tests contained in `TotpServiceTests_Rfc6238Compliant`.

### Example 2: Invoking via an IDE test explorer
1. Open the solution in Visual Studio or Rider.  
2. Locate `TotpServiceTests_Rfc6238Compliant` under the test project in the Test Explorer window.  
3. Right‑click the class and select **Run** (or **Debug**) to execute all its test methods.

## Notes
- The test class contains no mutable state; each method operates on local variables or static constants. Consequently, the class is thread‑safe and can be run in parallel with other test classes without risk of interference.  
- All tests rely on the public API of the TOTP service implementation; they do not access private members directly.  
- Assertion failures are reported by the underlying unit‑testing framework (e.g., xUnit’s `Assert` methods). No custom exceptions are thrown by the test methods themselves.  
- Edge cases such as null/empty inputs, malformed Base32 strings, and extreme window sizes are explicitly covered by the respective test methods to ensure robust compliance with RFC 6238.  
- If the underlying TOTP service is altered to use a different hash function or to modify the code length, the corresponding tests will fail, providing immediate feedback on specification drift.
