# Verification

## The checks

| Check | How | What it proves |
| --- | --- | --- |
| Build | `dotnet build Forge.Next.Shared.slnx -c Debug --no-incremental` | 0 warnings for every target (`net48`, `net8.0`, `net9.0`, `net10.0`, `net11.0`). Needs the .NET 11 SDK. |
| Tests | `dotnet test tests/Forge.Next.Shared.Tests` | 74 test cases (68 test methods), against the `net10.0` build of the library |
| Tests on another target | A temporary test project outside the repository with `<TargetFramework>net11.0</TargetFramework>`, the test files from `tests/Forge.Next.Shared.Tests` as `Compile` items (`EnableDefaultCompileItems` off), the same package references, and a `ProjectReference` to the library; then `dotnet test` | The library's build for that target passes the same tests. Needed while the test project targets `net10.0` only (see `OPEN.md`). |
| Package contents | `dotnet build -c Release`, then open `artifacts/Forge.Next.Shared.<version>.nupkg` as a zip | `lib/<target>/` for every target, the README and the icon; the `.nuspec` dependency groups match the table in `CLAUDE.md` |

## What was run

| Date | Check | Where | Result |
| --- | --- | --- | --- |
| 2026-11-11 | Build, after `net11.0` was added | Windows 11, SDK 11.0.100 | 0 warnings, 0 errors, all five targets |
| 2026-11-11 | Tests | Windows 11, runtime 10.0.15 | 74/74 |
| 2026-11-11 | Tests on `net11.0` (temporary project) | Windows 11, runtime 11.0.0 | 74/74 |
| 2026-11-11 | Package contents of `Forge.Next.Shared.3.3.1.nupkg` (built by Zoltán) | Windows 11 | `lib/` for all five targets; the `net11.0` group depends on `ErrorOr` only, the other .NET groups also on `Microsoft.Extensions.Logging.Abstractions` of their major version, `net48` also on `System.Text.Json`. The packed README still lists four targets. |
| 2026-11-11 | Package contents after the README fix (Release build with `PackageOutputPath` in a temporary folder; `artifacts/` untouched) | Windows 11 | 0 warnings; `lib/` for all five targets; the packed README is the fixed one (five targets, the dependency table, `net11.0` in the SHA-3 and `LogUtils` notes) |

## Not verified yet

See `OPEN.md`. In short:
- `net48`, `net8.0` and `net9.0` builds are compiled but not tested;
- the WinForms and WPF paths of `UIExtensions` and `Event.Fire` are tested with fakes only, not with a real control.
