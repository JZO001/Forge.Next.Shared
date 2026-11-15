# Verification

## The checks

| Check | How | What it proves |
| --- | --- | --- |
| Build | `dotnet build Forge.Next.Shared.slnx -c Debug --no-incremental` | 0 warnings for every target (`net48`, `net8.0`, `net9.0`, `net10.0`, `net11.0`), the library and the tests. Needs the .NET 11 SDK. |
| Tests | `dotnet test tests/Forge.Next.Shared.Tests` | The test project targets every target of the library, so each build of the library passes the tests: 77 test cases on each .NET target, 71 on `net48` (the SHA-3 tests are behind `#if NETCOREAPP`). Needs the .NET 8 to 11 runtimes and .NET Framework 4.8. |
| Package contents | `dotnet build -c Release` (into `artifacts/`), or the library project with `-p:PackageOutputPath=<a folder outside the repository>\`; then open the `.nupkg` as a zip | `lib/<target>/` for every target, the README and the icon; each `lib/<target>/Forge.Next.Shared.xml` parses as XML, the four .NET targets' files are the same and `net48` has its own; the `.nuspec` dependency groups match the table in `CLAUDE.md` |

## What was run

| Date | Check | Where | Result |
| --- | --- | --- | --- |
| 2026-11-11 | Build, after `net11.0` was added | Windows 11, SDK 11.0.100 | 0 warnings, 0 errors, all five targets |
| 2026-11-11 | Tests | Windows 11, runtime 10.0.15 | 74/74 |
| 2026-11-11 | Tests on `net11.0` (a temporary test project with the test files as `Compile` items and a `ProjectReference` to the library) | Windows 11, runtime 11.0.0 | 74/74 |
| 2026-11-11 | Package contents of `Forge.Next.Shared.3.3.1.nupkg` (built by Zoltán) | Windows 11 | `lib/` for all five targets; the `net11.0` group depends on `ErrorOr` only, the other .NET groups also on `Microsoft.Extensions.Logging.Abstractions` of their major version, `net48` also on `System.Text.Json`. The packed README still lists four targets. |
| 2026-11-11 | Package contents after the README fix (Release build with `PackageOutputPath` in a temporary folder; `artifacts/` untouched) | Windows 11 | 0 warnings; `lib/` for all five targets; the packed README is the fixed one (five targets, the dependency table, `net11.0` in the SHA-3 and `LogUtils` notes) |
| 2026-11-11 | The new icon: Release build with `PackageOutputPath` in a temporary folder; `artifacts/` untouched | Windows 11, SDK 11.0.100 | 0 warnings; the package's `logo.png` is the new 128 × 128 mark, named as the icon in the `.nuspec` |
| 2026-11-11 | The XML documentation in the local packages (read only: each `lib/<target>/Forge.Next.Shared.xml` parsed, its members counted, its SHA-256 compared) | Windows 11 | 3.0.0 and 3.1.0 whole (`net48` 71 members, the others 77); 3.1.1: one file, 78 members, under all four targets; 3.2.0: `lib/net8.0` holds the `net48` file (72 members, the others 78); 3.3.0 and 3.3.1 whole (`net48` 72, the others 78) |
| 2026-11-15 | Before the tests moved to every target: the unchanged test files on all five targets (a temporary test project outside the repository) | Windows 11, SDK 11.0.100 | With `LangVersion` 14: 0 warnings; 74/74 on `net8.0` to `net11.0`, 68/68 on `net48`. With `LangVersion` 12: 7 × CS0121 on `net9.0` (Shouldly `ShouldContainKey`/`ShouldNotContainKey` ambiguous between the `IDictionary` and `IReadOnlyDictionary` overloads) |
| 2026-11-15 | Before the `#if IS_WINDOWS` fix: `Process.GetCurrentProcess().StartInfo` and the Windows check (a temporary console project) | Windows 11 | `net48`: an empty `ProcessStartInfo`, `Domain` and `LoadUserProfile` readable; `net8.0`, `net10.0`: `InvalidOperationException` ("Process was not started by this object …"). `ProcessStartInfo.Domain`/`LoadUserProfile` unguarded: CA1416 on the .NET targets; behind `RuntimeInformation.IsOSPlatform(OSPlatform.Windows)`: 0 warnings |
| 2026-11-15 | Build, after the 3.3.2 fixes (before the `TraceAssemblyLoads` fix) | Windows 11, SDK 11.0.100 | 0 warnings, 0 errors; the library and the tests on all five targets; each target's XML documentation under `obj/Debug/<target>/`, none in the project folder |
| 2026-11-15 | Tests on every target | Windows 11; runtimes 8.0.34, 9.0.23, 10.0.15, 11.0.0; .NET Framework 4.8 | 76/76 on `net8.0`, `net9.0`, `net10.0`, `net11.0`; 70/70 on `net48` |
| 2026-11-15 | Package contents, two Release builds of 3.3.2 with `PackageOutputPath` in temporary folders | Windows 11 | 0 warnings; every `lib/<target>/Forge.Next.Shared.xml` parses; the four .NET files are the same (78 members), `net48` has its own (72 members); the same files in both builds; dependency groups as in `CLAUDE.md`; the new icon |
| 2026-11-15 | Package contents of `artifacts/Forge.Next.Shared.3.3.2.nupkg` (`dotnet build -c Release` of the library) | Windows 11 | As the line above, with the same XML files; the packed README is the repository's (3.3.2 in the installation example, the `WithIndex` section, the start-info note). Superseded by the rebuild below. |
| 2026-11-15 | Before the `TraceAssemblyLoads` fix: which loads raise `AppDomain.AssemblyLoad` (a temporary console project with `LogUtils` and a recording logger) | Windows 11 | `AssemblyBuilder.DefineDynamicAssembly`: exactly one event on `net48`, `net8.0` and `net10.0`, and the handler does not throw; `Assembly.Load(byte[])`: 5 to 6 events (its dependencies load too) |
| 2026-11-15 | The new tests against the old condition (`else if (IsSubscribedForAssemblyLoad)` put back for one run, then the fix restored and compared byte for byte) | Windows 11 | `TraceAssemblyLoadsIdempotentTest` and `LogAllTest` fail on all five targets (`IsSubscribedForAssemblyLoad should be True`) |
| 2026-11-15 | Build and tests after the `TraceAssemblyLoads` fix | Windows 11; runtimes 8.0.34, 9.0.23, 10.0.15, 11.0.0; .NET Framework 4.8 | 0 warnings, 0 errors; 77/77 on `net8.0`, `net9.0`, `net10.0`, `net11.0`; 71/71 on `net48` |
| 2026-11-15 | Strong naming: the public key token of every `lib/<target>/Forge.Next.Shared.dll` in the local packages, and of the `net48` dependencies; a throwaway strong-named `net48` library using `ErrorOr`, called from a `net48` console project | Windows 11, .NET Framework 4.8 | 3.0.0 to 3.3.2: no DLL strong-named. `ErrorOr` 2.1.1 (`netstandard2.0`): not strong-named; `Microsoft.Extensions.Logging.Abstractions` 8.0.0 and `System.Text.Json` 8.0.6: strong-named. The probe: CS8002 at build, `FileLoadException` (0x80131044, "A strongly-named assembly is required") at run time |
| 2026-11-15 | Package contents of the rebuilt `artifacts/Forge.Next.Shared.3.3.2.nupkg` | Windows 11 | 0 warnings; every `lib/<target>/Forge.Next.Shared.xml` parses, the four .NET files the same (78 members), `net48` its own (72); the `TraceAssemblyLoads` documentation in them; dependency groups as in `CLAUDE.md`; the packed README is the repository's; the new icon |

## Not verified yet

See `OPEN.md`. In short: the WinForms and WPF paths of `UIExtensions` and `Event.Fire` are tested with fakes only,
not with a real control.
