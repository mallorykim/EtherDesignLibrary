[CmdletBinding()]
param(
    [switch]$SkipSolutionBuild
)

# Local / self-hosted desktop owner for Gallery smoke, L3 consumer runtime
# markers, and unsigned MSIX produce. Hosted .github/workflows/build.yml must
# not invoke this script (it also runs Verify-MsixPackage.ps1 itself, in the
# package-consumers job, since that check needs no GUI or desktop session).
#
# arm64 is intentionally not chained here. Directory.Build.props declares only
# x64 (see the comment there); Verify-Arm64Packages.ps1 packs/compiles for
# arm64 but was never wired into any CI or release gate, so it verified a
# support range nobody was promising. It is left in scripts/ (not deleted) and
# marked disabled at its own top; re-enable both the Platforms/RuntimeIdentifiers
# declaration and this script together if arm64 consumer demand returns.

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$consumerFixtures = Join-Path $repoRoot 'scripts\Verify-ConsumerFixtures.ps1'
$gallerySmoke = Join-Path $repoRoot 'scripts\Verify-GallerySmoke.ps1'
$msixPackage = Join-Path $repoRoot 'scripts\Verify-MsixPackage.ps1'

foreach ($path in @($consumerFixtures, $gallerySmoke, $msixPackage)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required runtime verifier is missing: $path"
    }
}

$consumerArgs = @{}
if ($SkipSolutionBuild) {
    $consumerArgs['SkipSolutionBuild'] = $true
}

Push-Location $repoRoot
try {
    & $consumerFixtures @consumerArgs
    & $gallerySmoke
    & $msixPackage @consumerArgs
    Write-Host 'Runtime gates passed: consumer fixture markers, Gallery smoke Light/Dark, and unsigned MSIX produce.'
}
finally {
    Pop-Location
}
