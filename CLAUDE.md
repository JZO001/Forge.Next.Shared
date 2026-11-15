# Forge.Next.Shared – notes for AI sessions working on this repository

This repository holds one NuGet package: a small, dependency-light collection of helper types and extension methods
(fluent boolean branching, deep sequence comparison, exception-safe execution returning `ErrorOr` results, object
hashing, file-system probing, WinForms/WPF reflection helpers, diagnostic logging). Read in this order:

1. `README.md`: the user documentation: every public type and method, with examples. It is packed into the NuGet
   package.
2. This file: the rules that must hold in every change.
3. `AI/README.md`: the knowledge base: how the package came about and what was decided (`AI/HISTORY.md`), the design
   (`AI/DESIGN.md`), how it is verified (`AI/VERIFICATION.md`), and what is still open (`AI/OPEN.md`).

## Working with Zoltán

- **Language:** Zoltán Juhász owns the package. Zoltán writes in Hungarian; answer in Hungarian. Code, comments, commit
  messages and every document in this repository are in English.
- **Git and publishing are Zoltán's:** Zoltán manages the git repository and publishes the NuGet package. Do not run
  git commands that change the repository, and do not publish, unless asked. The `/commit.changes` command is such a
  request: it runs the tests and commits, without pushing.
- **Working style:**
  - one topic at a time;
  - options with a recommendation;
  - facts checked before they are stated;
  - after a work item, report and stop.
- **Keep the knowledge base current:** when a change, a decision or a verification happens, update `AI/` in the same
  step, so the next session can continue from it.
- **No reference to the package's consumers** (Zoltán, 2026-11-11): no name of a product, repository or internal
  decision of a project that uses, or will use, this library, in the code, the documents or this knowledge base.
  Describe a consumer generically ("a desktop application", "a local admin tool"). Allowed:
  - the other `Forge.*` projects;
  - the libraries and tools this package uses or documents: ErrorOr, the `Microsoft.*` libraries, System.Text.Json,
    xUnit, Shouldly, NSubstitute.

## Invariants (do not break)

1. **The public API is a contract.** The package is published and has consumers.
   - A new parameter is optional, and its default keeps the earlier behaviour (as in 3.1.0, 3.2.0 and 3.3.0).
   - In async methods the `CancellationToken` stays the last parameter.
   - A change that breaks a caller (a removed member, a changed signature or default) raises at least the minor
     version, and the commit message says what breaks under "Compatibility".
2. **Dependency-light.** `ErrorOr` is the only dependency on every target. Anything else is referenced only where the
   framework lacks it:

   | Target | Extra package references |
   | --- | --- |
   | `net48` | `Microsoft.Extensions.Logging.Abstractions` 8.0.0, `System.Text.Json` 8.0.6 |
   | `net8.0`, `net9.0`, `net10.0` | `Microsoft.Extensions.Logging.Abstractions` of the same major version |
   | `net11.0` | none: `Microsoft.Extensions.Logging.Abstractions` is part of the `Microsoft.NETCore.App` shared framework from .NET 11 |

   Do not add a package without asking. A future target (`net12.0` …) gets a package reference only if its shared
   framework stops providing the assembly; check the ref pack (`packs/Microsoft.NETCore.App.Ref/<version>/ref`).
3. **One source for every target.** `net48` is the only target without `NETCOREAPP`. Code that cannot exist there sits
   behind `#if NETCOREAPP` (or `NETCOREAPP3_1_OR_GREATER`), and the README says which targets lack it (as for SHA-3).
   Do not add per-target code for the .NET targets unless the API really differs.
4. **The safe wrappers do not throw for the work they wrap.**
   - `Protect`, `ProtectDo`, `ProtectAsync`, `ProtectDoAsync` turn every exception of the delegate into `ErrorOr`
     errors. The only exception: with `rethrowCancellation: true`, an `OperationCanceledException` while the caller's
     own token requested cancellation is rethrown.
   - `Event.Fire` invokes every handler of the invocation list, even when one throws; the exception becomes that
     handler's element in the result.
   - `PathExtensions.PerformFolderSecurityCheck` returns errors, it does not throw.
5. **The error shape is stable.** `ToErrorOrError` produces one `Error` per exception in the inner-exception chain,
   outermost first, with the metadata keys `type` (assembly-qualified name), `message` and `stackTrace`.
   `includeStackTrace: false` leaves out `stackTrace` only.
6. **No UI framework reference.** `UIExtensions` finds WinForms controls and WPF dependency objects by the full type
   name in their base-type chain (`Consts`) and calls `Invoke` through reflection. The package must not reference
   WinForms or WPF.
7. **A hash is the hash of the JSON.** The hashing helpers hash the UTF-8 JSON made with `DefaultJsonSerializerOptions`.
   Changing those options changes every hash value: treat it as a breaking change. The `*AsInt` values use
   `HashCode.Combine` and differ between processes; the README says not to persist them.

## Conventions

- **Code style:**
  - explicit types (no `var`), file-scoped namespaces, one type per file named after the type;
  - sub-folders map to sub-namespaces (`IO`, `UI`, `Logging`);
  - `LangVersion` 12 for every target, `Nullable` and `ImplicitUsings` on;
  - XML documentation on every public member (`GenerateDocumentationFile`, no `DocumentationFile`: each target writes
    its own file under `obj/`), saying what null means, what comes back on failure, and why.
- **Tests** (`tests/Forge.Next.Shared.Tests`, every target of the library, `net48` to `net11.0`):
  - xUnit 2, Shouldly, NSubstitute; `LangVersion` 14 for every target (`net48` would default to C# 7.3, and C# 12
    makes the Shouldly `ShouldContainKey` calls ambiguous on `net9.0`);
  - code that a target lacks is tested behind the same `#if` as in the library (the SHA-3 tests: `#if NETCOREAPP`);
  - one test class per tested class (`<Class>Tests`), test methods named after the tested member with a `Test`
    suffix (`ProtectIncludeStackTraceTest`), richly commented (see `prompts/init_tests.txt`).
- **Before handing over a change:**
  - a 0-warning build of the solution, all targets;
  - all tests, on every target;
  - when a target is added: add it to the test project's `TargetFrameworks` as well;
  - when a target was added or a dependency changed: the package's `.nuspec` dependency groups checked (see
    `AI/VERIFICATION.md`).
- **Documentation:** every public change is reflected in `README.md` in the same step: the signature, an example, and
  the target availability in "Requirements".
- **Release:**
  1. Raise `AssemblyVersion` in `Forge.Next.Shared/Forge.Next.Shared.csproj` (`Version` and `FileVersion` follow it).
  2. Set the same version in the README's installation example (`PackageReference … Version=`).
  3. Run `dotnet build -c Release`; it writes the package to `artifacts/` (`GeneratePackageOnBuild`). A Release build
     overwrites the package of the current version there, so build a published version again only on purpose. To
     check a package without touching `artifacts/`, build the library project with
     `-p:PackageOutputPath=<another folder>\`.
  4. Zoltán publishes it.
