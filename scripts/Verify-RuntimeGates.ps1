[CmdletBinding()]
param(
    [switch]$SkipSolutionBuild
)

# Local / self-hosted desktop owner for Gallery smoke and L3 consumer runtime
# markers. Hosted .github/workflows/build.yml must not invoke this script.

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$consumerFixtures = Join-Path $repoRoot 'scripts\Verify-ConsumerFixtures.ps1'
$gallerySmoke = Join-Path $repoRoot 'scripts\Verify-GallerySmoke.ps1'

foreach ($path in @($consumerFixtures, $gallerySmoke)) {
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
    Write-Host 'Runtime gates passed: consumer fixture markers and Gallery smoke Light/Dark.'
}
finally {
    Pop-Location
}
