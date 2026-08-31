<#
.SYNOPSIS
    Single authorized entry point for publishing Ether.DesignSystem.{Foundation,Controls,
    Interactions} to the internal GitHub Packages feed.

.DESCRIPTION
    This script exists so that "push a package without running the full acceptance gate" is not
    a thing anyone can do by hand. It always runs, in order and to completion:

      1. dotnet build (Debug x64, then Release x64) for Ether.DesignSystem.slnx
      2. Every gate in scripts/Gates.psd1 (the single source of truth, R-02) tagged 'ci',
         'local-runtime', or 'local-external' - the union of everything build.yml's
         package-consumers job, Verify-RuntimeGates.ps1, and the out-of-repo consumer check would
         run anywhere, using the fuller variant wherever the same script has both a CI-trimmed
         '-Skip*' form and a full local-runtime form (Verify-ConsumerFixtures.ps1,
         Verify-MsixPackage.ps1). This is the FINAL pre-release check, so nothing is CI-trimmed
         here: Verify-PowerShellCompatibility.ps1 and Verify-GallerySmoke.ps1 run too, and
         Verify-ConsumerFixtures.ps1/Verify-MsixPackage.ps1 always run to completion, not skipped.
         Pass -ListGates to print exactly what this resolves to without running any of it.
      3. git diff --check
      4. The manifest's Verify-ConsumerFixtures.ps1 gate, TWICE in a row (determinism: the same
         GUI runtime pass must produce the same outcome back to back)
      5. The manifest's Verify-MsixPackage.ps1 gate, always after Verify-ConsumerFixtures.ps1
         (it restores from the local feed ConsumerFixtures packs)

    There is no switch to skip, shrink, or "-Force" past any of the above. If you need that,
    you need a different script, and probably a conversation with whoever owns this file.

    Every gate must be run from the version currently on disk - reusing a previous run's
    artifacts/audit-runs evidence to claim a fresh pass is exactly the failure mode this script
    exists to close (this repository has been burned by stale-evidence-looks-like-a-pass four
    times). So this script snapshots artifacts/audit-runs/ before it starts and, after the two
    Verify-ConsumerFixtures.ps1 rounds, asserts that at least two NEW evidence directories exist
    whose timestamps are later than this script's own start time. An evidence directory that
    predates this run does not count, no matter how good it looks.

    Version discipline: NuGet versions are immutable and this repository has already shipped a
    consumer-visible incident from a reused version number silently resolving to a stale cached
    package (see SEMVER.md / docs/consumers/getting-started.md 5th section). This script reads
    the version once (from the Controls project's MSBuild PackageVersion property - the single
    source of truth also used by dotnet pack) and refuses to proceed if that exact version has
    already been recorded as pushed in artifacts/release-evidence/published-versions.json, or if
    a matching git tag already exists. When -Push is used, it additionally asks the configured
    feed whether the version already exists there before pushing anything.

    By default this script only verifies and packs (a full release rehearsal with nothing sent
    anywhere). Pushing to the feed requires the explicit -Push switch, plus feed configuration
    (below), plus a typed confirmation once the packages/version/feed are printed.

    Feed configuration: this repository does not know which GitHub organization or user will own
    the internal package feed - that has not been decided/provided yet. -PackageOwner (or
    $env:ETHER_PUBLISH_PACKAGE_OWNER) supplies it, or pass a full -Feed URL directly if the feed
    is not a standard https://nuget.pkg.github.com/{owner}/index.json address. Neither is
    required to run this script in rehearsal (non -Push) mode - only -Push needs to know where
    packages would go.

.PARAMETER Push
    Actually push the verified, packed packages to the configured feed. Without this switch the
    script only verifies and packs (safe to run repeatedly as a release rehearsal).

.PARAMETER PackageOwner
    The GitHub organization or user that owns the internal GitHub Packages feed. Required (via
    this parameter or $env:ETHER_PUBLISH_PACKAGE_OWNER) when -Push is used and -Feed is not
    given directly. Builds the feed URL as https://nuget.pkg.github.com/{PackageOwner}/index.json.

.PARAMETER Feed
    Full NuGet v3 service index URL to push to, when it is not the standard GitHub Packages
    pattern for -PackageOwner. Required (via this parameter or $env:ETHER_PUBLISH_FEED) when
    -Push is used and -PackageOwner is not given.

