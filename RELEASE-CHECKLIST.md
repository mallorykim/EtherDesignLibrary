# Release checklist — Ether Design System `0.1.0-preview.7`

A record of what is confirmed, what is not, and exactly how the **next agent** publishes this preview
to a **company-owned GitHub Packages feed**. Publishing is done from a company machine with a company
GitHub account — not from the agent/machine that prepared this release.

- **Version:** `0.1.0-preview.7` (`Directory.Build.props` → `EtherDesignSystemPreviewVersion`)
- **Prepared on branch:** `codex/refine-components`, **merged to `main`** (`93ded04`)
- **Source repo (prepared here):** `github.com/yiqizhong/ether-lib`
- **Packages (3):** `Ether.DesignSystem.Foundation`, `Ether.DesignSystem.Controls`,
  `Ether.DesignSystem.Interactions` — all at `0.1.0-preview.7`
- **Publish tool:** `scripts/Publish-Internal.ps1` (the only authorized release entry point)

---

## 1. Items to confirm before publishing

| # | Item | Who | Status |
|---|------|-----|--------|
| 1 | Version is `0.1.0-preview.7` everywhere (props + test csproj), and this exact version has **never been published** before (NuGet versions are immutable — never re-pack a published version) | prep | ✅ |
| 2 | Full `Publish-Internal.ps1` **rehearsal** (no `-Push`) is green: build Debug+Release, all `Gates.psd1` gates, `ConsumerFixtures` ×2 (determinism), `SilentPropertyCoverage`, `MsixPackage`, `GallerySmoke`, `ExternalConsumer`; 3 `.nupkg` produced | prep | ✅ |
| 3 | Hosted CI green on the pushed branch (build + static gates) | prep | ✅ |
| 4 | Consumer docs complete + consistent (`design library handoff/`), doc-consistency gate green | prep | ✅ |
| 5 | No `git tag` for `0.1.0-preview.7` exists yet (version-discipline check will pass) | prep | ✅ |
| 6 | **PackageOwner decided** — the company GitHub org/user that owns the private feed (feed becomes `https://nuget.pkg.github.com/<owner>/index.json`) | **you** | ⬜ |
| 7 | **Company PAT** created with `write:packages` **and** `read:packages` (and SSO-authorized if the org requires it). This is more privileged than the read-only consumer PAT — do **not** reuse a consumer token | **you** | ⬜ |
| 8 | **Company machine** has: this repo at HEAD `4e3f025` **or later** (so it includes the moved docs + the gate-path fix); .NET 8 SDK + WinApp SDK **x64** build toolchain; and — for the canonical path — a **real interactive Windows desktop** (the `ConsumerFixtures` runtime gates fail headless) | **you / next agent** | ⬜ |
| 9 | **Merge `codex/refine-components` → `main`** so the release tag sits on `main` | prep | ✅ (fast-forwarded to `93ded04`) |
| 10 | **Package ↔ repo linkage** confirmed for the company owner: GitHub Packages associates a NuGet package with a repo via the package's `RepositoryUrl` (currently `github.com/yiqizhong/ether-lib`). Confirm the company org accepts the push, or point the source/`RepositoryUrl` at a company repo, per your org's GitHub Packages policy | **you** | ⬜ |
| 11 | **The actual push** completed and the 3 packages are visible under the owner's **Packages** | next agent + you | ⬜ |

---

## 2. Already checked ✅ (verified on the prep machine, current product code)

- **Version + version discipline** — `0.1.0-preview.7`; no prior tag; NuGet-immutability rule understood.
- **Full release rehearsal green** — `scripts/Publish-Internal.ps1` (no `-Push`) passed the entire gate
  chain and produced the 3 `.nupkg` at `artifacts/release-evidence/0.1.0-preview.7/packages/`. (The
  ConsumerFixtures round-2 DPI determinism flake was fixed; the `SilentPropertyCoverage` accounting was
  reconciled.)
- **Hosted CI green** on `codex/refine-components` (build + static gates; CI does **not** launch WinUI,
  so runtime gates are covered by the local rehearsal above, not CI).
- **Consumer documentation** — `design library handoff/getting-started.md` + 17 per-component guides +
  index; cross-checked over five independent Codex `gpt-5.6-sol` audits + a self-review; every link
  resolves; `Verify-UnsupportedProperties` doc-consistency gate green (§4 boundary table matches
  `scripts/UnsupportedProperties.psd1` exactly).
- **Release notes** — `docs/releases/0.1.0-preview.7.md`.

> Note: the last **full** rehearsal predates the doc move + audit edits, but those changed only docs
> and the test harness — **no product/library code changed since**. The one gate the doc move touched
> (`Verify-UnsupportedProperties`, new path) was re-run green. The `-Push` step below **re-runs the
> entire chain**, so it is the authoritative final verification before anything is published.

---

## 3. Not yet checked ⬜ (to do on the company side)

- Items **6–11** in the table above (PackageOwner, company PAT, company-machine capability, merge-to-main
  decision, package↔repo linkage, and the push itself).
