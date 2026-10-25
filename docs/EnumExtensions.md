# EnumExtensions

The `EnumExtensions` class is a public static class in the `DotnetAuthServer.Extensions` namespace. It provides four helpers for enum types: one extension method that returns a display string for an enum value, two static parsers that take a `string` and resolve it to an enum member by name or number, and one method that lists the members of an enum type. Only `ToDescriptionString` is an extension method (it has `this`); the other three are called as `EnumExtensions.<Method><T>(...)`.

The class has no private members and no state. All methods are stateless and safe to call from multiple threads.

## API

| Method | Kind | Returns | Purpose |
|---|---|---|---|
| [`ToDescriptionString`](#todescriptionstring) | extension on `T` | `string` | Display text: `DescriptionAttribute` value, or the name, or the number |
| [`FromString`](#fromstring) | static | `T` | Parse a name or number; throws on failure |
| [`GetValues`](#getvalues) | static | `IEnumerable<T>` | All named constants of `T` |
| [`IsValidValue`](#isvalidvalue) | static | `bool` | Whether a string parses to a value of `T` |

### ToDescriptionString

```csharp
public static string ToDescriptionString<T>(this T @enum) where T : Enum
```

Gets the text to show for an enum value. The method takes `ToString()` of the value and looks up a public static field with that name on the value's runtime type. If the field has a `System.ComponentModel.DescriptionAttribute`, its `Description` is returned. Otherwise the `ToString()` text is returned.

Because the lookup is by the text of `ToString()`, a value that is not a named constant (for example `(Status)99`) or a flags combination (for example `Read | Write`) has no matching field. In those cases the `ToString()` text is returned: `"99"` or `"Read, Write"`. The method does not throw for these values.

**Parameters**

- `enum`: The enum value to convert.

**Returns**

The `DescriptionAttribute.Description` of the matching field, or `ToString()` of the value when there is no description.

**Exceptions**

- `ArgumentNullException`: `enum` is `null`. The check is effectively unreachable for a non-nullable enum argument, because the value is boxed and cannot be `null`.

### FromString

```csharp
public static T FromString<T>(string value) where T : struct, Enum
```

Parses a string into a member of `T`. It calls `Enum.TryParse<T>` with `ignoreCase: true`. Parsing accepts member names, numeric values, and comma-separated flag names. Leading and trailing whitespace is ignored. `DescriptionAttribute` values are **not** used for matching.

Because `Enum.TryParse` accepts any number, a numeric string that is not a named constant (for example `"99"`) parses successfully and returns that value cast to `T`.

**Parameters**

- `value`: The name, number, or flag-name list to parse.

**Returns**

The parsed value of type `T`.

**Exceptions**

- `ArgumentNullException`: `value` is `null`. The exception's parameter name is `value`.
- `ArgumentException`: `value` is empty, whitespace, or cannot be parsed as `T`. The message is `Value '<value>' is not a valid value for enum type <TypeName>.`

### GetValues

```csharp
public static IEnumerable<T> GetValues<T>() where T : Enum
```

Gets the values of `T` by calling `Enum.GetValues(typeof(T))` and casting each element to `T`. The result has one entry per named constant, in ascending order of the underlying value. Aliases (two names with the same value) therefore appear once for each name. Each entry is rendered with the canonical name of its value, so an alias prints as the first name declared for that value.

**Parameters**

None. `T` is the only type argument, and it is supplied explicitly, for example `GetValues<Status>()`.

**Returns**

An `IEnumerable<T>` containing every named constant of `T`.

**Exceptions**

None. The `where T : Enum` constraint prevents non-enum types at compile time.

### IsValidValue

```csharp
public static bool IsValidValue<T>(string value) where T : struct, Enum
```

Checks whether a string parses to a value of `T`. It uses the same rules as [`FromString`](#fromstring): case-insensitive, whitespace-trimmed, names and numbers accepted, and `DescriptionAttribute` values ignored. The check does not require the parsed number to be a named constant.

**Parameters**

- `value`: The string to check.

**Returns**

`true` if the string parses to a value of `T`; otherwise `false`. Empty and whitespace-only strings return `false`.

**Exceptions**

- `ArgumentNullException`: `value` is `null`.

## Usage

### Example 1: Round-tripping names

`ToDescriptionString` is for display. `FromString` parses names and numbers, so a description only round-trips through `FromString` if it equals the field name.

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

OrderStatus status = OrderStatus.Shipped;
string label = status.ToDescriptionString();          // "Shipped"

OrderStatus parsed = EnumExtensions.FromString<OrderStatus>("shipped");
Console.WriteLine(parsed == OrderStatus.Shipped);     // True

// Descriptions are not parsed:
EnumExtensions.IsValidValue<OrderStatus>("Pending Approval"); // False
```

### Example 2: Populating a dropdown and validating input

```csharp
var options = EnumExtensions.GetValues<OrderStatus>()
    .Select(v => new { Value = v.ToString(), Label = v.ToDescriptionString() })
    .ToList();

string raw = "99";
if (EnumExtensions.IsValidValue<OrderStatus>(raw))
{
    // Returns true for "99" even though no member has that value.
    // Check the parsed value against the defined members if that matters.
}
```

## Notes

- **Descriptions are not parsed.** `ToDescriptionString` reads `DescriptionAttribute`. `FromString` and `IsValidValue` match only member names and numbers, so a value must be parsed from its name (or number) to round-trip.
- **Case and whitespace.** `FromString` and `IsValidValue` ignore case and leading or trailing whitespace. `"shipped"`, `" Shipped "` and `"SHIPPED"` all parse to `Shipped`.
- **Undefined numeric values.** Numeric strings are accepted even when they are not named constants. `IsValidValue<OrderStatus>("99")` returns `true`, and `FromString<OrderStatus>("99")` returns `(OrderStatus)99`. Validate the parsed value against `GetValues<T>()` when the input is untrusted.
- **Flags.** `FromString` and `IsValidValue` accept comma-separated flag names such as `"Read, Write"`. `ToDescriptionString` does not look up descriptions for combined values; it returns the `ToString()` text.
- **Aliases.** `GetValues` returns an entry for each name, so aliased values appear more than once. `FromString` returns the same value for any alias.
- **Exceptions differ.** `FromString` throws on failure. `IsValidValue` returns `false` for unparseable input and only throws for `null`. `ToDescriptionString` does not throw for undefined values.
