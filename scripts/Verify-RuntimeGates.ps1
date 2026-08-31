[CmdletBinding()]
param(
    [switch]$SkipSolutionBuild
)

# Local / self-hosted desktop owner for Gallery smoke, L3 consumer runtime
# markers, and unsigned MSIX produce. Hosted .github/workflows/build.yml must
# not invoke this script (it also runs Verify-MsixPackage.ps1 itself, in the
# package-consumers job, since that check needs no GUI or desktop session).
#
# Gate set is derived from scripts/Gates.psd1 (single source of truth, R-02):
# every entry tagged environment 'local-runtime' runs here, in the order it is
# declared in the manifest, invoked via hashtable splat (never array splat -
# see Gates.psd1's header comment / R-04). This replaced a hardcoded 3-script
# list that had no automated caller of its own and had already drifted from
# .github/workflows/build.yml and scripts/Publish-Internal.ps1.
#
# arm64 remains intentionally not chained here - see Gates.psd1's
# UnmanifestedScripts entry for Verify-Arm64Packages.ps1 for why it stays on
# disk unused rather than being deleted or re-added to this chain.

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptStartUtc = [DateTime]::UtcNow
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$scriptsDir = Join-Path $repoRoot 'scripts'
$manifestPath = Join-Path $scriptsDir 'Gates.psd1'
$auditRunsRoot = Join-Path $repoRoot 'artifacts\audit-runs'

if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    throw "Gate manifest is missing: $manifestPath"
}

$manifest = Import-PowerShellDataFile -Path $manifestPath
$gates = @($manifest.Gates | Where-Object { @($_.Environments) -contains 'local-runtime' })
if ($gates.Count -eq 0) {
    throw "No gates tagged 'local-runtime' found in $manifestPath. Expected at least Verify-ConsumerFixtures.ps1, Verify-GallerySmoke.ps1, and Verify-MsixPackage.ps1."
}

foreach ($gate in $gates) {
    $gatePath = Join-Path $scriptsDir $gate.Script
    if (-not (Test-Path -LiteralPath $gatePath -PathType Leaf)) {
        throw "Required runtime verifier is missing: $gatePath"
    }
}

# Verify-ConsumerFixtures.ps1 and Verify-MsixPackage.ps1 each accept their own
# -SkipSolutionBuild switch; Verify-GallerySmoke.ps1 does not. This script's own
# -SkipSolutionBuild is passed through only to gates whose script supports it.
$skipSolutionBuildCapableScripts = @('Verify-ConsumerFixtures.ps1', 'Verify-MsixPackage.ps1')

# R-12: Verify-SilentPropertyCoverage.ps1 CONSUMES the runtime evidence that
# Verify-ConsumerFixtures.ps1 GENERATES (artifacts/audit-runs/consumer-runtime-evidence-*/
# runtime-result.json). Gates.psd1 already declares SilentPropertyCoverage after
# ConsumerFixtures-runtime, so running gates in declared order is correct here - but capture and
# pass the exact evidence directory ConsumerFixtures just produced anyway (via
# Verify-SilentPropertyCoverage.ps1's -EvidenceDir/-MinCreationTimeUtc), so correctness does not
# silently depend on manifest ordering alone. See Gates.psd1's comment on the SilentPropertyCoverage
# entry and docs/plans/2026-08-31-release-blockers-spec.md R-12.
$capturedEvidenceDir = $null

Push-Location $repoRoot
try {
    foreach ($gate in $gates) {
        $callArgs = @{}
        foreach ($key in $gate.Args.Keys) { $callArgs[$key] = $gate.Args[$key] }
        if ($SkipSolutionBuild -and ($skipSolutionBuildCapableScripts -contains $gate.Script)) {
            $callArgs['SkipSolutionBuild'] = $true
        }

        if ($gate.Script -eq 'Verify-ConsumerFixtures.ps1') {
            $evidenceDirsBefore = @()
            if (Test-Path -LiteralPath $auditRunsRoot) {
                $evidenceDirsBefore = @(Get-ChildItem -LiteralPath $auditRunsRoot -Directory -Filter 'consumer-runtime-evidence-*' | ForEach-Object { $_.Name })
            }
            & (Join-Path $scriptsDir $gate.Script) @callArgs
            if (Test-Path -LiteralPath $auditRunsRoot) {
                $newEvidenceDirs = @(
                    Get-ChildItem -LiteralPath $auditRunsRoot -Directory -Filter 'consumer-runtime-evidence-*' |
                        Where-Object { $evidenceDirsBefore -notcontains $_.Name } |
                        Sort-Object Name -Descending
                )
                if ($newEvidenceDirs.Count -gt 0) {
                    $capturedEvidenceDir = $newEvidenceDirs[0].FullName
                }
            }
            continue
        }

        if ($gate.Script -eq 'Verify-SilentPropertyCoverage.ps1' -and $capturedEvidenceDir) {
            $callArgs['EvidenceDir'] = $capturedEvidenceDir
            $callArgs['MinCreationTimeUtc'] = $scriptStartUtc.ToString('o')
        }

        & (Join-Path $scriptsDir $gate.Script) @callArgs
    }
    Write-Host 'Runtime gates passed: consumer fixture markers, Gallery smoke Light/Dark, and unsigned MSIX produce.'
}
finally {
    Pop-Location
}
