# Design

## The parts

All types are public and static, except `InitializationException`.

| File | Namespace | Role |
| --- | --- | --- |
| `BoolExtensions.cs` | `Forge.Next.Shared` | `IfTrue` / `IfFalse` (value), `IfTrueDo` / `IfFalseDo` (side effect, return the condition for chaining), each with a value-forwarding and an async variant |
| `IEnumerableExtensions.cs` | `Forge.Next.Shared` | `ForEach`, `WithIndex`, `IsDeepEqual` (recursive; strings are compared as values, not as sequences) |
| `ISetExtensions.cs` | `Forge.Next.Shared` | `AddRange` for `ISet<T>` |
| `ObjectExtensions.cs` | `Forge.Next.Shared` | The `Protect*` wrappers (exception → `ErrorOr`); the hashing helpers (SHA-2 on every target, SHA-3 on `NETCOREAPP`); `DefaultJsonSerializerOptions` |
| `ExceptionErrorOrExtensions.cs` | `Forge.Next.Shared` | `ToErrorOrError`: an exception and its inner exceptions → `List<Error>` or `ErrorOr<T>` |
| `Event.cs` | `Forge.Next.Shared` | `Fire`: invokes each handler of a multicast delegate separately, optionally on the UI thread, optionally logged |
| `Consts.cs` | `Forge.Next.Shared` | The full type names of the WinForms `Control` and the WPF `DependencyObject` |
| `InitializationException.cs` | `Forge.Next.Shared` | An exception type for failed initialization |
| `IO/PathExtensions.cs` | `Forge.Next.Shared.IO` | `PerformFolderSecurityCheck`: creates the folder if needed, and writes and deletes a test file |
| `UI/UIExtensions.cs` | `Forge.Next.Shared.UI` | WinForms / WPF detection and `Invoke` through reflection |
| `Logging/LogUtils.cs` | `Forge.Next.Shared.Logging` | Process, AppDomain and assembly diagnostics through an `ILogger`; optional AppDomain event subscriptions; process-wide static state |

## The targets

| Target | `NETCOREAPP` | Package references besides `ErrorOr` 2.1.1 |
| --- | --- | --- |
| `net48` | no | `Microsoft.Extensions.Logging.Abstractions` 8.0.0, `System.Text.Json` 8.0.6 |
| `net8.0` | yes | `Microsoft.Extensions.Logging.Abstractions` 8.0.0 |
| `net9.0` | yes | `Microsoft.Extensions.Logging.Abstractions` 9.0.0 |
| `net10.0` | yes | `Microsoft.Extensions.Logging.Abstractions` 10.0.0 |
| `net11.0` | yes | none |

**What differs on `net48`:**
- no SHA-3 helpers (`#if NETCOREAPP` in `ObjectExtensions`);
- `LogUtils` logs the .NET Framework-only data: the AppDomain setup information, `Assembly.CodeBase` instead of
  `Location`, `GlobalAssemblyCache`.

`#if IS_WINDOWS` in `LogUtils.LogProcessInfo` guards two `ProcessStartInfo` properties (`Domain`, `LoadUserProfile`),
but nothing defines the symbol, so those lines are never compiled (see `OPEN.md`).

## Why each choice was made

- **Many targets, one source:** the same code serves .NET Framework 4.8 and current .NET; a new .NET release gets a
  target of its own (`net11.0` in 3.3.1).
- **Logging abstractions per major version on `net8.0` to `net10.0`:** the package does not force a newer
  `Microsoft.Extensions.Logging.Abstractions` on a consumer than its framework's own.
- **No logging reference on `net11.0`:** the shared framework provides it from .NET 11. A package reference there would
  add nothing but a lower-version entry for conflict resolution to override.
- **`ErrorOr` for failures:** expected failures are values, not exceptions; the `Protect*` wrappers are the bridge from
  code that throws.
- **Cancellation is converted by default, rethrown on request** (3.2.0): converting kept the earlier behaviour;
  rethrowing the caller's own cancellation follows the .NET convention. Only a cancellation the caller's token
  requested is rethrown, so an inner timeout is still an error.
- **The stack trace optional in the errors** (3.2.0, 3.3.0): errors that leave the process (HTTP responses, messages,
  a language model) must not carry it; the exception itself can be logged where the trace is needed.
- **`Event.Fire` collects instead of throwing:** one failing handler must not keep the others from running; the caller
  sees each handler's result or exception.
- **UI helpers by reflection and type name:** the package works for WinForms and WPF consumers without referencing
  either framework, and stays usable on every target.
- **Hashing through JSON:** any object can be hashed without implementing an interface; `ReferenceHandler.Preserve`
  handles cycles, `IncludeFields` takes fields into account.