- **Post-publish smoke** (recommended): from a throwaway consumer project authenticated to the company
  feed, `dotnet add package Ether.DesignSystem.Controls --version 0.1.0-preview.7`, restore, build x64,
  confirm it resolves from the company feed.

### Known-red — NOT release blockers (documented in the release notes)

- Accessibility Insights, Appium / out-of-process UIA.
- Hosted CI does not launch WinUI (`build.yml` keeps `-SkipRuntimeSmoke`).
- MSIX **install/runtime** (only unsigned MSIX *produce* is verified; no `Add-AppxPackage`).
- Any architecture other than `x64`.

---

## 4. How to publish — instructions for the publishing agent

> **Publishing is irreversible and outward-facing.** A published NuGet version can never be replaced
> (only unlisted). Do not push until items 6–10 are confirmed. The person doing the publish keeps the
> PAT — the agent must never be given, log, or store the PAT.

### Canonical path (recommended): `Publish-Internal.ps1 -Push`

This re-runs the full gate chain on the company machine and then pushes — self-contained from the repo.
Requirements: the toolchain + interactive desktop in item 8, and ~40 minutes.

1. **Get the source** on the company machine at the release commit (item 8):
   ```bash
   git clone https://github.com/yiqizhong/ether-lib.git
   cd ether-lib
   git checkout main   # release is merged to main (item 9)
   ```
2. **Provide the feed owner + PAT in the current PowerShell session** (the human types/pastes the PAT;
   it is never written to a file or committed):
   ```powershell
   $env:ETHER_PUBLISH_PAT = '<company PAT with write:packages + read:packages>'
   ```
   Fill `<owner>` with the **company GitHub org or user** that owns the feed (item 6).
3. **Run the publish** (from the repo root):
   ```powershell
   ./scripts/Publish-Internal.ps1 -Push -PackageOwner <owner>
   ```
   - `-PackageOwner <owner>` makes the feed `https://nuget.pkg.github.com/<owner>/index.json`.
     (Alternatively pass `-Feed <full v3 index URL>` if the feed is non-standard.)
   - The script: checks the version isn't already tagged/published, re-runs every gate + builds +
     packs, queries the feed to confirm `0.1.0-preview.7` doesn't already exist, then prompts:
     **`Type the version '0.1.0-preview.7' to confirm this push`**. A human must type
     `0.1.0-preview.7` exactly (anything else aborts) — this is a deliberate safety gate, so this step
     is **interactive**; an autonomous/headless run cannot get past it.
   - On confirmation the script `dotnet nuget push`es all three `.nupkg` to the feed.
4. **Verify** (item 11): on GitHub, open the owner's **Packages** (`github.com/<owner>` → *Packages*,
   or the org's packages page) and confirm `Ether.DesignSystem.Foundation`, `.Controls`, and
   `.Interactions` each show version `0.1.0-preview.7`. Then run the post-publish consume smoke in §3.

### Fallback: direct `dotnet nuget push` of the already-built packages

Use only if the company machine cannot run the full WinUI toolchain. This **skips re-verification** —
it just publishes the `.nupkg` that the rehearsal already produced. Those files live in
`artifacts/release-evidence/0.1.0-preview.7/packages/` on the prep machine and are **git-ignored**, so
they must be copied to the company machine (or rebuilt there with `dotnet pack -c Release`).

```powershell
$env:ETHER_PUBLISH_PAT = '<company PAT with write:packages>'
dotnet nuget push "Ether.DesignSystem.Foundation.0.1.0-preview.7.nupkg"   --source "https://nuget.pkg.github.com/<owner>/index.json" --api-key $env:ETHER_PUBLISH_PAT
dotnet nuget push "Ether.DesignSystem.Controls.0.1.0-preview.7.nupkg"     --source "https://nuget.pkg.github.com/<owner>/index.json" --api-key $env:ETHER_PUBLISH_PAT
dotnet nuget push "Ether.DesignSystem.Interactions.0.1.0-preview.7.nupkg" --source "https://nuget.pkg.github.com/<owner>/index.json" --api-key $env:ETHER_PUBLISH_PAT
```
(You may first need `dotnet nuget add source "https://nuget.pkg.github.com/<owner>/index.json" --name company-ether --username <user> --password $env:ETHER_PUBLISH_PAT --store-password-in-clear-text`.)

### What to fill in (both paths)

| Placeholder | Value |
|-------------|-------|
| `<owner>` | the company GitHub **org or user** that owns the private feed (item 6) |
| `$env:ETHER_PUBLISH_PAT` | the company PAT with `write:packages` (+ `read:packages` for the canonical path) — entered by the human at publish time, never committed |
| version to type at the confirm prompt | `0.1.0-preview.7` |

### After a successful publish

- Consumers install via `nuget.pkg.github.com/<owner>/index.json` (a `read:packages` PAT) — see
  `design library handoff/getting-started.md` §1.
- The next code change ships as `0.1.0-preview.8` (bump `EtherDesignSystemPreviewVersion`); never
  re-pack `preview.7`.
