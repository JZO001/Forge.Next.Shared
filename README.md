# Forge.Next.Shared

**Forge Patterns and Practices** — a small, dependency-light collection of helper types and extension
methods that smooth over common .NET tasks: fluent boolean branching, deep sequence comparison,
exception‑safe execution that returns [`ErrorOr`](https://github.com/amantinband/error-or) results,
object hashing, exception‑to‑error conversion, file‑system probing and lightweight UI reflection.

---

## Table of contents

- [Installation](#installation)
- [Requirements](#requirements)
- [Quick start](#quick-start)
- [API reference](#api-reference)
  - [BoolExtensions](#boolextensions)
  - [IEnumerableExtensions](#ienumerableextensions)
  - [ISetExtensions](#isetextensions)
  - [ObjectExtensions](#objectextensions)
  - [ExceptionErrorOrExtensions](#exceptionerrororextensions)
  - [PathExtensions](#pathextensions)
  - [UIExtensions](#uiextensions)
  - [Event](#event)
  - [LogUtils](#logutils)
  - [Consts](#consts)
  - [InitializationException](#initializationexception)
- [License](#license)

---

## Installation

```bash
dotnet add package Forge.Next.Shared
```

Or via a `PackageReference`:

```xml
<ItemGroup>
  <PackageReference Include="Forge.Next.Shared" Version="3.0.0" />
</ItemGroup>
```

## Requirements

| | |
|---|---|
| **Target frameworks** | `net48`, `net8.0`, `net9.0`, `net10.0` |
| **Dependencies** | [`ErrorOr`](https://www.nuget.org/packages/ErrorOr) (`>= 2.1.1`) — used by the `Protect*` and `ToErrorOrError*` APIs. On `net48`, [`Microsoft.Extensions.Logging.Abstractions`](https://www.nuget.org/packages/Microsoft.Extensions.Logging.Abstractions) and [`System.Text.Json`](https://www.nuget.org/packages/System.Text.Json) are also pulled in. |

> **Note about SHA‑3.** The `GetSHA3_*` helpers are compiled **only for .NET (`NETCOREAPP`) target
> frameworks** — they are excluded on non‑.NET‑Core targets via `#if NETCOREAPP`. Of this package's targets,
> `net8.0`/`net9.0`/`net10.0` define `NETCOREAPP` (so the helpers are present there), but **`net48` does not**,
> so the SHA‑3 helpers are **absent from the `net48` build**. Additionally, at **runtime** SHA‑3 depends on the
> operating system's support: where it is unavailable the underlying `SHA3_*.Create()` call throws
> `PlatformNotSupportedException`. Use `SHA3_256.IsSupported` (and friends) to probe before calling.

## Quick start

Most types live in the root `Forge.Next.Shared` namespace; the helper groups that live in sub‑folders use
matching sub‑namespaces — `PathExtensions` in **`Forge.Next.Shared.IO`**, `UIExtensions` in
**`Forge.Next.Shared.UI`** and `LogUtils` in **`Forge.Next.Shared.Logging`**:

```csharp
using Forge.Next.Shared;          // BoolExtensions, IEnumerableExtensions, ISetExtensions, ObjectExtensions, Event, ...
using Forge.Next.Shared.IO;       // PathExtensions
using Forge.Next.Shared.UI;       // UIExtensions
using Forge.Next.Shared.Logging;  // LogUtils

// Fluent boolean branching
string label = (user.Age >= 18).IfTrue(() => "adult", () => "minor");

// Exception-safe execution returning an ErrorOr result
ErrorOr<int> parsed = "42".Protect(s => int.Parse(s));

// Deep, recursive sequence equality
bool same = new[] { 1, 2, 3 }.IsDeepEqual(new[] { 1, 2, 3 }); // true
```

---

# API reference

## BoolExtensions

Fluent extension methods on `bool` that let you express `if`/`else` logic as a single expression.

There are two families:

- **`IfTrue*`** — the *primary* callback fires when the condition is `true`.
- **`IfFalse*`** — the *primary* callback fires when the condition is `false`.

Within each family there are value‑returning methods (`IfTrue` / `IfFalse`), side‑effect methods
(`IfTrueDo` / `IfFalseDo`, which return the **original condition** so you can keep chaining), and `async`
variants of both. Every method has an overload that forwards an input `value` to the selected callback,
which keeps the lambdas closure‑free.

> For value‑returning methods, when the condition does not select a callback and no optional callback was
> supplied, the method returns `default(TNextValue)`.

### `IfTrue<TNextValue>(Func<TNextValue> onTrue, Func<TNextValue>? onFalse = null)`

Returns `onTrue()` when the condition is `true`; otherwise returns `onFalse()` if supplied, or `default`.

```csharp
bool isAdmin = true;

string role = isAdmin.IfTrue(() => "Administrator", () => "User");
// role == "Administrator"

// Without an onFalse callback, a false condition yields default(string) == null:
string? maybe = false.IfTrue(() => "yes");
// maybe == null
```

### `IfTrue<TValue, TNextValue>(TValue value, Func<TValue, TNextValue> onTrue, Func<TValue, TNextValue>? onFalse = null)`

Same as above, but forwards `value` to the chosen callback.

```csharp
int discountPercent = isMember.IfTrue(
    value: order,
    onTrue: o => o.Total > 100 ? 15 : 10,
    onFalse: o => 0);
```

### `IfTrueAsync<TNextValue>(Func<Task<TNextValue>> onTrue, Func<Task<TNextValue>>? onFalse = null)`

Asynchronous counterpart of `IfTrue`.

```csharp
User user = await isCacheHit.IfTrueAsync(
    onTrue: () => cache.GetAsync(id),
    onFalse: () => repository.LoadAsync(id));
```

### `IfTrueAsync<TValue, TNextValue>(TValue value, Func<TValue, Task<TNextValue>> onTrue, Func<TValue, Task<TNextValue>>? onFalse = null)`

```csharp
Invoice invoice = await shouldRecalculate.IfTrueAsync(
    value: invoiceId,
    onTrue: id => billing.RecalculateAsync(id),
    onFalse: id => billing.LoadAsync(id));
```

### `IfTrueDo(Action onTrue, Action? onFalse = null) : bool`

Runs a side effect and returns the **original condition** (handy for chaining or logging).

```csharp
bool wasValid = isValid.IfTrueDo(
    onTrue: () => Console.WriteLine("Accepted"),
    onFalse: () => Console.WriteLine("Rejected"));
// wasValid == isValid
```

### `IfTrueDo<TValue>(TValue value, Action<TValue> onTrue, Action<TValue>? onFalse = null) : bool`

```csharp
hasChanges.IfTrueDo(document, onTrue: d => d.Save());
```

### `IfTrueDoAsync(Func<Task> onTrue, Func<Task>? onFalse = null) : Task<bool>`

```csharp
bool sent = await hasRecipients.IfTrueDoAsync(
    onTrue: () => mailer.SendAsync(message));
```

### `IfTrueDoAsync<TValue>(TValue value, Func<TValue, Task> onTrue, Func<TValue, Task>? onFalse = null) : Task<bool>`

```csharp
await isDirty.IfTrueDoAsync(entity, onTrue: e => db.SaveAsync(e));
```

### `IfFalse<TNextValue>(Func<TNextValue> onFalse, Func<TNextValue>? onTrue = null)`

The mirror image of `IfTrue`: the **first** callback fires when the condition is `false`.

```csharp
string status = isOnline.IfFalse(
    onFalse: () => "offline",
    onTrue: () => "online");
```

### `IfFalse<TValue, TNextValue>(TValue value, Func<TValue, TNextValue> onFalse, Func<TValue, TNextValue>? onTrue = null)`

```csharp
decimal fee = isPremium.IfFalse(
    value: account,
    onFalse: a => a.StandardFee,
    onTrue: a => 0m);
```

### `IfFalseAsync<TNextValue>(Func<Task<TNextValue>> onFalse, Func<Task<TNextValue>>? onTrue = null)`

```csharp
Config config = await isOverridden.IfFalseAsync(
    onFalse: () => loader.LoadDefaultsAsync());
```

### `IfFalseAsync<TValue, TNextValue>(TValue value, Func<TValue, Task<TNextValue>> onFalse, Func<TValue, Task<TNextValue>>? onTrue = null)`

```csharp
Profile profile = await isCached.IfFalseAsync(
    value: userId,
    onFalse: id => api.FetchProfileAsync(id),
    onTrue: id => cache.GetProfileAsync(id));
```

### `IfFalseDo(Action onFalse, Action? onTrue = null) : bool`

```csharp
isHealthy.IfFalseDo(
    onFalse: () => alerts.Raise("Service unhealthy"));
```

### `IfFalseDo<TValue>(TValue value, Action<TValue> onFalse, Action<TValue>? onTrue = null) : bool`

```csharp
exists.IfFalseDo(path, onFalse: p => Directory.CreateDirectory(p));
```

### `IfFalseDoAsync(Func<Task> onFalse, Func<Task>? onTrue = null) : Task<bool>`

```csharp
await isAuthenticated.IfFalseDoAsync(
    onFalse: () => authenticator.ChallengeAsync());
```

### `IfFalseDoAsync<TValue>(TValue value, Func<TValue, Task> onFalse, Func<TValue, Task>? onTrue = null) : Task<bool>`

```csharp
await isCached.IfFalseDoAsync(key, onFalse: k => cache.WarmUpAsync(k));
```

---

## IEnumerableExtensions

Helpers for `IEnumerable` and `IEnumerable<T>`.

### `ForEach<T>(this IEnumerable<T> items, Action<T> action)`

Executes `action` for each element. A `null` source is a safe no‑op. The source is materialized
(`ToList()`) before iteration, so the action may safely mutate the underlying collection.

```csharp
new[] { "a", "b", "c" }.ForEach(Console.WriteLine);

// null is ignored — no exception:
IEnumerable<int>? nothing = null;
nothing.ForEach(x => Console.WriteLine(x)); // does nothing
```

### `IsDeepEqual(this IEnumerable? first, IEnumerable? second) : bool`

Non‑generic, **deep** order‑sensitive comparison. Two sequences are equal when:

- both are `null`, or are the same reference; **and**
- they share the **same runtime type**; **and**
- they have the same length and every element is deeply equal.

Nested `IEnumerable` elements are compared recursively. **Strings are treated as scalar values** (compared
by value, not character by character). The fast path short‑circuits on differing `ICollection.Count`.

```csharp
using System.Collections;

IEnumerable a = new object[] { 1, new[] { 2, 3 }, "x" };
IEnumerable b = new object[] { 1, new[] { 2, 3 }, "x" };

a.IsDeepEqual(b); // true — nested arrays compared deeply

// Different runtime types are never equal, even with identical contents:
((IEnumerable)new List<int> { 1, 2 }).IsDeepEqual((IEnumerable)new[] { 1, 2 }); // false
```

### `IsDeepEqual<T>(this IEnumerable<T>? first, IEnumerable<T>? second) : bool`

Generic counterpart. Elements are compared with `EqualityComparer<T>.Default`, while nested `IEnumerable`
elements still recurse. Unlike the non‑generic overload, **it does not require the same container type**, so
it behaves like a deep `SequenceEqual`.

```csharp
new List<int> { 1, 2, 3 }.IsDeepEqual(new[] { 1, 2, 3 });          // true (container-agnostic)
new[] { 1, 2, 3 }.IsDeepEqual(new[] { 1, 9, 3 });                  // false (element differs)

// Nested sequences compare deeply:
var x = new List<int[]> { new[] { 1, 2 }, new[] { 3, 4 } };
var y = new List<int[]> { new[] { 1, 2 }, new[] { 3, 4 } };
x.IsDeepEqual(y);                                                  // true
```

> **Overload tip:** strongly‑typed collections bind to the **generic** overload. Cast both operands to
> `System.Collections.IEnumerable` if you specifically need the same‑runtime‑type semantics of the
> non‑generic overload.

---

## ISetExtensions

Extension methods for `ISet<T>`.

### `AddRange<T>(this ISet<T> set, IEnumerable<T> collection)`

Adds every element of `collection` to `set`. Because it is a set, elements already present are simply
ignored. A `null` `collection` is treated as empty (no‑op). Throws `ArgumentNullException` when `set` is
`null`.

```csharp
var seen = new HashSet<int> { 1, 2 };

seen.AddRange(new[] { 2, 3, 4 });
// seen == { 1, 2, 3, 4 }  (the duplicate 2 was ignored)

seen.AddRange(null);       // no-op, does not throw
```

---

## ObjectExtensions

Exception‑safe execution wrappers that return `ErrorOr` results, plus object hashing helpers based on JSON
serialization.

### `DefaultJsonSerializerOptions { get; set; } : JsonSerializerOptions`

The options used to serialize objects to JSON before hashing. By default fields are included and reference
loops are preserved. You can replace it to customize hashing input.

```csharp
ObjectExtensions.DefaultJsonSerializerOptions.IncludeFields;       // true
ObjectExtensions.DefaultJsonSerializerOptions.ReferenceHandler;    // ReferenceHandler.Preserve
```

### `Protect<T, TResult>(Func<T, ErrorOr<TResult>> func, ErrorType errorType = ErrorType.Unexpected) : ErrorOr<TResult>`

Invokes `func` and returns its result. If `func` throws, the exception is caught and converted into an
`ErrorOr` error whose `Type` is `errorType` and whose `Description` is the exception message (the full
exception chain is captured in the error metadata — see [`ToErrorOrError`](#exceptionerrororextensions)).

```csharp
using ErrorOr;

ErrorOr<int> ok = "42".Protect(s => int.Parse(s));
// ok.Value == 42

ErrorOr<int> failed = "oops".Protect(s => int.Parse(s), ErrorType.Validation);
if (failed.IsError)
{
    Console.WriteLine(failed.FirstError.Type);        // Validation
    Console.WriteLine(failed.FirstError.Description);  // the FormatException message
}
```

### `ProtectAsync<T, TResult>(Func<T, CancellationToken, Task<ErrorOr<TResult>>> func, ErrorType errorType = ErrorType.Unexpected, bool configureAwait = false, CancellationToken cancellationToken = default) : Task<ErrorOr<TResult>>`

Asynchronous version of `Protect`. The `func` receives the supplied `cancellationToken`, which lets the
awaited operation observe cancellation; `configureAwait` is forwarded to the internal `ConfigureAwait` call.

```csharp
ErrorOr<User> result = await userId.ProtectAsync(
    func: (id, ct) => repository.GetAsync(id, ct),   // returns Task<ErrorOr<User>>
    errorType: ErrorType.NotFound,
    cancellationToken: token);
```

### `ProtectDo<T>(Action<T> action, ErrorType errorType = ErrorType.Unexpected) : ErrorOr<T>`

Runs `action` for its side effects and returns the **original object** on success, or an error if the
action throws.

```csharp
ErrorOr<Order> result = order.ProtectDo(o => o.Validate());
// On success: result.Value is the same 'order' instance.
// On failure: result.IsError is true.
```

### `ProtectDoAsync<T>(Func<T, CancellationToken, Task> func, ErrorType errorType = ErrorType.Unexpected, bool configureAwait = false, CancellationToken cancellationToken = default) : Task<ErrorOr<T>>`

Asynchronous version of `ProtectDo`. The `func` receives the supplied `cancellationToken` so the
side-effecting operation can observe cancellation. Returns the **original object** on success.

```csharp
ErrorOr<Document> result = await document.ProtectDoAsync(
    (d, ct) => storage.SaveAsync(d, ct),
    cancellationToken: token);
```

### Hashing helpers

Each helper serializes the object to JSON (using `DefaultJsonSerializerOptions`) and hashes the UTF‑8
bytes. Two flavors are provided per algorithm:

- `GetSHAxxx(this object obj) : byte[]` — the raw digest.
- `GetSHAxxxAsInt(this object obj) : int` — the digest folded into a 32‑bit integer (useful as a hash code).

All helpers throw `ArgumentNullException` when `obj` is `null`. Equal objects (by serialized JSON) produce
equal hashes.

| Method | Output | Digest size | Availability |
|---|---|---|---|
| `GetSHA256` / `GetSHA256AsInt` | `byte[]` / `int` | 32 bytes | All targets |
| `GetSHA384` / `GetSHA384AsInt` | `byte[]` / `int` | 48 bytes | All targets |
| `GetSHA512` / `GetSHA512AsInt` | `byte[]` / `int` | 64 bytes | All targets |
| `GetSHA3_256` / `GetSHA3_256_AsInt` | `byte[]` / `int` | 32 bytes | `NETCOREAPP` only † |
| `GetSHA3_384` / `GetSHA3_384_AsInt` | `byte[]` / `int` | 48 bytes | `NETCOREAPP` only † |
| `GetSHA3_512` / `GetSHA3_512_AsInt` | `byte[]` / `int` | 64 bytes | `NETCOREAPP` only † |

> **† SHA‑3 availability.** The `GetSHA3_*` helpers are compiled only for .NET (`NETCOREAPP`) target
> frameworks — they are wrapped in `#if NETCOREAPP` in the source. All current package targets
> (`net8.0`/`net9.0`/`net10.0`) define `NETCOREAPP`, so they are present in every shipped build; if you
> cross‑compile this source against a non‑.NET‑Core target they are omitted. They additionally require
> runtime OS support — guard calls with `SHA3_256.IsSupported` (see below).

```csharp
var customer = new { Id = 7, Name = "Ada" };

byte[] digest = customer.GetSHA256();        // 32 bytes
int    folded = customer.GetSHA256AsInt();   // deterministic per process

// SHA-3 — probe for platform support first:
using System.Security.Cryptography;
if (SHA3_256.IsSupported)
{
    byte[] sha3 = customer.GetSHA3_256();
}
```

> The `*AsInt` value is produced with `HashCode.Combine`, which uses a per‑process random seed: it is
> stable for the lifetime of the process but varies between runs. Do not persist it.

---

## ExceptionErrorOrExtensions

Convert an `Exception` (and its inner‑exception chain) into `ErrorOr` errors.

### `ToErrorOrError(this Exception ex, ErrorType expectedErrorType = ErrorType.Unexpected) : List<Error>`

Walks `ex` and its `InnerException` chain, producing **one `Error` per exception**, ordered outermost →
innermost. Each error's `Description` is the exception message; its `Metadata` captures the exception's
assembly‑qualified `type`, `message` and `stackTrace`. `expectedErrorType` selects which `Error` factory is
used (`Validation`, `Forbidden`, `Unexpected`, `NotFound`, `Unauthorized`, `Conflict`, `Failure`).

```csharp
using ErrorOr;

try
{
    DoWork();
}
catch (Exception ex)
{
    List<Error> errors = ex.ToErrorOrError(ErrorType.Failure);

    Error first = errors[0];
    Console.WriteLine(first.Type);                       // Failure
    Console.WriteLine(first.Description);                 // ex.Message
    Console.WriteLine(first.Metadata!["type"]);          // assembly-qualified type name
    Console.WriteLine(first.Metadata!["stackTrace"]);    // stack trace (or "")
}
```

### `ToErrorOrError<TValue>(this Exception ex, ErrorType expectedErrorType = ErrorType.Unexpected) : ErrorOr<TValue>`

Same conversion, but returns a **failed** `ErrorOr<TValue>` wrapping the whole error list — convenient for
returning straight out of a method.

```csharp
public ErrorOr<User> Load(int id)
{
    try
    {
        return repository.Get(id);
    }
    catch (Exception ex)
    {
        return ex.ToErrorOrError<User>(ErrorType.NotFound);
    }
}
```

---

## PathExtensions

File‑system path helpers. Namespace: **`Forge.Next.Shared.IO`** (`using Forge.Next.Shared.IO;`).

### `PerformFolderSecurityCheck(string path) : ErrorOr<Success>`

Verifies a folder is usable by performing a real write probe: it creates the directory if it does not
exist, then creates and deletes a uniquely named temporary file inside it.

- Returns a **successful** `ErrorOr<Success>` (`Result.Success`) when the directory exists (or could be
  created) and is writable.
- Returns a **`Validation` error** (`"Path cannot be null"`) when `path` is `null` — it does **not** throw.
- Returns an **error result** converted from the caught exception when any file‑system operation fails (for
  example due to missing permissions, an empty path, or an invalid path).

```csharp
using ErrorOr;
using Forge.Next.Shared.IO;

ErrorOr<Success> result = PathExtensions.PerformFolderSecurityCheck(@"C:\data\exports");

if (!result.IsError)
{
    // The folder exists (or was created) and is writable.
}
else
{
    // Not writable — inspect the error(s):
    Console.WriteLine(result.FirstError.Type);          // e.g. Validation or Unexpected
    Console.WriteLine(result.FirstError.Description);    // human-readable reason
}
```

> A non‑existent folder is **created** as a side effect of a successful probe.

---

## UIExtensions

Extension methods for detecting and invoking on WinForms / WPF UI objects **without referencing the
WinForms or WPF assemblies** — recognition is based on type names (see [`Consts`](#consts)) and invocation
is performed via reflection. Namespace: **`Forge.Next.Shared.UI`** (`using Forge.Next.Shared.UI;`). Because
they are extension methods on `object?`, you can call them directly on any instance (e.g.
`myControl.IsWinFormsControl()`). All four methods are **null‑tolerant**: the `Is*` methods return `false`
and the `InvokeOn*` methods return `null` for a `null` receiver (they never throw for
`null`).

### `IsWinFormsControl(this object? obj) : bool`

Returns `true` when `obj`'s type (or any of its base types) is `System.Windows.Forms.Control`; returns
`false` for any other object and for `null`.

```csharp
bool isControl = someObject.IsWinFormsControl();
```

### `InvokeOnWinFormsControl(this object? control, Delegate @delegate, IEnumerable<object?>? parameters = null) : object?`

Reflectively calls `control.Invoke(Delegate, object[])` to marshal `@delegate` onto the control's UI
thread, returning the delegate's result. `parameters` is optional — when omitted (or `null`) the delegate
is invoked with no arguments. Returns `null` when `control` is `null`.

```csharp
// Update a WinForms label from a background thread:
myLabel.InvokeOnWinFormsControl(
    @delegate: new Action<string>(text => myLabel.Text = text),
    parameters: new object?[] { "Done" });
```

### `IsWPFDependency(this object? obj) : bool`

Returns `true` when `obj`'s type (or any of its base types) is `System.Windows.DependencyObject`; returns
`false` for any other object and for `null`.

```csharp
bool isDependency = viewModelTargetObject.IsWPFDependency();
```

### `InvokeOnWPFDependency(this object? control, Delegate @delegate, IEnumerable<object?>? parameters = null) : object?`

Reflectively reads the object's `Dispatcher` property and calls `Dispatcher.Invoke(Delegate, object[])` to
marshal `@delegate` onto the WPF UI thread, returning the delegate's result. `parameters` is optional — when
omitted (or `null`) the delegate is invoked with no arguments. Returns `null` when `control` is `null`.

```csharp
myWpfControl.InvokeOnWPFDependency(
    @delegate: new Action<string>(text => myWpfControl.Title = text),
    parameters: new object?[] { "Ready" });
```

---

## Event

Fault‑tolerant invocation of multicast delegates (events). `Event.Fire` walks the delegate's invocation
list and invokes each handler **independently**, so a handler that throws never prevents the remaining
handlers from running — instead its exception is captured into the result. Handlers can optionally be
marshalled onto a WinForms/WPF UI thread, and an optional `ILogger` traces each invocation.

### `Fire(Delegate @event, IEnumerable<object?> parameters, bool controlInvoke = false, ILogger? logger = null) : IReadOnlyCollection<object?>`

Invokes every handler of `@event` in order and returns a read‑only collection containing, per handler:

- the handler's **return value** (which may be `null`, e.g. for `void` handlers), or
- the **`Exception`** that the handler threw (caught, not propagated).

Behavior:

- A `null` `@event` returns an empty collection.
- `parameters` are passed to every handler.
- When `controlInvoke` is `true`, a handler whose target is a `System.Windows.Forms.Control` or a
  `System.Windows.DependencyObject` is invoked on that object's UI thread (via
  [`UIExtensions`](#uiextensions)); all other handlers run directly on the calling thread.
- When a `logger` is supplied, a `Debug` entry is written before and after each invocation, and an `Error`
  entry is written when a handler throws.

**Basic usage — collecting results and per‑handler failures:**

```csharp
using Forge.Next.Shared;

// A multicast delegate with three subscribers, the middle one faulty:
Func<int, int>? calculate = null;
calculate += x => x + 1;
calculate += x => throw new InvalidOperationException("bad handler");
calculate += x => x * 2;

IReadOnlyCollection<object?> results = Event.Fire(calculate!, new object?[] { 10 });

// Every handler ran despite the exception in the second one:
foreach (object? r in results)
{
    if (r is Exception ex)
        Console.WriteLine($"Handler failed: {ex.Message}"); // "bad handler"
    else
        Console.WriteLine($"Handler returned: {r}");         // 11, then 20
}
```

**Marshalling UI handlers onto their UI thread:**

```csharp
// Handlers that belong to a WinForms control or WPF dependency object are invoked on their UI thread;
// other handlers run inline. Useful when raising an event from a background thread.
IReadOnlyCollection<object?> r = Event.Fire(
    MyChanged,
    parameters: new object?[] { this, EventArgs.Empty },
    controlInvoke: true);
```

**With diagnostics:**

```csharp
ILogger logger = loggerFactory.CreateLogger("Events");

Event.Fire(
    MyChanged,
    parameters: new object?[] { this, EventArgs.Empty },
    controlInvoke: true,
    logger: logger);
```

> Because failures are returned rather than thrown, always inspect the result if you need to know whether
> every handler succeeded — check for `Exception` elements in the returned collection.

---

## LogUtils

Diagnostic logging helpers that dump current **process**, **AppDomain** and **loaded‑assembly** information
through an `ILogger`, and that can subscribe to AppDomain events (assembly loads and unhandled exceptions).
Namespace: **`Forge.Next.Shared.Logging`** (`using Forge.Next.Shared.Logging;`). It is a `static` class with
process‑wide state.

All informational output is written at `Information` level (the unhandled‑exception trace uses `Critical`);
each method is a no‑op when that level isn't enabled. Configure the logger first — until then a no‑op logger
is used.

> On `net48`, `LogUtils` requires the `Microsoft.Extensions.Logging.Abstractions` package (already a
> dependency of this library on that target).

### `ConfigureLogging(ILoggerFactory loggerFactory)`

Sets the logger used by every method (category name `"LogUtils"`). A `null` factory disables logging (no‑op
logger).

```csharp
using Forge.Next.Shared.Logging;

LogUtils.ConfigureLogging(loggerFactory);   // start logging through your ILoggerFactory
LogUtils.ConfigureLogging(null);            // disable again
```

### `LogProcessInfo()`

Logs current process details (id, priority, name, machine, session, start time, start‑info).

### `LogDomainInfo()`

Logs current `AppDomain` details (id, directories, friendly name, trust flags, and — on .NET Framework —
setup information).

### `LogLoadedAssemblies()`

Logs every assembly loaded into the `AppDomain` together with its properties (full name, location, runtime
version, ...).

```csharp
LogUtils.LogProcessInfo();
LogUtils.LogDomainInfo();
LogUtils.LogLoadedAssemblies();
```

### `TraceAssemblyLoads(bool state)`

Starts (`true`) or stops (`false`) logging each newly loaded assembly by subscribing to / unsubscribing from
`AppDomain.AssemblyLoad`. The current state is exposed by the read‑only `IsSubscribedForAssemblyLoad`
property.

```csharp
LogUtils.TraceAssemblyLoads(true);    // log assemblies as they load
// ...
LogUtils.TraceAssemblyLoads(false);   // stop
```

### `IsSubscribedForAppDomainUnhandledException { get; set; }`

Subscribes (`true`) or unsubscribes (`false`) a handler that logs unhandled exceptions at `Critical` level.
Idempotent and thread‑safe.

```csharp
LogUtils.IsSubscribedForAppDomainUnhandledException = true;
```

### `LogAll()`

One‑call diagnostic dump: logs process, AppDomain and loaded‑assembly information, then turns on assembly‑load
tracing and the unhandled‑exception subscription.

```csharp
LogUtils.ConfigureLogging(loggerFactory);
LogUtils.LogAll();   // dump everything + start ongoing tracing
```

> `LogAll` leaves two global subscriptions active (assembly‑load and unhandled‑exception). Turn them off with
> `LogUtils.TraceAssemblyLoads(false)` and `LogUtils.IsSubscribedForAppDomainUnhandledException = false` when
> you no longer need them.

---

## Consts

String constants used by [`UIExtensions`](#uiextensions) to identify UI framework types.

| Constant | Value |
|---|---|
| `Consts.WINDOWS_FORMS_CONTROL` | `"System.Windows.Forms.Control"` |
| `Consts.WINDOWS_DEPENDENCY_OBJECT` | `"System.Windows.DependencyObject"` |

```csharp
string controlTypeName = Consts.WINDOWS_FORMS_CONTROL;
```

---

## InitializationException

An `Exception` subtype for initialization‑failure scenarios. It provides the three standard exception
constructors.

| Constructor | Description |
|---|---|
| `InitializationException()` | Parameterless. |
| `InitializationException(string? message)` | With a message. |
| `InitializationException(string? message, Exception? innerException)` | With a message and an inner exception. |

```csharp
if (!TryInitialize(out string? reason))
{
    throw new InitializationException($"Startup failed: {reason}");
}

// Wrapping a root cause:
try
{
    LoadConfiguration();
}
catch (IOException io)
{
    throw new InitializationException("Could not load configuration.", io);
}
```

---

## License

Licensed under the **Apache‑2.0** license. See the
[repository](https://github.com/JZO001/Forge.Next.Shared) for details.
