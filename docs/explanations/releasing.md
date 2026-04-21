# Releasing a new version

This repo publishes **five NuGet packages together** at a single shared version:
`BrowserApi`, `BrowserApi.JSInterop`, `BrowserApi.Blazor`, `BrowserApi.Runtime`, `BrowserApi.SourceGen`.

Release notes live in one file — `CHANGELOG.md` at the repo root. Every user-facing surface (nuget.org pages, IDE NuGet dialogs, GitHub Releases) is driven from that single file. There is no other place to update.

## The flow (what to do on every release)

1. **Move entries under `[Unreleased]` in `CHANGELOG.md` to a new dated version section**, e.g.:
   ```markdown
   ## [Unreleased]

   ## [0.1.0-preview.2] — 2026-04-21

   ### BrowserApi.SourceGen
   - Added …
   - Fixed …
   ```
   The heading must be exactly `## [<version>]` — the workflow matches this pattern to extract the release body.

2. **Commit the changelog change** and push to `main`.

3. **Tag the commit** and push the tag:
   ```bash
   git tag v0.1.0-preview.2
   git push origin v0.1.0-preview.2
   ```
   The tag name must be the version prefixed with `v`. The workflow strips the `v` to compute the package version.

That's it. The rest is automated by `.github/workflows/publish.yml`.

## What the workflow does

Triggered on any tag matching `v*`, it:

1. Extracts the version from the tag (`v0.1.0-preview.2` → `0.1.0-preview.2`).
2. **Fails early** if `CHANGELOG.md` doesn't have a `## [0.1.0-preview.2]` section — no silent publish without notes.
3. Restores, builds, runs the full test suite.
4. Packs all five packages with `-p:Version=<version>`.
5. Pushes all packages to nuget.org (`--skip-duplicate` makes re-runs safe).
6. Creates a GitHub Release tagged `v0.1.0-preview.2` with the CHANGELOG section as the body, and attaches the `.nupkg` files.

## Where release notes show up

| Surface | Source |
|---|---|
| nuget.org package page, "Release Notes" section | Each csproj has `<PackageReleaseNotes>See …/CHANGELOG.md…</PackageReleaseNotes>` — users click through to the file on GitHub. |
| IDE NuGet package dialogs (Visual Studio, Rider) | Same `<PackageReleaseNotes>` text. |
| GitHub Releases page (`/releases`) | The workflow extracts the matching `CHANGELOG.md` section and posts it as the release body. |

Editing `CHANGELOG.md` after the fact updates the nuget.org and IDE link target automatically (they point to the live file on GitHub). Changing a past GitHub Release body requires editing it manually on GitHub — or deleting the release and re-running the workflow.

## Versioning rules

- Follow [SemVer](https://semver.org/). While in preview, use `0.1.0-preview.N` (increment `N` per preview release).
- All five packages release at the same version. The `<Version>` in each csproj is a local-dev placeholder; the workflow overrides it via `-p:Version=`.
- Do **not** create a GitHub Release manually — the workflow handles that. Creating one by hand will result in two releases for the same tag.

## If something fails mid-release

- **Tests fail**: fix on `main`, delete the tag (`git tag -d vX && git push --delete origin vX`), re-tag the new commit.
- **NuGet push fails for one package**: re-run the workflow from the Actions tab. `--skip-duplicate` ensures already-pushed packages are not republished.
- **Wrong notes went out**: edit the CHANGELOG section, then edit the GitHub Release body by hand (or `gh release edit`). The nuget.org link will pick up the CHANGELOG edit automatically.

## Related files

- `CHANGELOG.md` — single source of truth for release notes.
- `.github/workflows/publish.yml` — the release pipeline.
- Each `src/*/*.csproj` — contains `<PackageReleaseNotes>` pointing at the CHANGELOG.
