[CmdletBinding()]
param()

# DISABLED / NOT CHAINED — not invoked from scripts/Verify-RuntimeGates.ps1, any
# CI workflow, or any release gate. Directory.Build.props declares only
# <Platforms>x64</Platforms> / <RuntimeIdentifiers>win-x64</RuntimeIdentifiers>
# (2026-08-30): the only consumers of this library are known to be x64-only, and
# this script's arm64 pack/compile output was never checked by any automated
# gate, so it verified a support range nobody was promising. The script itself
# still works and is kept for when arm64 demand actually appears; to re-enable,
# restore arm64 to Directory.Build.props' Platforms/RuntimeIdentifiers, add this
# script back to the $arm64Packages chain in Verify-RuntimeGates.ps1, and update
# the package README/consumer docs to claim arm64 support again.
#
# Packs Foundation, Controls, and Interactions for arm64 and compiles the
# unpackaged package-consumer fixture for arm64. Does not launch the arm64 exe
# (this machine is x64).

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$artifactsRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot 'artifacts'))
$feed = [System.IO.Path]::GetFullPath((Join-Path $artifactsRoot 'arm64-packages'))
$packageCache = Join-Path $feed 'packages'
$configuration = 'Debug'
$platform = 'arm64'
# Single source of truth for the preview version: Directory.Build.props (no drift on a version bump).
$directoryBuildProps = Join-Path $repoRoot 'Directory.Build.props'
if ((Get-Content -LiteralPath $directoryBuildProps -Raw) -match '<EtherDesignSystemPreviewVersion>([^<]+)</EtherDesignSystemPreviewVersion>') {
    $packageVersion = $Matches[1].Trim()
} else {
    throw "Could not read EtherDesignSystemPreviewVersion from $directoryBuildProps."
}
$platformProperty = "-p:Platform=$platform"
$fixtureNuGetConfig = Join-Path $repoRoot 'tests\Ether.DesignSystem.ConsumerFixtures\NuGet.Config'
$unpackagedProject = Join-Path $repoRoot 'tests\Ether.DesignSystem.ConsumerFixtures\Unpackaged\Ether.DesignSystem.ConsumerFixtures.Unpackaged.csproj'
$foundationProject = Join-Path $repoRoot 'src\Ether.DesignSystem.Foundation\Ether.DesignSystem.Foundation.csproj'
$controlsProject = Join-Path $repoRoot 'src\Ether.DesignSystem.Controls\Ether.DesignSystem.Controls.csproj'
$interactionsProject = Join-Path $repoRoot 'src\Ether.DesignSystem.Interactions\Ether.DesignSystem.Interactions.csproj'

if (-not $feed.StartsWith($artifactsRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to clear an output path outside artifacts: $feed"
}

function Invoke-DotNet {
    param([Parameter(Mandatory)][string[]]$Arguments)
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

function Stop-LeftoverUnpackagedFixture {
    Get-Process -Name 'Ether.DesignSystem.ConsumerFixtures.Unpackaged' -ErrorAction SilentlyContinue |
        Stop-Process -Force
}

if (-not (Test-Path -LiteralPath $fixtureNuGetConfig -PathType Leaf)) {
    throw "Required fixture NuGet.Config is missing: $fixtureNuGetConfig"
}

if (Test-Path -LiteralPath $feed) {
    Remove-Item -LiteralPath $feed -Recurse -Force
}

New-Item -ItemType Directory -Path $feed, $packageCache -Force | Out-Null

# The fixture NuGet.Config still points at artifacts/consumer-fixtures/local-feed (x64).
# Clone its source layout against this arm64 feed so restore does not pick up x64 nupkgs.
$nugetConfig = Join-Path $feed 'NuGet.Config'
$nugetConfigXml = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="ether-local" value="$feed" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
"@
Set-Content -LiteralPath $nugetConfig -Value $nugetConfigXml -Encoding UTF8

Push-Location $repoRoot
try {
    Stop-LeftoverUnpackagedFixture

    Invoke-DotNet @('pack', $foundationProject, '-c', $configuration, $platformProperty, '-o', $feed)
    Invoke-DotNet @('pack', $controlsProject, '-c', $configuration, $platformProperty, '-o', $feed)
    Invoke-DotNet @('pack', $interactionsProject, '-c', $configuration, $platformProperty, '-o', $feed)

    $requiredPackages = @(
        (Join-Path $feed "Ether.DesignSystem.Foundation.$packageVersion.nupkg"),
        (Join-Path $feed "Ether.DesignSystem.Controls.$packageVersion.nupkg"),
        (Join-Path $feed "Ether.DesignSystem.Interactions.$packageVersion.nupkg")
    )
    foreach ($package in $requiredPackages) {
        if (-not (Test-Path -LiteralPath $package -PathType Leaf)) {
            throw "arm64 pack gate is missing required nupkg: $package"
        }
    }

    Invoke-DotNet @(
        'restore', $unpackagedProject,
        '--configfile', $nugetConfig,
        '--packages', $packageCache,
        $platformProperty
    )
    Invoke-DotNet @(
        'build', $unpackagedProject,
        '-c', $configuration,
        $platformProperty,
        '--no-restore'
    )

    Write-Host "arm64 pack and unpackaged fixture compile passed (exe not launched): $feed"
}
finally {
    Pop-Location
}
