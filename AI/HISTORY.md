# History and decisions

Newest last. Each entry: what was asked or decided, by whom, and why. The entries before 2026-11-11 are reconstructed
from the commit messages.

## 2026-06-30 – 3.0.0: the library

The first full implementation, multi-targeting `net48`, `net8.0`, `net9.0` and `net10.0`:
- `BoolExtensions`, `IEnumerableExtensions` (`IsDeepEqual`), `ISetExtensions`, `ObjectExtensions` (the `Protect*`
  wrappers, the hashing helpers), `ExceptionErrorOrExtensions`, `Event`, `IO/PathExtensions`, `UI/UIExtensions`,
  `Logging/LogUtils`, `Consts`, `InitializationException`;
- `ErrorOr` as the only common dependency; `net48` also gets `Microsoft.Extensions.Logging.Abstractions` and
  `System.Text.Json`; SHA-3 only on the `NETCOREAPP` targets;
- the test project (`net10.0`, xUnit, Shouldly, NSubstitute), the README as a full API reference; the prompts asking
  for the README and the tests are in `prompts/`.

The generated `Forge.Next.Shared.xml` documentation file was removed from the repository and ignored.

## 2026-06-30 – 3.1.0: cancellation in the async wrappers

`ProtectAsync` and `ProtectDoAsync` take a `CancellationToken` and pass it to the delegate, whose shape became
`Func<T, CancellationToken, Task…>`. A breaking change of the delegate type, released as a minor version.

## 2026-07-11 – 3.1.1: `WithIndex`

`IEnumerableExtensions.WithIndex<T>()` projects a sequence into `(item, index)` tuples, for
`foreach ((T item, int i) in list.WithIndex())`. Its README section and test came in 3.3.2.

## 2026-10-27 – 3.2.0: opt-in cancellation rethrow, stack trace omission

- `ProtectAsync` and `ProtectDoAsync`: `rethrowCancellation` (default `false`). When on, an
  `OperationCanceledException` while the caller's token requested cancellation is rethrown, following the .NET
  convention. A cancellation the token did not request (an inner timeout) still becomes an error.
- `ToErrorOrError`: `includeStackTrace` (default `true`). When off, the `stackTrace` metadata is left out, for errors
  that leave the process or reach a user or a language model, where the trace is an information leak.
- The package got a proper license expression (`Apache-2.0`).

Both switches keep the earlier behaviour by default.

## 2026-10-28 – 3.3.0: `includeStackTrace` on the `Protect*` wrappers

All four `Protect*` wrappers take `includeStackTrace` and pass it to `ToErrorOrError`. In the async ones it sits after
`rethrowCancellation`, so the `CancellationToken` stays last. A fully positional async call that passed the token right
after `rethrowCancellation` no longer compiles; the commit message said so under "Compatibility".

## 2026-11-11 – 3.3.1: the `net11.0` target; the knowledge base

**Zoltán's change:** `net11.0` added to `TargetFrameworks`, `AssemblyVersion` 3.3.0.0 → 3.3.1.0. No source change, so a
patch version.

**Reviewed at Zoltán's request the same day:**
- The `net11.0` target has no `Microsoft.Extensions.Logging.Abstractions` reference, unlike `net8.0` to `net10.0`, and
  it builds: from .NET 11 the assembly is part of the `Microsoft.NETCore.App` shared framework (checked in the
  `Microsoft.NETCore.App.Ref` 11.0.0 ref pack, together with the other `Microsoft.Extensions.*.Abstractions`). The
  package's `net11.0` dependency group therefore lists `ErrorOr` only, which is correct.
- `net11.0` defines `NETCOREAPP` and `NETCOREAPP3_1_OR_GREATER`, so it compiles the same code paths as the other .NET
  targets (SHA-3 included).
- Build and tests: see `VERIFICATION.md`, 2026-11-11.
- Found: the README still lists four targets; the test project does not run the `net11.0` build; the local
  `artifacts/Forge.Next.Shared.3.3.0.nupkg` was rebuilt with `net11.0` before the version was raised. See `OPEN.md`.

**The README, fixed at Zoltán's request the same day:**
- "Requirements" lists the five targets, and a table gives each target's further dependencies (the logging
  abstractions on `net48` to `net10.0`, none on `net11.0`; System.Text.Json on `net48`). The old text said only `net48`
  pulls in the logging abstractions.
- Both SHA-3 notes name `net8.0` to `net11.0`; the second one no longer claims that every shipped build has SHA-3
  (`net48` does not).
- The `LogUtils` note covers every target, not only `net48`.
- The installation example shows 3.3.1 instead of 3.0.0. Keeping it current became a release step in `CLAUDE.md`.

**The knowledge base** (`CLAUDE.md`, `AI/`) was created at Zoltán's request, following the structure of the
`Forge.Next.LocalWebShell` repository's knowledge base.

## 2026-11-11 – Which projects may be named

