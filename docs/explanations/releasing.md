# Releasing a new version

This repo publishes **six NuGet packages together** at a single shared version:
`BrowserApi`, `BrowserApi.JSInterop`, `BrowserApi.Blazor`, `BrowserApi.Runtime`, `BrowserApi.SourceGen`, `BrowserApi.Css.SourceGen`.

There are **two paths** to get a change into a consuming project:

| Path | Destination | When to use |
|---|---|---|
| **Public release** | nuget.org, visible to everyone | Cut versions, publish to the public feed, GitHub Release created automatically. |
| **Local feed** | Private local folder / server feed | Pre-release testing loop — pack a `-local.N` build, install into MitWare, verify, *then* cut a public release. |

### Which path to take

The choice depends on **what kind of change** the release contains:

- **Changes that alter the shape of generator *output* (BrowserApi.SourceGen)** — e.g. new type mappings, changed parameter types, new emitted classes, signature changes. These **must go through the local feed first**. The generator's output is compiled by downstream consumers, so a subtle shape mistake (wrong type name, CS0721-class errors, namespace resolution issues, ambiguous references) cannot be caught by the generator's own unit tests — it only surfaces when a real consumer tries to build. MitWare's build is the first real consumer. If it doesn't build against a local-feed `-local.N` package, the public release is not safe.
- **Internal refactors, fixes that don't change emitted code shape, and changes to non-SourceGen packages** (BrowserApi, BrowserApi.Blazor, BrowserApi.JSInterop, BrowserApi.Runtime) — go straight to the public path. The compile-output integration test (`tests/BrowserApi.SourceGen.Tests/JsModuleGeneratorDriverTests.cs` → `Generator_output_compiles_cleanly_against_real_Microsoft_JSInterop`) already catches the common cases, but it runs against a synthetic consumer, not a real one.

The default for a SourceGen output-shape change is **local feed → MitWare build → public**. When in doubt, take the longer path — a local-feed iteration costs minutes; a broken public preview costs a new version number and a cycle of consumer confusion.

When the user asks for a "release" without more detail, clarify which kind of change it is if unsure, then route accordingly. Suggest the local path proactively for SourceGen output-shape changes — do not wait to be asked.

## Path 1 — Public release to nuget.org

Release notes live in one file — `CHANGELOG.md` at the repo root. Every user-facing surface (nuget.org pages, IDE NuGet dialogs, GitHub Releases) is driven from that single file. There is no other place to update.

### The flow

1. **Move entries under `[Unreleased]` in `CHANGELOG.md` to a new dated version section**, e.g.:
   ```markdown
   ## [Unreleased]

   ## [0.1.0-preview.4] — 2026-05-10

   ### BrowserApi.SourceGen
   - Added …
   - Fixed …
   ```
   The heading must be exactly `## [<version>]` — the workflow matches this pattern to extract the release body.

2. **Commit the changelog change** and push to `main`.

3. **Tag the commit** and push the tag:
   ```bash
   git tag v0.1.0-preview.4
   git push origin v0.1.0-preview.4
   ```
   The tag name must be the version prefixed with `v`. The workflow strips the `v` to compute the package version.

That's it. The rest is automated by `.github/workflows/publish.yml`.

### What the workflow does

Triggered on any tag matching `v*`, it:

1. Extracts the version from the tag (`v0.1.0-preview.4` → `0.1.0-preview.4`).
2. **Fails early** if `CHANGELOG.md` doesn't have a `## [0.1.0-preview.4]` section — no silent publish without notes.
3. Restores, builds, runs the full test suite.
4. Packs all six packages with `-p:Version=<version>`.
5. Pushes all packages to nuget.org (`--skip-duplicate` makes re-runs safe).
6. Creates a GitHub Release tagged `v0.1.0-preview.4` with the CHANGELOG section as the body, and attaches the `.nupkg` files.

### Where release notes show up

| Surface | Source |
|---|---|
| nuget.org package page, "Release Notes" section | Each csproj has `<PackageReleaseNotes>See …/CHANGELOG.md…</PackageReleaseNotes>` — users click through to the file on GitHub. |
| IDE NuGet package dialogs (Visual Studio, Rider) | Same `<PackageReleaseNotes>` text. |
| GitHub Releases page (`/releases`) | The workflow extracts the matching `CHANGELOG.md` section and posts it as the release body. |

Editing `CHANGELOG.md` after the fact updates the nuget.org and IDE link target automatically (they point to the live file on GitHub). Changing a past GitHub Release body requires editing it manually on GitHub — or deleting the release and re-running the workflow.

### Versioning rules

- Follow [SemVer](https://semver.org/). While in preview, use `0.1.0-preview.N` (increment `N` per preview release).
- All five packages release at the same version. The `<Version>` in each csproj is a local-dev placeholder; the workflow overrides it via `-p:Version=`.
- Do **not** create a GitHub Release manually — the workflow handles that. Creating one by hand will result in two releases for the same tag.

### If something fails mid-release

- **Tests fail**: fix on `main`, delete the tag (`git tag -d vX && git push --delete origin vX`), re-tag the new commit.
- **NuGet push fails for one package**: re-run the workflow from the Actions tab. `--skip-duplicate` ensures already-pushed packages are not republished.
- **Wrong notes went out**: edit the CHANGELOG section, then edit the GitHub Release body by hand (or `gh release edit`). The nuget.org link will pick up the CHANGELOG edit automatically.

## Path 2 — Local feed for pre-release testing

Use this when a source-gen change needs to be validated in MitWare (or another consumer) before committing to a public nuget.org version. Nothing in this path touches git tags, the public NuGet feed, or the GitHub Releases page.

### The flow

1. **Pack a `-local.N` version** into the local feed:
   ```bash
   dotnet pack src/BrowserApi.SourceGen/BrowserApi.SourceGen.csproj -c Release \
       -o nupkgs -p:Version=0.1.0-local.X
   ```
   `nupkgs/` is already registered as NuGet source `BrowserApiLocal` (local dev).

2. **Deploy to the MitWare server if needed** (only when the change must reach the server, not just local dev):
   ```bash
   ./scripts/publish-local.sh 0.1.0-local.X
   ```
   This script packs the `.nupkg` and `scp`s it to the MitWare server's `tools/nupkg/` directories (main + dev). The server's `NuGet.Config` has `tools/nupkg/` registered as a local source.

3. **Bump MitWare's reference** to the local version:
   ```bash
   dotnet add package BrowserApi.SourceGen --version 0.1.0-local.X
   ```
   Since `local-tools` / `BrowserApiLocal` is registered as a NuGet source in MitWare's `NuGet.Config`, the package resolves from there by exact-version match.

4. **Test in MitWare**, iterate, re-pack with a bumped `-local.(X+1)` version as needed.

5. **When satisfied, cut a public release** via Path 1.

### Caveats

- `-local.N` versions do **not** show up on nuget.org. Do not reference them from projects that need to build on clean CI environments unless that CI has access to the same local feed.
- MitWare's production server only has `BrowserApi` packages in `tools/nupkg/` *if* `publish-local.sh` has been run against it — otherwise the server resolves from public nuget.org like everywhere else.
- Normal MitWare deploys do not touch `tools/nupkg/`. Run `publish-local.sh` only when the generator itself has a pending change that must reach the server.

## Related files

- `CHANGELOG.md` — single source of truth for public-release notes.
- `.github/workflows/publish.yml` — the tag-driven public-release pipeline.
- `scripts/publish-local.sh` — local-feed deploy to the MitWare server.
- Each `src/*/*.csproj` — contains `<PackageReleaseNotes>` pointing at the CHANGELOG.
