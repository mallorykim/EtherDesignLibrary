[CmdletBinding()]
param(
    [switch]$SkipSolutionBuild
)

# Local / self-hosted desktop owner for Gallery smoke, L3 consumer runtime
# markers, unsigned MSIX produce, and arm64 pack/compile. Hosted
# .github/workflows/build.yml must not invoke this script.

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$consumerFixtures = Join-Path $repoRoot 'scripts\Verify-ConsumerFixtures.ps1'
$gallerySmoke = Join-Path $repoRoot 'scripts\Verify-GallerySmoke.ps1'
$msixPackage = Join-Path $repoRoot 'scripts\Verify-MsixPackage.ps1'
$arm64Packages = Join-Path $repoRoot 'scripts\Verify-Arm64Packages.ps1'

foreach ($path in @($consumerFixtures, $gallerySmoke, $msixPackage, $arm64Packages)) {
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
    & $arm64Packages
    Write-Host 'Runtime gates passed: consumer fixture markers, Gallery smoke Light/Dark, unsigned MSIX produce, and arm64 pack/compile.'
}
finally {
    Pop-Location
}
