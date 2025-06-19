# WebAuthnPublicKeyParam

The `WebAuthnPublicKeyParam` type serves as a low-level utility record within the `dotnet-auth-server` project, designed to facilitate the parsing and inspection of CBOR (Concise Binary Object Representation) data structures commonly used in WebAuthn credential exchanges. As a sealed record, it provides immutable access to parsed values such as integers, byte arrays, and text strings, while offering static helper methods to peek at data types, retrieve specific values, and navigate through the binary stream without deserializing the entire payload.

## API

### Type Definition
**`public sealed record WebAuthnPublicKeyParam`**
Defines the immutable data structure representing a single public key parameter or a CBOR map entry. Being a record, it supports value-based equality and immutability by default.

### Instance Members
**`public string Value`**
Retrieves the string representation of the current parameter if the underlying data type is text. If the parameter does not hold a text value, accessing this may return null or an empty string depending on the internal state initialization.

### Static Members
**`public static bool PeekIsInt`**
Inspects the current position in the data stream to determine if the next item is an integer. Returns `true` if the next token is an integer; otherwise, `false`. This operation does not advance the stream pointer.

**`public static bool PeekIsBytes`**
Inspects the current position in the data stream to determine if the next item is a byte array. Returns `true` if the next token is a byte sequence; otherwise, `false`. This operation does not advance the stream pointer.

**`public static int MapSize`**
Returns the size of the current CBOR map being processed. This indicates the number of key-value pairs contained within the current scope.

**`public static string Text`**
Extracts and returns the current item as a string. Throws an exception if the current item in the stream is not a valid text string.

**`public static byte[] Bytes`**
Extracts and returns the current item as a byte array. Throws an exception if the current item in the stream is not a valid byte sequence.

**`public static long Int`**
Extracts and returns the current item as a 64-bit integer. Throws an exception if the current item in the stream is not a valid integer.

**`public static void Skip`**
Advances the stream pointer past the current item, effectively ignoring its value. This is useful for traversing CBOR structures when specific fields are not required.

*Note: The following types and methods listed in the project context are related to the WebAuthn service workflow but are distinct members of the `WebAuthnService` class or separate option records, not direct members of `WebAuthnPublicKeyParam`: `WebAuthnRegistrationOptions`, `WebAuthnAuthenticationOptions`, `WebAuthnService`, `GenerateRegistrationOptionsAsync`, `CompleteRegistrationAsync`, `GenerateAuthenticationOptionsAsync`, and `CompleteAuthenticationAsync`.*

## Usage

### Example 1: Inspecting CBOR Parameter Types
This example demonstrates how to use the static peek methods to safely determine the data type of a public key parameter before attempting to extract its value, preventing format exceptions.

```csharp
using DotNetAuthServer.WebAuthn;

// Assume a CBOR stream has been initialized internally by the service
if (WebAuthnPublicKeyParam.PeekIsInt)
{
    long algorithmId = WebAuthnPublicKeyParam.Int;
    Console.WriteLine($"Algorithm ID: {algorithmId}");
}
else if (WebAuthnPublicKeyParam.PeekIsBytes)
{
    byte[] keyData = WebAuthnPublicKeyParam.Bytes;
    Console.WriteLine($"Key data length: {keyData.Length} bytes");
}
else
{
    // Handle unexpected types or skip
    WebAuthnPublicKeyParam.Skip();
}
```

### Example 2: Navigating a Public Key Map
This example illustrates reading the size of a parameter map and extracting specific text and binary fields, utilizing the `Skip` method to ignore irrelevant parameters.

```csharp
using DotNetAuthServer.WebAuthn;
using System;

public void ProcessPublicKeyParameters()
{
    int totalParams = WebAuthnPublicKeyParam.MapSize;
    Console.WriteLine($"Processing {totalParams} parameters.");

    for (int i = 0; i < totalParams; i++)
    {
        // Attempt to read a text label
        try 
        {
            string label = WebAuthnPublicKeyParam.Text;
            if (label == "name")
            {
                Console.WriteLine($"Found name parameter");
                continue;
            }
        }
        catch (InvalidOperationException)
        {
            // Not a text item, skip it
            WebAuthnPublicKeyParam.Skip();
            continue;
        }

        // If not handled, skip the value associated with the key
        WebAuthnPublicKeyParam.Skip();
    }
}
```

## Notes

### Thread Safety
The static members of `WebAuthnPublicKeyParam` (e.g., `PeekIsInt`, `Int`, `Skip`) imply an internal stateful cursor or stream context. Consequently, these static methods are **not thread-safe**. Concurrent calls from multiple threads sharing the same underlying data context will result in race conditions, corrupted stream positions, or inconsistent read results. Instances of the record itself (`WebAuthnPublicKeyParam`) are immutable and thread-safe once constructed, but the static parsing helpers must be invoked within a single-threaded context or protected by external synchronization.

### Edge Cases
- **Type Mismatch**: Calling `Text`, `Bytes`, or `Int` when the current stream position does not match the expected type will result in a runtime exception (likely `InvalidOperationException` or `FormatException`). Always verify types using `PeekIsInt` or `PeekIsBytes` prior to extraction.
- **Stream Exhaustion**: Invoking `Skip` or extraction methods beyond the bounds of the defined `MapSize` or available data may lead to undefined behavior or exceptions. Ensure loop counters respect the `MapSize` property.
- **Empty Maps**: If `MapSize` returns 0, no iteration should be performed, and direct calls to extraction methods will fail immediately.