.PARAMETER ApiKey
    PAT used to push (needs write:packages, and read:packages to run the pre-push version-exists
    check). Falls back to $env:ETHER_PUBLISH_PAT. This is deliberately never a plaintext default
    - there is no built-in credential.

.PARAMETER ListGates
    Print the gate list derived from scripts/Gates.psd1 (name, script, args, execution order) and
    exit immediately - no version resolution, no dotnet build, no gates actually run. For
    inspecting what a real run would do without paying for it.

.EXAMPLE
    ./scripts/Publish-Internal.ps1
    Full release rehearsal: every gate, pack, no push, nothing needs to be configured.

.EXAMPLE
    $env:ETHER_PUBLISH_PAT = '<a PAT with write:packages>'
    ./scripts/Publish-Internal.ps1 -Push -PackageOwner contoso-internal
    Full gate chain, pack, then (after a typed confirmation) push to
    https://nuget.pkg.github.com/contoso-internal/index.json.
#>
[CmdletBinding()]
param(
    [switch]$Push,
    [string]$PackageOwner,
    [string]$Feed,
    [string]$ApiKey,
    [switch]$ListGates
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# $LASTEXITCODE does not exist as a variable at all until some native (non-PowerShell) command
# has run at least once in this session - under Set-StrictMode, reading it before that throws
# "cannot be retrieved because it has not been set" rather than comparing against $null. The gate
# loop below reads it after every gate script invocation, so define it up front instead of relying
# on the dotnet build calls a few lines down to happen to run first.
$LASTEXITCODE = 0

$scriptStartUtc = [DateTime]::UtcNow
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$artifactsRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot 'artifacts'))
$auditRunsRoot = Join-Path $artifactsRoot 'audit-runs'
$releaseEvidenceRoot = Join-Path $artifactsRoot 'release-evidence'
$ledgerPath = Join-Path $releaseEvidenceRoot 'published-versions.json'
$platform = 'x64'
$platformProperty = "-p:Platform=$platform"

$packageIds = @('Ether.DesignSystem.Foundation', 'Ether.DesignSystem.Controls', 'Ether.DesignSystem.Interactions')
$controlsProject = Join-Path $repoRoot 'src\Ether.DesignSystem.Controls\Ether.DesignSystem.Controls.csproj'

function Write-Section {
    param([Parameter(Mandatory)][string]$Title)
    Write-Host ''
    Write-Host "=== $Title ===" -ForegroundColor Cyan
}

function Invoke-DotNet {
    param([Parameter(Mandatory)][string[]]$Arguments)
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

function Invoke-Gate {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][scriptblock]$Action
    )
    Write-Section $Name
    try {
        & $Action
    }
    catch {
        Write-Host ''
        Write-Host "GATE FAILED: $Name" -ForegroundColor Red
        Write-Host $_.Exception.Message -ForegroundColor Red
        throw "Publish-Internal aborted: gate '$Name' failed. Fix the underlying issue and re-run the full chain from the top - there is no partial-resume or -Force in this script by design."
    }
    Write-Host "PASSED: $Name" -ForegroundColor Green
}

# ---------------------------------------------------------------------------
# Gate list: derived from scripts/Gates.psd1 (single source of truth, R-02) instead of a
# hardcoded, hand-synced list. This is the FINAL pre-release check, so it runs EVERYTHING any
# environment would run - the union of every gate tagged 'ci', 'local-runtime', or
# 'local-external' - using the fuller variant wherever the same script appears in more than one
# environment (e.g. full Verify-ConsumerFixtures.ps1, not the ci form's
# -SkipSolutionBuild/-SkipRuntimeSmoke; full Verify-MsixPackage.ps1, not the ci form's
# -SkipSolutionBuild). This fixes R-03: the previous hardcoded 22-gate list omitted
# Verify-PowerShellCompatibility.ps1 and Verify-GallerySmoke.ps1 entirely and never ran any
# local-runtime gate.
# ---------------------------------------------------------------------------
$gatesManifestPath = Join-Path $repoRoot 'scripts\Gates.psd1'
if (-not (Test-Path -LiteralPath $gatesManifestPath -PathType Leaf)) {
    throw "Gate manifest is missing: $gatesManifestPath"
}
$gatesManifest = Import-PowerShellDataFile -Path $gatesManifestPath
$publishEnvironments = @('ci', 'local-runtime', 'local-external')
# Preference when the same Script is tagged more than one of the environments above: prefer
# whichever runs to completion locally over the CI-trimmed '-Skip*' form.
$environmentRank = @{ 'local-runtime' = 2; 'local-external' = 1; 'ci' = 0 }

