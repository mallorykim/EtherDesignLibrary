[CmdletBinding()]
param(
    [switch]$SkipSolutionBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$project = Join-Path $repoRoot 'tests\Ether.DesignSystem.ConsumerFixtures\Packaged\Ether.DesignSystem.ConsumerFixtures.Packaged.csproj'
$configuration = 'Debug'
$platform = 'x64'

function Invoke-DotNet {
    param([Parameter(Mandatory)][string[]]$Arguments)
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

Push-Location $repoRoot
try {
    if (-not $SkipSolutionBuild) {
        Invoke-DotNet @('build', 'Ether.DesignSystem.slnx', '-c', $configuration, "-p:Platform=$platform")
    }

    Invoke-DotNet @(
        'build', $project,
        '-c', $configuration,
        "-p:Platform=$platform",
        '-p:GenerateAppxPackageOnBuild=true',
        '-p:AppxPackageSigningEnabled=false',
        # Symbol-free fixture: the packaged .csproj drops .pdb payload so the WindowsAppSDK MSIX
        # targets never look for mspdbcmf.exe (absent on CI runners without full MSVC tooling).
        '-p:EtherDesignSystemFoundationIncludeHostRootFonts=false'
    )

    $appPackages = Join-Path $repoRoot 'tests\Ether.DesignSystem.ConsumerFixtures\Packaged\AppPackages'
    $packages = @(Get-ChildItem -LiteralPath $appPackages -Recurse -Include *.msix, *.msixbundle -ErrorAction SilentlyContinue)
    if ($packages.Count -eq 0) {
        throw "MSIX produce gate found no .msix or .msixbundle under $appPackages."
    }

    $artifact = $packages | Sort-Object Length -Descending | Select-Object -First 1
    if ($artifact.Length -lt 1024) {
        throw "MSIX artifact '$($artifact.FullName)' is too small: $($artifact.Length) bytes."
    }

    Write-Host "MSIX produce gate passed (unsigned, not installed): $($artifact.FullName) ($($artifact.Length) bytes)"
}
finally {
    Pop-Location
}
