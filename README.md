# ExpressionParser

A lightweight, high-performance string expression parser for .NET 10 that translates dynamic filter strings into LINQ Expression Trees (`Expression<Func<T, bool>>`) for Entity Framework Core and `IQueryable<T>`.

Built on Dijkstra's **Shunting-Yard algorithm** and compile-time **Regex Source Generators** for minimal overhead and zero third-party runtime dependencies.

---

## Features

- **Direct `IQueryable<T>` Integration**: Fluent `.Where("...")` extension method.
- **SQL Translation Friendly**: Generates native LINQ trees compatible with EF Core SQL translation.
- **Rich Operator Support**:
  - Logical: `and`, `or`, `not` (with precedence and parentheses `(...)`)
  - Comparison: `eq`, `neq`, `lt`, `leq`, `gt`, `geq`
  - String: `Contains`, `StartsWith`, `EndsWith`
- **Property-to-Property Comparisons**: Compare two entity fields (e.g., `EndDate gt StartDate`).
- **Quote Escaping**: Single quotes inside string literals via SQL-standard `''` (e.g., `'O''Reilly'`).
- **Null Safety**: Built-in support for `eq null` and `neq null` on nullable types.
- **Comprehensive Type Support**:
  - Integers: `byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`
  - Floating & Decimal: `float`, `double`, `decimal`
  - Date & Time: `DateTime` (UTC), `DateOnly`, `TimeOnly`
  - Text & Logical: `string`, `char`, `bool`
  - All corresponding `Nullable<T>` variants.

---

## Building and Testing

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) (required to run integration tests with Testcontainers)

### Build
```bash
dotnet build
```

### Run Tests
Tests run against a real PostgreSQL 16 instance via Testcontainers:
```bash
dotnet test
```

### Run Benchmarks
```bash
dotnet run -c Release --project ExpressionParser.Benchmark
```

---

## Quick Start

Import the namespace and use `.Where(string)` on any `IQueryable<T>`:

```csharp
using ExpressionParser;

// Filter users with a dynamic query
var activeAdults = await dbContext.Users
    .Where("Age geq 18 and Status eq 'Active'")
    .ToListAsync();
```

---

## Examples

### 1. Logical Precedence and Parentheses
```csharp
var users = await dbContext.Users
    .Where("(Age geq 18 and Age leq 60) or Status eq 'Staff'")
    .ToListAsync();
```

### 2. Direct Boolean Flags and `not`
```csharp
// Evaluates boolean properties directly without redundant 'eq true'
var activeUsers = await dbContext.Users
    .Where("IsActive and not IsBanned")
    .ToListAsync();
```

### 3. String Operations and Escaped Quotes
```csharp
// Double single-quotes '' unescape to a single quote '
var developers = await dbContext.Users
    .Where("LastName eq 'O''Reilly' and Email EndsWith '@company.com' and Bio Contains 'C#'")
    .ToListAsync();
```

### 4. Property-to-Property Comparison
```csharp
// Compare columns against each other
var overdueTasks = await dbContext.Tasks
    .Where("CompletedDate gt DueDate")
    .ToListAsync();
```

### 5. Date and Time Filtering (UTC)
```csharp
var recentRegistrations = await dbContext.Users
    .Where("CreatedAt geq '2025-01-01T00:00:00' and BirthDate leq '2005-01-01'")
    .ToListAsync();
```

### 6. Null Checks
```csharp
var completedProfiles = await dbContext.Users
    .Where("PhoneNumber neq null and DeletedAt eq null")
    .ToListAsync();
```

---

## Syntax Reference

| Operator | Description | Example |
| :--- | :--- | :--- |
| `eq` | Equal | `Status eq 'Active'` |
| `neq` | Not equal | `Role neq 'Guest'` |
| `lt` | Less than | `Price lt 100` |
| `leq` | Less than or equal | `Age leq 18` |
| `gt` | Greater than | `Score gt 50` |
| `geq` | Greater than or equal | `Salary geq 50000` |
| `and` | Logical AND | `A eq 1 and B eq 2` |
| `or` | Logical OR | `Role eq 'Admin' or Role eq 'User'` |
| `not` | Logical NOT | `not IsDeleted` / `not (A eq B)` |
| `Contains` | Substring match | `Name Contains 'smith'` |
| `StartsWith`| Prefix match | `Code StartsWith 'INV-'` |
| `EndsWith` | Suffix match | `Email EndsWith '.org'` |

---

## License

MIT