function Get-GateEnvironmentRank {
    param([Parameter(Mandatory)]$Gate)
    $best = -1
    foreach ($gateEnv in @($Gate.Environments)) {
        if ($environmentRank.ContainsKey($gateEnv) -and $environmentRank[$gateEnv] -gt $best) {
            $best = $environmentRank[$gateEnv]
        }
    }
    return $best
}

$publishCandidates = @($gatesManifest.Gates | Where-Object {
    $gateEnvs = @($_.Environments)
    @($gateEnvs | Where-Object { $publishEnvironments -contains $_ }).Count -gt 0
})

$bestRankByScript = @{}
foreach ($candidate in $publishCandidates) {
    $rank = Get-GateEnvironmentRank -Gate $candidate
    if (-not $bestRankByScript.ContainsKey($candidate.Script) -or $rank -gt $bestRankByScript[$candidate.Script]) {
        $bestRankByScript[$candidate.Script] = $rank
    }
}

$seenPublishScripts = New-Object System.Collections.Generic.HashSet[string]
$publishGates = New-Object System.Collections.Generic.List[object]
foreach ($candidate in $publishCandidates) {
    $isWinner = (Get-GateEnvironmentRank -Gate $candidate) -eq $bestRankByScript[$candidate.Script]
    if ($isWinner -and -not $seenPublishScripts.Contains($candidate.Script)) {
        [void]$seenPublishScripts.Add($candidate.Script)
        $publishGates.Add($candidate)
    }
}
if ($publishGates.Count -eq 0) {
    throw "Derived an empty gate list from $gatesManifestPath for environments ($($publishEnvironments -join ', ')). Refusing to proceed."
}

# Verify-ConsumerFixtures.ps1 packs Foundation/Controls/Interactions into the local feed that
# Verify-MsixPackage.ps1 restores from, so MsixPackage must always run after it (same dependency
# .github/workflows/build.yml documents for the 'ci' forms of both). ConsumerFixtures also runs
# twice in a row further down (determinism: the same GUI runtime pass must produce the same
# outcome back to back), so both are pulled out of the single-pass loop below and driven
# explicitly, in dependency order, instead.
$consumerFixturesGate = @($publishGates | Where-Object { $_.Script -eq 'Verify-ConsumerFixtures.ps1' } | Select-Object -First 1)
$msixPackageGate = @($publishGates | Where-Object { $_.Script -eq 'Verify-MsixPackage.ps1' } | Select-Object -First 1)
if ($consumerFixturesGate.Count -eq 0) {
    throw "$gatesManifestPath has no Verify-ConsumerFixtures.ps1 entry tagged 'ci', 'local-runtime', or 'local-external'."
}
if ($msixPackageGate.Count -eq 0) {
    throw "$gatesManifestPath has no Verify-MsixPackage.ps1 entry tagged 'ci', 'local-runtime', or 'local-external'."
}
$consumerFixturesGate = $consumerFixturesGate[0]
$msixPackageGate = $msixPackageGate[0]
$singlePassGates = @($publishGates | Where-Object { $_.Script -ne 'Verify-ConsumerFixtures.ps1' -and $_.Script -ne 'Verify-MsixPackage.ps1' })

if ($ListGates) {
    Write-Host "Publish-Internal would run $($publishGates.Count) gate(s), derived from $gatesManifestPath (environments: $($publishEnvironments -join ', ')):"
    $listIndex = 0
    foreach ($gate in $singlePassGates) {
        $listIndex++
        $argsText = '(no args)'
        if (@($gate.Args.Keys).Count -gt 0) {
            $argsText = (@($gate.Args.Keys) | ForEach-Object { "-$_=$($gate.Args[$_])" }) -join ' '
        }
        Write-Host ("  {0,2}. {1,-28} {2,-38} {3}" -f $listIndex, $gate.Name, $gate.Script, $argsText)
    }
    $listIndex++
    Write-Host ("  {0,2}. {1,-28} {2,-38} (runs twice in a row)" -f $listIndex, $consumerFixturesGate.Name, $consumerFixturesGate.Script)
    $listIndex++
    Write-Host ("  {0,2}. {1,-28} {2,-38} (always after ConsumerFixtures)" -f $listIndex, $msixPackageGate.Name, $msixPackageGate.Script)
    Write-Host ''
    Write-Host 'Plus, unconditionally and not from the manifest: dotnet build (Debug, x64), dotnet build (Release, x64), and git diff --check.'
    return
}

