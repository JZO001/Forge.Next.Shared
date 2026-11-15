# Open items

As of 2026-11-15. Close an item by moving its outcome into `HISTORY.md` (decisions) or `VERIFICATION.md` (checks), and
deleting it here.

## Waiting for Zoltán

- **Publishing 3.3.2:** `artifacts/Forge.Next.Shared.3.3.2.nupkg` is the package to publish (see `VERIFICATION.md`,
  2026-11-15).
- **The local 3.3.0 package:** `artifacts/Forge.Next.Shared.3.3.0.nupkg` was rebuilt on 2026-11-11 with `net11.0`,
  before the version was raised. If 3.3.0 is published, the local file differs from the published one; do not publish
  it again.

## Not verified yet

- **WinForms and WPF:** `UIExtensions` and `Event.Fire` with `controlInvoke` are tested with fake types of the same
  full names, not with a real control or dispatcher.
