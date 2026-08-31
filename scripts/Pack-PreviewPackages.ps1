[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [string]$OutputDirectory
)

# Packs the coordinated Foundation, Controls, and Interactions preview packages. Pack-only - this
# script has no push capability (R-03). scripts/Publish-Internal.ps1 is the single authorized
# publish entry point: it runs the full acceptance gate chain (derived from scripts/Gates.psd1),
# calls this script to pack, and only then - behind its own -Push switch, feed configuration, and
# a typed confirmation - runs `dotnet nuget push` itself. This script used to also accept -Source
# / -ApiKey and push directly, which let a real release bypass every gate Publish-Internal.ps1
# enforces; do not re-add push here.

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot 'artifacts\packages'
}

$OutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

function Invoke-DotNet {
    param([Parameter(Mandatory)][string[]]$Arguments)

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

$platformProperty = '-p:Platform=x64'
$foundationProject = Join-Path $repoRoot 'src\Ether.DesignSystem.Foundation\Ether.DesignSystem.Foundation.csproj'
$controlsProject = Join-Path $repoRoot 'src\Ether.DesignSystem.Controls\Ether.DesignSystem.Controls.csproj'
$interactionsProject = Join-Path $repoRoot 'src\Ether.DesignSystem.Interactions\Ether.DesignSystem.Interactions.csproj'

Push-Location $repoRoot
try {
    Invoke-DotNet @('pack', $foundationProject, '-c', $Configuration, $platformProperty, '-o', $OutputDirectory)
    Invoke-DotNet @('pack', $controlsProject, '-c', $Configuration, $platformProperty, '-o', $OutputDirectory)
    Invoke-DotNet @('pack', $interactionsProject, '-c', $Configuration, $platformProperty, '-o', $OutputDirectory)
}
finally {
    Pop-Location
}

$packages = @(
    Get-ChildItem -LiteralPath $OutputDirectory -Filter 'Ether.DesignSystem.Foundation.*.nupkg' |
        Where-Object { $_.Name -notlike '*.symbols.nupkg' }
    Get-ChildItem -LiteralPath $OutputDirectory -Filter 'Ether.DesignSystem.Controls.*.nupkg' |
        Where-Object { $_.Name -notlike '*.symbols.nupkg' }
    Get-ChildItem -LiteralPath $OutputDirectory -Filter 'Ether.DesignSystem.Interactions.*.nupkg' |
        Where-Object { $_.Name -notlike '*.symbols.nupkg' }
)

if ($packages.Count -lt 3) {
    throw "Expected Foundation, Controls, and Interactions nupkg files under $OutputDirectory."
}

foreach ($package in $packages) {
    Write-Host "Packed $($package.FullName)"
}

Write-Host "Preview packages are local only. This script cannot push - use scripts/Publish-Internal.ps1 -Push to publish for real."
