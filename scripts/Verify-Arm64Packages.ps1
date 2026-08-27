[CmdletBinding()]
param()

# Packs Foundation + Controls for arm64 and compiles the unpackaged package-consumer
# fixture for arm64. Does not launch the arm64 exe (this machine is x64).

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$artifactsRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot 'artifacts'))
$feed = [System.IO.Path]::GetFullPath((Join-Path $artifactsRoot 'arm64-packages'))
$packageCache = Join-Path $feed 'packages'
$configuration = 'Debug'
$platform = 'arm64'
$packageVersion = '0.1.0-preview.1'
$platformProperty = "-p:Platform=$platform"
$fixtureNuGetConfig = Join-Path $repoRoot 'tests\Ether.DesignSystem.ConsumerFixtures\NuGet.Config'
$unpackagedProject = Join-Path $repoRoot 'tests\Ether.DesignSystem.ConsumerFixtures\Unpackaged\Ether.DesignSystem.ConsumerFixtures.Unpackaged.csproj'
$foundationProject = Join-Path $repoRoot 'src\Ether.DesignSystem.Foundation\Ether.DesignSystem.Foundation.csproj'
$controlsProject = Join-Path $repoRoot 'src\Ether.DesignSystem.Controls\Ether.DesignSystem.Controls.csproj'

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

    $requiredPackages = @(
        (Join-Path $feed "Ether.DesignSystem.Foundation.$packageVersion.nupkg"),
        (Join-Path $feed "Ether.DesignSystem.Controls.$packageVersion.nupkg")
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
