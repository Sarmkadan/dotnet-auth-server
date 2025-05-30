# EnumExtensions

Provides utility methods for working with enumerations, including conversion to and from human-readable description strings, retrieval of all defined values, and validation of enum members. The methods rely on the presence of a `DescriptionAttribute` on enum fields to supply the string representations used by `ToDescriptionString` and `FromString`.

## API

### ToDescriptionString\<T\>
```csharp
public static string ToDescriptionString<T>(this T value) where T : Enum
```
Returns the description string associated with the given enum value. The description is obtained from the `DescriptionAttribute` applied to the enum field. If no such attribute is present, the method falls back to the enum field's name.

**Parameters:**
- `value` — The enum value whose description is to be retrieved.

**Returns:**
- The content of the `DescriptionAttribute` on the enum field, or the field's name if the attribute is absent.

**Throws:**
- `ArgumentNullException` — when `value` is `null`.
- `InvalidOperationException` — when the supplied value is not a defined enum member (e.g., an out-of-range integer cast to the enum type).

---

### FromString\<T\>
```csharp
public static T? FromString<T>(this string description) where T : struct, Enum
```
Parses a description string and returns the corresponding enum value of type `T`. Matching is performed against the `DescriptionAttribute` values of all defined enum fields. The comparison is case-sensitive.

**Parameters:**
- `description` — The description string to match against enum field descriptions.

**Returns:**
- The enum value whose `DescriptionAttribute` matches the input string, or `null` if no match is found or if the input is `null` or whitespace.

**Throws:**
- This method does not throw exceptions; it returns `null` for any input that cannot be resolved.

---

### GetValues\<T\>
```csharp
public static IEnumerable<T> GetValues<T>() where T : Enum
```
Enumerates all defined values of the enum type `T`.

**Parameters:**
- None (generic type parameter only).

**Returns:**
- An `IEnumerable<T>` containing every named constant of the enum, in declaration order.

**Throws:**
- No exceptions are thrown under normal circumstances. The method relies on `Enum.GetValues`, which may throw `ArgumentException` if `T` is not an enum type at runtime, but the generic constraint prevents this at compile time.

---

### IsValidValue\<T\>
```csharp
public static bool IsValidValue<T>(this T value) where T : Enum
```
Determines whether the provided value is a defined member of the enum type `T`. This validates against named constants only; numeric combinations of flags are considered invalid unless they correspond exactly to a named field.

**Parameters:**
- `value` — The enum value to validate.

**Returns:**
- `true` if the value is one of the named constants of `T`; otherwise `false`.

**Throws:**
- `ArgumentNullException` — when `value` is `null`.

## Usage

### Example 1: Converting an enum to a user-facing string and back
```csharp
public enum OrderStatus
{
    [Description("Pending Approval")]
    Pending,

    [Description("Shipped")]
    Shipped,

    [Description("Delivered")]
    Delivered
}

// Convert enum to display string
OrderStatus status = OrderStatus.Shipped;
string displayText = status.ToDescriptionString(); // "Shipped"

// Parse display string back to enum
OrderStatus? parsed = displayText.FromString<OrderStatus>();
Console.WriteLine(parsed == OrderStatus.Shipped); // True
```

### Example 2: Populating a dropdown and validating input
```csharp
// Populate dropdown with all enum values using their descriptions
var options = EnumExtensions.GetValues<OrderStatus>()
    .Select(v => new { Value = v, Label = v.ToDescriptionString() })
    .ToList();

// Validate a raw integer received from an API
int rawValue = 2;
OrderStatus candidate = (OrderStatus)rawValue;
if (candidate.IsValidValue())
{
    Console.WriteLine($"Valid status: {candidate.ToDescriptionString()}");
}
else
{
    Console.WriteLine("Invalid status value.");
}
```

## Notes

- **DescriptionAttribute dependency:** `ToDescriptionString` and `FromString` depend entirely on `DescriptionAttribute` annotations. Enum fields without this attribute expose their raw field name via `ToDescriptionString` and are matched by field name in `FromString`.
- **Case sensitivity:** `FromString` performs an exact, case-sensitive comparison. Input strings that differ in casing from the attribute value will not match.
- **Null and whitespace handling:** `FromString` returns `null` for `null`, empty, or whitespace-only input strings without throwing.
- **Undefined enum values:** `ToDescriptionString` throws `InvalidOperationException` when called on a value that is not a named constant (e.g., `(OrderStatus)99`). Callers should guard with `IsValidValue` when the source of the value is untrusted.
- **Flags enums:** `IsValidValue` checks for exact match against named constants. Combined flag values (e.g., `Read | Write`) return `false` unless the combination itself is a named field. `GetValues` returns only the individually declared fields, not all possible combinations.
- **Thread safety:** All methods are stateless and operate on immutable reflection data. They are safe to call concurrently from multiple threads without external synchronization.
