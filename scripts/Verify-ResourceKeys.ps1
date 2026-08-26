[CmdletBinding()]
param(
    [string]$OutputPath = (Join-Path $PSScriptRoot '..\artifacts\resource-key-audit.json'),
    [switch]$RequireHighContrastParity
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$colorsPath = Join-Path $repoRoot 'src\Resources\Tokens\EtherColors.xaml'
$xamlNamespace = 'http://schemas.microsoft.com/winfx/2006/xaml'

$expectedTokenHashes = [ordered]@{
    'src/Resources/Tokens/EtherPrimitives.xaml' = 'd6ff0e5301b3672dbb492484c8d0ea584aca12be'
    'src/Resources/Tokens/EtherColors.xaml' = '794404850d4eb20a2fc3ec43c3480be8f318ac64'
    'src/Resources/Tokens/EtherSpacing.xaml' = '5d631cd2ebde306d389a1fa8999000359441bcd2'
    'src/Resources/Tokens/EtherTypography.xaml' = 'b24444567ee95467408e004fe7fab622eba79759'
    'src/Resources/Tokens/EtherIconGeometries.xaml' = '134c1667934376ba4943cf350b110a02607c81f4'
}

Push-Location $repoRoot
try {
    foreach ($tokenPath in $expectedTokenHashes.Keys) {
        $actualHash = (& git hash-object $tokenPath).Trim()
        if ($actualHash -ne $expectedTokenHashes[$tokenPath]) {
            throw "Frozen token hash changed for $tokenPath. Expected $($expectedTokenHashes[$tokenPath]), got $actualHash."
        }
    }
}
finally {
    Pop-Location
}

[xml]$document = Get-Content -LiteralPath $colorsPath -Raw
$themeDictionaries = @($document.SelectNodes("//*[local-name()='ResourceDictionary.ThemeDictionaries']/*[local-name()='ResourceDictionary']"))
if ($themeDictionaries.Count -eq 0) {
    throw "No ThemeDictionaries were found in $colorsPath."
}

$themeKeys = @{}
foreach ($dictionary in $themeDictionaries) {
    $themeKeyAttribute = @($dictionary.Attributes | Where-Object {
        $_.NamespaceURI -eq $xamlNamespace -and $_.LocalName -eq 'Key'
    })
    if ($themeKeyAttribute.Count -ne 1) {
        throw 'A theme ResourceDictionary is missing its x:Key or has more than one x:Key.'
    }

    $themeName = $themeKeyAttribute[0].Value
    if ($themeKeys.ContainsKey($themeName)) {
        throw "EtherColors.xaml contains more than one ThemeDictionary named '$themeName'."
    }

    $resourceKeyOccurrences = [System.Collections.Generic.List[string]]::new()
    foreach ($resource in @($dictionary.ChildNodes | Where-Object { $_.NodeType -eq [System.Xml.XmlNodeType]::Element })) {
        foreach ($keyAttribute in @($resource.Attributes | Where-Object {
            $_.NamespaceURI -eq $xamlNamespace -and $_.LocalName -eq 'Key'
        })) {
            $resourceKeyOccurrences.Add($keyAttribute.Value)
        }
    }

    $duplicateKeys = @($resourceKeyOccurrences | Group-Object | Where-Object Count -gt 1 | Select-Object -ExpandProperty Name)
    if ($duplicateKeys.Count -gt 0) {
        throw "ThemeDictionary '$themeName' contains duplicate x:Key values: $($duplicateKeys -join ', ')."
    }

    $themeKeys[$themeName] = @($resourceKeyOccurrences | Sort-Object)
}

foreach ($requiredTheme in 'Light', 'Dark', 'HighContrast') {
    if (-not $themeKeys.ContainsKey($requiredTheme)) {
        throw "EtherColors.xaml is missing the '$requiredTheme' ThemeDictionary."
    }
}

function Get-KeyDifference {
    param([string[]]$Left, [string[]]$Right)

    return @($Left | Where-Object { $_ -cnotin $Right })
}

$lightOnly = @(Get-KeyDifference $themeKeys.Light $themeKeys.Dark)
$darkOnly = @(Get-KeyDifference $themeKeys.Dark $themeKeys.Light)
$missingFromHighContrast = @(Get-KeyDifference $themeKeys.Light $themeKeys.HighContrast)
$highContrastOnly = @(Get-KeyDifference $themeKeys.HighContrast $themeKeys.Light)

$result = [ordered]@{
    source = 'src/Resources/Tokens/EtherColors.xaml'
    tokenHashes = $expectedTokenHashes
    themes = [ordered]@{
        Light = $themeKeys.Light
        Dark = $themeKeys.Dark
        HighContrast = $themeKeys.HighContrast
    }
    counts = [ordered]@{
        Light = $themeKeys.Light.Count
        Dark = $themeKeys.Dark.Count
        HighContrast = $themeKeys.HighContrast.Count
    }
    differences = [ordered]@{
        lightOnly = $lightOnly
        darkOnly = $darkOnly
        missingFromHighContrast = $missingFromHighContrast
        highContrastOnly = $highContrastOnly
    }
}

$resolvedOutputPath = [System.IO.Path]::GetFullPath($OutputPath)
$outputDirectory = Split-Path -Parent $resolvedOutputPath
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
$result | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $resolvedOutputPath -Encoding utf8

Write-Host "Resource-key audit written to $resolvedOutputPath"
Write-Host "Light: $($themeKeys.Light.Count); Dark: $($themeKeys.Dark.Count); HighContrast: $($themeKeys.HighContrast.Count)"
Write-Host "Light-only: $($lightOnly.Count); Dark-only: $($darkOnly.Count); Missing from HighContrast: $($missingFromHighContrast.Count); HighContrast-only: $($highContrastOnly.Count)"

if ($missingFromHighContrast.Count -gt 0 -or $highContrastOnly.Count -gt 0) {
    Write-Warning 'HighContrast key-set differences are reported only; frozen tokens are not modified by this audit.'
    if ($RequireHighContrastParity) {
        throw "HighContrast ThemeDictionary resource key parity is required for this release gate. Missing: $($missingFromHighContrast.Count); HighContrast-only: $($highContrastOnly.Count)."
    }
}

if ($lightOnly.Count -gt 0 -or $darkOnly.Count -gt 0) {
    throw 'Light and Dark ThemeDictionary resource key sets differ. See the JSON audit artifact for exact keys.'
}