# ---------------------------------------------------------------------------
# 0. Version (single source of truth: the Controls project's MSBuild property,
#    the same one `dotnet pack` uses).
# ---------------------------------------------------------------------------
Write-Section 'Resolve package version'
Push-Location $repoRoot
try {
    $version = (& dotnet msbuild $controlsProject '-getProperty:PackageVersion' $platformProperty 2>$null | Select-Object -Last 1).Trim()
}
finally {
    Pop-Location
}
if ([string]::IsNullOrWhiteSpace($version)) {
    throw 'Could not resolve PackageVersion from src/Ether.DesignSystem.Controls/Ether.DesignSystem.Controls.csproj via dotnet msbuild -getProperty. Publish-Internal aborted.'
}
Write-Host "Resolved package version: $version"

# ---------------------------------------------------------------------------
# 1. Version discipline (local ledger + git tag), unconditionally. NuGet
#    versions are immutable - reusing one silently hands consumers a stale
#    package while restore still reports success (this has happened before;
#    see SEMVER.md and docs/consumers/getting-started.md).
# ---------------------------------------------------------------------------
Write-Section 'Version discipline (local)'
New-Item -ItemType Directory -Path $releaseEvidenceRoot -Force | Out-Null

$ledger = @()
if (Test-Path -LiteralPath $ledgerPath) {
    $raw = Get-Content -LiteralPath $ledgerPath -Raw
    if (-not [string]::IsNullOrWhiteSpace($raw)) {
        $ledger = @(ConvertFrom-Json -InputObject $raw -Depth 10)
    }
}

$reusedInLedger = @($ledger | Where-Object { $_.version -eq $version })
if ($reusedInLedger.Count -gt 0) {
    $pushedAt = ($reusedInLedger | Select-Object -First 1).pushedAtUtc
    throw "Version '$version' is already recorded as pushed in $ledgerPath (pushed $pushedAt). Bump the version (EtherDesignSystemPreviewVersion in Directory.Build.props) before publishing again. Publish-Internal refuses to proceed - NuGet versions are immutable and reusing one silently gives consumers a stale package."
}

$matchingTags = @(& git -C $repoRoot tag -l "*$version*")
if ($matchingTags.Count -gt 0) {
    throw "A git tag matching version '$version' already exists ($($matchingTags -join ', ')). Publish-Internal refuses to proceed against a version that already looks tagged/released. Bump the version and re-run."
}
Write-Host "No local record (ledger or git tag) of '$version' having been published before."

# ---------------------------------------------------------------------------
# 2. Snapshot existing audit-run evidence directories so the freshness check
#    later can tell "produced by this run" from "already sitting there".
# ---------------------------------------------------------------------------
$auditRunsBefore = @()
if (Test-Path -LiteralPath $auditRunsRoot) {
    $auditRunsBefore = @(Get-ChildItem -LiteralPath $auditRunsRoot -Directory | ForEach-Object { $_.Name })
}

