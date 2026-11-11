# Open items

As of 2026-11-11. Close an item by moving its outcome into `HISTORY.md` (decisions) or `VERIFICATION.md` (checks), and
deleting it here.

## Waiting for Zoltán

- **Testing the `net11.0` build in the repository:** the test project targets `net10.0` only. Recommended:
  `<TargetFrameworks>net10.0;net11.0</TargetFrameworks>` (or every .NET target the library has).
- **The local 3.3.0 package:** `artifacts/Forge.Next.Shared.3.3.0.nupkg` was rebuilt on 2026-11-11 with `net11.0`,
  before the version was raised. If 3.3.0 is published, the local file differs from the published one; do not publish
  it again.
- **Publishing 3.3.1:** the README is fixed (2026-11-11), but `artifacts/Forge.Next.Shared.3.3.1.nupkg` was built
  before that and still carries the old README. A `dotnet build -c Release` rebuilds it; then Zoltán publishes it.

## Not verified yet

- **The `net48`, `net8.0` and `net9.0` builds** are not tested: the test project runs `net10.0` only.
- **WinForms and WPF:** `UIExtensions` and `Event.Fire` with `controlInvoke` are tested with fake types of the same
  full names, not with a real control or dispatcher.

## Ideas, not decided

- **`WithIndex`** (3.1.1) has neither a README section nor a test.
- **`#if IS_WINDOWS`** in `LogUtils.LogProcessInfo`: nothing defines the symbol, so the `Domain` and `LoadUserProfile`
  lines are dead code. Either define it, guard the lines with `OperatingSystem.IsWindows()` (not on `net48`), or remove
  them.
- **Test file names** that do not follow the tested class: `IO/PathHelperTests.cs` tests `PathExtensions`,
  `UI/UIReflectionExtensionsTests.cs` tests `UIExtensions`.
- **A comment in the csproj** explaining why `net11.0` has no logging package reference, so nobody "fixes" it.
