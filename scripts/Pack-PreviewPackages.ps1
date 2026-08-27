[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [string]$OutputDirectory,
    [string]$Source,
    [string]$ApiKey
)

# Packs the coordinated preview pair into artifacts/packages (or -OutputDirectory).
# Push is opt-in: pass -Source (and -ApiKey when the feed requires it).
# Hosted CI must not call this with a public source.

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

Push-Location $repoRoot
try {
    Invoke-DotNet @('pack', $foundationProject, '-c', $Configuration, $platformProperty, '-o', $OutputDirectory)
    Invoke-DotNet @('pack', $controlsProject, '-c', $Configuration, $platformProperty, '-o', $OutputDirectory)
}
finally {
    Pop-Location
}

$packages = @(
    Get-ChildItem -LiteralPath $OutputDirectory -Filter 'Ether.DesignSystem.Foundation.*.nupkg' |
        Where-Object { $_.Name -notlike '*.symbols.nupkg' }
    Get-ChildItem -LiteralPath $OutputDirectory -Filter 'Ether.DesignSystem.Controls.*.nupkg' |
        Where-Object { $_.Name -notlike '*.symbols.nupkg' }
)

if ($packages.Count -lt 2) {
    throw "Expected Foundation and Controls nupkg files under $OutputDirectory."
}

foreach ($package in $packages) {
    Write-Host "Packed $($package.FullName)"
}

if ([string]::IsNullOrWhiteSpace($Source)) {
    Write-Host "Preview packages are local only. Pass -Source to push (and -ApiKey when required)."
    return
}

foreach ($package in $packages) {
    $pushArgs = @('nuget', 'push', $package.FullName, '--source', $Source, '--skip-duplicate')
    if (-not [string]::IsNullOrWhiteSpace($ApiKey)) {
        $pushArgs += @('--api-key', $ApiKey)
    }
    Invoke-DotNet $pushArgs
}

Write-Host "Pushed preview packages to $Source."