# ---------------------------------------------------------------------------
# 3. Full gate chain. Every step throws on failure (Set-StrictMode +
#    $ErrorActionPreference='Stop' inside each called script, or Invoke-DotNet
#    above), and Invoke-Gate turns that into a clearly attributed abort.
# ---------------------------------------------------------------------------
Push-Location $repoRoot
try {
    Invoke-Gate 'dotnet build (Debug, x64)' {
        Invoke-DotNet @('build', 'Ether.DesignSystem.slnx', '-c', 'Debug', $platformProperty)
    }

    Invoke-Gate 'dotnet build (Release, x64)' {
        Invoke-DotNet @('build', 'Ether.DesignSystem.slnx', '-c', 'Release', $platformProperty)
    }

    # Every manifest-derived publish gate except Verify-ConsumerFixtures.ps1 and
    # Verify-MsixPackage.ps1 (those two are driven explicitly below, in dependency order - see
    # the comment above where $singlePassGates was computed). Invoked via hashtable splat only -
    # never array splat (R-04; see scripts/Gates.psd1's header comment).
    foreach ($gate in $singlePassGates) {
        Invoke-Gate $gate.Name {
            $scriptPath = Join-Path $repoRoot ('scripts\' + $gate.Script)
            $gateArgs = @{}
            foreach ($key in $gate.Args.Keys) { $gateArgs[$key] = $gate.Args[$key] }
            & $scriptPath @gateArgs
            if ($LASTEXITCODE -ne 0 -and $null -ne $LASTEXITCODE) {
                throw "$($gate.Script) exited with code $LASTEXITCODE."
            }
        }
    }

    Invoke-Gate 'git diff --check' {
        & git -C $repoRoot diff --check
        if ($LASTEXITCODE -ne 0) {
            throw 'git diff --check failed (whitespace errors in the working tree).'
        }
    }

    $consumerFixturesArgs = @{}
    foreach ($key in $consumerFixturesGate.Args.Keys) { $consumerFixturesArgs[$key] = $consumerFixturesGate.Args[$key] }
    $consumerFixturesPath = Join-Path $repoRoot ('scripts\' + $consumerFixturesGate.Script)

    Invoke-Gate "$($consumerFixturesGate.Name) (round 1 of 2)" {
        & $consumerFixturesPath @consumerFixturesArgs
    }

    Invoke-Gate "$($consumerFixturesGate.Name) (round 2 of 2)" {
        & $consumerFixturesPath @consumerFixturesArgs
    }

    $msixPackageArgs = @{}
    foreach ($key in $msixPackageGate.Args.Keys) { $msixPackageArgs[$key] = $msixPackageGate.Args[$key] }

    Invoke-Gate $msixPackageGate.Name {
        & (Join-Path $repoRoot ('scripts\' + $msixPackageGate.Script)) @msixPackageArgs
    }
}
finally {
    Pop-Location
}

# ---------------------------------------------------------------------------
# 4. Evidence freshness. The two Verify-ConsumerFixtures.ps1 rounds above must
#    each have written a NEW artifacts/audit-runs/consumer-runtime-evidence-*
#    directory with a timestamp after this script started. A directory left
#    over from an earlier run does not satisfy this, even if its contents
#    claim success.
# ---------------------------------------------------------------------------
Write-Section 'Evidence freshness'
if (-not (Test-Path -LiteralPath $auditRunsRoot)) {
    throw "Publish-Internal aborted: $auditRunsRoot does not exist after running Verify-ConsumerFixtures.ps1 twice. Expected fresh evidence directories."
}
$auditRunsAfter = @(Get-ChildItem -LiteralPath $auditRunsRoot -Directory)
$newAuditRuns = @($auditRunsAfter | Where-Object { $_.Name -notin $auditRunsBefore })
$freshAuditRuns = @($newAuditRuns | Where-Object { $_.CreationTimeUtc -ge $scriptStartUtc })

if ($freshAuditRuns.Count -lt 2) {
    $seen = if ($newAuditRuns.Count -eq 0) { '(none)' } else { ($newAuditRuns | ForEach-Object { "$($_.Name) [created $($_.CreationTimeUtc.ToString('o'))]" }) -join '; ' }
    throw "Publish-Internal aborted: expected at least 2 new artifacts/audit-runs/ evidence directories created at or after this run's start ($($scriptStartUtc.ToString('o'))), found $($freshAuditRuns.Count). New directories seen: $seen. This is the stale-evidence guard - do not investigate by re-running only the evidence check; re-run the whole script."
}
Write-Host "Fresh evidence confirmed: $($freshAuditRuns.Count) new audit-run director$(if ($freshAuditRuns.Count -eq 1) { 'y' } else { 'ies' }) created after $($scriptStartUtc.ToString('o'))."
foreach ($dir in $freshAuditRuns) {
    Write-Host "  - $($dir.FullName)"
}

# ---------------------------------------------------------------------------
# 5. Pack. Always happens (rehearsal must produce real packages too).
#    Pack-PreviewPackages.ps1 has no push capability (R-03) - it only packs.
#    Pushing, when -Push is given, happens explicitly in step 7 below, via this
#    script's own `dotnet nuget push`, after the typed confirmation.
# ---------------------------------------------------------------------------
Write-Section 'Pack Release packages'
$versionEvidenceRoot = Join-Path $releaseEvidenceRoot $version
$packOutput = Join-Path $versionEvidenceRoot 'packages'
New-Item -ItemType Directory -Path $packOutput -Force | Out-Null
& (Join-Path $repoRoot 'scripts\Pack-PreviewPackages.ps1') -Configuration Release -OutputDirectory $packOutput

$packedPackages = @(
    Get-ChildItem -LiteralPath $packOutput -Filter '*.nupkg' |
        Where-Object { $_.Name -notlike '*.symbols.nupkg' }
)
if ($packedPackages.Count -lt $packageIds.Count) {
    throw "Expected $($packageIds.Count) packed packages under $packOutput, found $($packedPackages.Count)."
}
foreach ($id in $packageIds) {
    $expectedName = "$id.$version.nupkg"
    if (-not ($packedPackages | Where-Object { $_.Name -eq $expectedName })) {
        throw "Expected packed package '$expectedName' under $packOutput but did not find it."
    }
}

# ---------------------------------------------------------------------------
# 6. Evidence bundle. Written every run (rehearsal or real push) so a
#    rehearsal leaves the same kind of trail a real publish does.
# ---------------------------------------------------------------------------
$summary = [ordered]@{
    version           = $version
    scriptStartUtc    = $scriptStartUtc.ToString('o')
    completedUtc      = [DateTime]::UtcNow.ToString('o')
    machine           = $env:COMPUTERNAME
    user              = $env:USERNAME
    gitCommit         = (& git -C $repoRoot rev-parse HEAD).Trim()
    gitBranch         = (& git -C $repoRoot rev-parse --abbrev-ref HEAD).Trim()
    pushed            = [bool]$Push
    packages          = @($packedPackages | ForEach-Object { $_.Name })
    freshEvidenceDirs = @($freshAuditRuns | ForEach-Object { $_.FullName })
}
$summaryPath = Join-Path $versionEvidenceRoot 'publish-summary.json'
($summary | ConvertTo-Json -Depth 10) | Set-Content -LiteralPath $summaryPath -Encoding utf8
Write-Host "Evidence bundle written: $versionEvidenceRoot"

if (-not $Push) {
    Write-Host ''
    Write-Host "Rehearsal complete. All gates passed, packages built at:" -ForegroundColor Green
    foreach ($p in $packedPackages) { Write-Host "  - $($p.FullName)" }
    Write-Host "Nothing was pushed. Re-run with -Push (and feed configuration) to publish for real."
    return
}

# ---------------------------------------------------------------------------
# 7. Push path. Everything above already ran unconditionally; this is purely
#    additive on top of a rehearsal that already succeeded.
# ---------------------------------------------------------------------------
Write-Section 'Resolve feed configuration'
if ([string]::IsNullOrWhiteSpace($Feed)) {
    if ([string]::IsNullOrWhiteSpace($PackageOwner)) {
        $PackageOwner = $env:ETHER_PUBLISH_PACKAGE_OWNER
    }
    if ([string]::IsNullOrWhiteSpace($PackageOwner)) {
        throw @'
Publish-Internal aborted: -Push was given but no feed is configured, and this repository does
not have a known GitHub Packages owner/feed baked in (the org or user that will host the
internal feed has not been decided/provided yet). Supply one of:
  - -PackageOwner <github-org-or-user>   (feed becomes https://nuget.pkg.github.com/<owner>/index.json)
  - -Feed <full nuget v3 service index URL>
  - $env:ETHER_PUBLISH_PACKAGE_OWNER or $env:ETHER_PUBLISH_FEED set beforehand
Do not hardcode a guessed address into this script - ask whoever owns the GitHub organization
for the internal feed's actual owner/URL first.
'@
    }
    $Feed = "https://nuget.pkg.github.com/$PackageOwner/index.json"
}

if ([string]::IsNullOrWhiteSpace($ApiKey)) {
    $ApiKey = $env:ETHER_PUBLISH_PAT
}
if ([string]::IsNullOrWhiteSpace($ApiKey)) {
    throw 'Publish-Internal aborted: -Push was given but no PAT is available (-ApiKey or $env:ETHER_PUBLISH_PAT). The PAT needs write:packages (and read:packages for the pre-push version check) - this is a different, more privileged token than the read:packages-only PAT documented for consumers in docs/consumers/getting-started.md; do not reuse a consumer token here.'
}
Write-Host "Feed: $Feed"

Write-Section 'Remote version-exists check'
$remoteCheckOk = $false
try {
    $orgMatch = if ($Feed -match 'nuget\.pkg\.github\.com/([^/]+)/') { $Matches[1] } elseif ($PackageOwner) { $PackageOwner } else { $null }
    if ($null -eq $orgMatch) {
        Write-Warning 'Could not derive a GitHub owner from -Feed to run the REST version-exists probe; skipping straight to the typed confirmation below. The local ledger/git-tag check above is still in effect.'
    }
    else {
        $headers = @{ Authorization = "Bearer $ApiKey"; Accept = 'application/vnd.github+json' }
        foreach ($ownerKind in @('orgs', 'users')) {
            foreach ($id in $packageIds) {
                $uri = "https://api.github.com/$ownerKind/$orgMatch/packages/nuget/$id/versions"
                try {
                    $versions = Invoke-RestMethod -Uri $uri -Headers $headers -Method Get -ErrorAction Stop
                    $remoteCheckOk = $true
                    $existing = @($versions | Where-Object { $_.name -eq $version })
                    if ($existing.Count -gt 0) {
                        throw "Version '$version' of package '$id' already exists on $Feed (queried via $uri). Publish-Internal refuses to push a version that is already published - bump the version and re-run."
                    }
                }
                catch [Microsoft.PowerShell.Commands.HttpResponseException] {
                    # 404 here means either the owner-kind guess (orgs vs users) was wrong, or the
                    # package has no versions yet (first-ever publish) - neither is a hard failure.
                    # Any other status is left to bubble up (e.g. 401/403 = bad/insufficient PAT).
                    $status = $_.Exception.Response.StatusCode.value__
                    if ($status -ne 404) { throw }
                }
            }
        }
    }
}
catch {
    if ($_.Exception.Message -like "Version '*already exists on*") { throw }
    Write-Warning "Remote version-exists probe against GitHub Packages did not complete cleanly ($($_.Exception.Message)). This can legitimately happen (package owner is a user vs org, first-ever publish, or a PAT scoped only for push). Falling through to the typed confirmation - you are responsible for confirming the version is genuinely new on $Feed before typing it."
}
if ($remoteCheckOk) {
    Write-Host "Remote check: no existing '$version' found for any of the three packages on $Feed."
}

Write-Section 'Confirm push'
Write-Host "About to push to: $Feed"
Write-Host "Version: $version"
Write-Host 'Packages:'
foreach ($p in $packedPackages) { Write-Host "  - $($p.Name)" }
Write-Host ''
$typed = Read-Host "Type the version '$version' to confirm this push (anything else aborts)"
if ($typed -ne $version) {
    throw "Publish-Internal aborted: confirmation did not match '$version'. Nothing was pushed."
}

Write-Section 'Push'
foreach ($p in $packedPackages) {
    Invoke-DotNet @('nuget', 'push', $p.FullName, '--source', $Feed, '--api-key', $ApiKey)
    Write-Host "Pushed $($p.Name) to $Feed"
}

$ledgerEntry = [ordered]@{
    version      = $version
    feed         = $Feed
    packages     = @($packedPackages | ForEach-Object { $_.Name })
    pushedAtUtc  = [DateTime]::UtcNow.ToString('o')
    machine      = $env:COMPUTERNAME
    user         = $env:USERNAME
    gitCommit    = (& git -C $repoRoot rev-parse HEAD).Trim()
    evidenceRoot = $versionEvidenceRoot
}
$ledger = @($ledger) + $ledgerEntry
($ledger | ConvertTo-Json -Depth 10) | Set-Content -LiteralPath $ledgerPath -Encoding utf8

$summary.pushed = $true
$summary.pushedAtUtc = $ledgerEntry.pushedAtUtc
$summary.feed = $Feed
($summary | ConvertTo-Json -Depth 10) | Set-Content -LiteralPath $summaryPath -Encoding utf8

Write-Host ''
Write-Host "Published $version to $Feed. Evidence: $versionEvidenceRoot" -ForegroundColor Green
