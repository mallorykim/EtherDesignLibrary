# Announcing the package to consuming teams

The playbook for telling developers they can start using the Ether Design System packages — what to
send, what **not** to send, and where each step happens. This is the step **after** publishing.

## Where this fits (end-to-end workflow)

| Step | Who | Where | Done when |
|------|-----|-------|-----------|
| 1. **Publish** the packages to the company feed | an agent / you | a company Windows machine | the 3 packages show `0.1.0-preview.7` under the owner's GitHub **Packages** (see [`../../RELEASE-CHECKLIST.md`](../../RELEASE-CHECKLIST.md) §4) |
| 2. **Announce** to developers | **you (a human)** | your email client — no build machine needed | the email below is sent |
| 3. **Onboard** | each developer | their own dev machine | they restore + build against the feed, following the guide |

You do not "run" the announcement on a machine — it is an email you send once step 1 has succeeded.

## What the email must contain (it's a NuGet feed — send access + instructions, not files)

1. **Feed URL** — `https://nuget.pkg.github.com/<owner>/index.json` (the company org/user that owns the feed).
2. **Auth instructions (the #1 blocker)** — GitHub Packages requires authentication even for internal
   packages. Each developer creates **their own** classic PAT with **`read:packages`** and authorizes
   it for SSO if the org requires it. Never share one PAT.
3. **The `nuget.config`** (feed + credential *references*, not the token itself).
4. **Package names + version** — `Ether.DesignSystem.Controls`, `.Foundation`, `.Interactions` at
   `0.1.0-preview.7` (`Foundation` comes transitively via `Controls`; `Interactions` only if they use
   the telemetry adapter).
5. **Minimal setup** — the three `dotnet add package` lines, the `App.xaml` resource-dictionary merge,
   and the `xmlns:ether` namespace.
6. **Scope & caveats** — preview (API may still change), **x64 only**, **internal only** (do not leak
   the feed or PAT externally), and the known-red limitations (see the release notes).
7. **Docs link + feedback channel.**

## What NOT to send

- ❌ The `.nupkg` / `.dll` files — developers pull from the feed (sending files bypasses versioning).
- ❌ Any PAT (yours or a shared one) — each developer makes their own `read:packages` token.
- ❌ A source zip — they consume the package, not the source.

## Before you send

- [ ] Step 1 is done and `0.1.0-preview.7` is live on the company feed.
- [ ] You know the **feed owner** (`<owner>`) to put in the email.
- [ ] Developers can reach the **docs**: either they have access to this repo (link them to
  `design library handoff/`), or you attach/host that folder for them.

## Email template (fill in `<owner>` and the two links)

> **Subject:** Ether Design System `0.1.0-preview.7` is available (internal WinUI 3 package)
>
> Hi team,
>
> The Ether Design System is now published to our internal GitHub Packages feed — you can start using
> it in WinUI 3 (net8.0-windows, **x64**) apps. It's a **preview** (`0.1.0-preview.7`), so the API may
> still change; please pin the exact version.
>
> **1. Get feed access (one-time).** Create a GitHub classic PAT with the **`read:packages`** scope
> (Settings → Developer settings → Personal access tokens → Tokens (classic)); if prompted, authorize
> it for our org's SSO. Then set it in your shell session:
> ```powershell
> $env:GITHUB_PACKAGES_USERNAME = '<your GitHub username>'
> $env:GITHUB_PACKAGES_PAT = '<your read:packages PAT>'
> ```
> Do not commit the token.
>
> **2. Add `nuget.config`** at your solution root (`<owner>` = our feed owner):
> ```xml
> <?xml version="1.0" encoding="utf-8"?>
> <configuration>
>   <packageSources>
>     <clear />
>     <add key="github-ether" value="https://nuget.pkg.github.com/<owner>/index.json" protocolVersion="3" />
>     <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
>   </packageSources>
>   <packageSourceCredentials>
>     <github-ether>
>       <add key="Username" value="%GITHUB_PACKAGES_USERNAME%" />
>       <add key="ClearTextPassword" value="%GITHUB_PACKAGES_PAT%" />
>     </github-ether>
>   </packageSourceCredentials>
> </configuration>
> ```
>
> **3. Add the packages:**
> ```powershell
> dotnet add package Ether.DesignSystem.Foundation --version 0.1.0-preview.7
> dotnet add package Ether.DesignSystem.Controls --version 0.1.0-preview.7
> dotnet add package Ether.DesignSystem.Interactions --version 0.1.0-preview.7   # optional (telemetry adapter)
> ```
>
> **4. Merge the design-system dictionary** in `App.xaml`, then use the `ether:` namespace:
> ```xml
> <ResourceDictionary Source="ms-appx:///Ether.DesignSystem.Controls/Themes/DesignSystem.xaml" />
> ...
> <ether:EtherButton Content="Save" Style="{StaticResource EtherButtonPrimary}"
>                    xmlns:ether="using:Ether.DesignSystem.Controls"/>
> ```
>
> **Docs:** start at `design library handoff/README.md` → full setup in `getting-started.md` →
> per-control usage in `components/`. **<link to the repo folder or attach it>**
>
> **Notes:** x64 only; internal only (don't share the feed/PAT outside the company); a few things are
> not yet verified (MSIX install/runtime, non-x64) — see the release notes. Questions / bugs →
> **<your channel, e.g. #ether-design-system>**.
>
> Thanks!

## Pre-empt the common blockers (so you get fewer replies)

- **`error NU1301` / 401 / "unable to load the service index"** on restore → the PAT/`nuget.config`
  isn't set, the PAT lacks `read:packages`, or SSO isn't authorized. This is by far the most common.
- **Package "not found"** but auth is fine → they didn't `clear` other sources or the version string is
  wrong; pin `0.1.0-preview.7` exactly.
- **Build/runtime oddities** → confirm the app is built **x64** and `DesignSystem.xaml` is merged.