The knowledge base first carried over a rule from `Forge.Next.LocalWebShell`: no reference to any other project.
Zoltán: that wording was unfortunate. The intent is that the projects which use this library are not named. The other
`Forge.*` projects, and the libraries this package uses (ErrorOr, the `Microsoft.*` libraries), may be named. The rule
in `CLAUDE.md` now says so.

## 2026-11-11 – The package icon: the Forge mark

Zoltán chose a new icon for the Forge packages: the Forge mark, a hexagon with a smaller, filled hexagon inside, the two
joined by three lines, on a dark rounded tile, so it reads on a light and on a dark ground. `logo.png` is now this mark,
128 by 128 pixels (NuGet's advice), in place of the earlier logo, which looked much like the Jedi Order's emblem
(Lucasfilm), a trademark risk. The project packs `logo.png` as before. The published versions keep the old icon; the
next version carries the new one.

## 2026-11-15 – 3.3.2: the open findings fixed

**Zoltán's change:** `AssemblyVersion` 3.3.1.0 → 3.3.2.0, 3.3.1 being published (Zoltán, 2026-11-15). The README's
installation example follows it.

**Asked by Zoltán:** the fixes of the findings listed in `OPEN.md` on 2026-11-12, with the recommended options. No
public signature changed.
- **One XML documentation file per target.** The `DocumentationFile` line, the
  `<None Remove="Forge.Next.Shared.xml" />` item and the `.gitignore` entry were removed, and the stray file deleted.
  `GenerateDocumentationFile` stays, so each target writes its own file under `obj/<configuration>/<target>/`, and
  the parallel target builds no longer share one file.
- **The tests on every target.** The test project targets `net48`, `net8.0`, `net9.0`, `net10.0` and
  `net11.0`, with `LangVersion` 14. Every target, not only `net10.0` and `net11.0`, because `net48` has code of its
  own. 14, because `net48` would default to C# 7.3, and C# 12 (the library's) makes seven Shouldly
  `ShouldContainKey`/`ShouldNotContainKey` calls ambiguous on `net9.0`; 14 is what `net10.0` used by default, so its
  run did not change. No test had to change; the SHA-3 tests stay behind `#if NETCOREAPP`.
- **`WithIndex`:** a README section, a full XML documentation (zero-based, deferred, a `null` source throws
  `ArgumentNullException` at the call, unlike `ForEach`), and `WithIndexTest`.
- **`#if IS_WINDOWS`** in `LogUtils.LogProcessInfo`: of the three options (define the symbol, check at run time,
  remove the lines), the run-time check `RuntimeInformation.IsOSPlatform(OSPlatform.Windows)`. The lines were meant
  to be logged on Windows; the check needs no `#if` and compiles on every target; a symbol defined at build time
  would describe the build machine, not the one the package runs on. What the log says changes on `net48` only (two
  more lines): on .NET, `Process.GetCurrentProcess().StartInfo` throws, so the start info is never logged there.
  The README and the XML documentation now say so; `LogProcessInfoStartInfoTest` covers both cases.
- **Test file names:** the test files and classes follow the tested class: `IO/PathExtensionsTests.cs` (was `PathHelperTests`)
  and `UI/UIExtensionsTests.cs` (was `UIReflectionExtensionsTests`).
- **The `net11.0` dependency:** a comment in the csproj says why `net11.0` has no logging package reference.

**Found on the way, fixed at Zoltán's request the same day:** `LogUtils.TraceAssemblyLoads(true)` while the tracing
was on unsubscribed the handler: the method unsubscribed whenever it was subscribed, whatever `state` was, so a second
`TraceAssemblyLoads(true)` or a second `LogAll()` stopped the tracing, against its documentation. Now it unsubscribes
only for `false`, so it is idempotent with at most one subscription. A behaviour fix, in the patch version 3.3.2; the
README and the XML documentation say that versions before 3.3.2 turned the tracing off. `TraceAssemblyLoadsIdempotentTest`
checks the subscription through its effect (a dynamic assembly with a unique name is logged exactly once), and
`LogAllTest` calls `LogAll` twice; both fail with the old condition on every target. The tests' `RecordingLogger`
writes under a lock, because while the tracing is on, test classes running in parallel log their assembly loads into
it from other threads.

**Strong naming, asked by Zoltán the same day:** the `net48` build was once meant to be strong-named, and the
`Properties/AssemblyInfo.cs` is gone. Findings: no signing setting or key file, and no `AssemblyInfo.cs` in the
repository's history (it starts on 2026-06-29; the empty `Properties` folder dates from 2026-06-30); none of the local
packages is strong-named. Zoltán recalled the reason: `ErrorOr` is not strong-named, which a probe confirmed (see
`DESIGN.md`). It stays unsigned.

**The fix plan** of 2026-11-12 was a working file; with the work done, Zoltán had it deleted, and the knowledge base no
longer refers to it.
