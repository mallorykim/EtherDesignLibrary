[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$xamlNamespace = 'http://schemas.microsoft.com/winfx/2006/xaml/presentation'

function Get-MergedDictionarySources {
    param([Parameter(Mandatory)][string]$Path)

    [xml]$document = Get-Content -LiteralPath $Path -Raw
    return @($document.SelectNodes("//*[local-name()='ResourceDictionary.MergedDictionaries']/*[local-name()='ResourceDictionary']") |
        ForEach-Object { $_.GetAttribute('Source') } |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
}

function Assert-SetEquals {
    param([string[]]$Actual, [string[]]$Expected, [string]$Description)

    $unexpected = @($Actual | Where-Object { $_ -cnotin $Expected })
    $missing = @($Expected | Where-Object { $_ -cnotin $Actual })
    if ($unexpected.Count -gt 0 -or $missing.Count -gt 0) {
        throw "$Description does not have the expected resource graph. Unexpected: $($unexpected -join ', '); missing: $($missing -join ', ')."
    }

    if ($Actual.Count -ne $Expected.Count) {
        throw "$Description contains duplicate resource-dictionary sources."
    }

    for ($index = 0; $index -lt $Expected.Count; $index++) {
        if ($Actual[$index] -cne $Expected[$index]) {
            throw "$Description has the wrong resource-dictionary order at index $index. Expected '$($Expected[$index])', got '$($Actual[$index])'."
        }
    }
}

function Assert-CompiledDictionary {
    param([string]$Path, [string]$TypeName, [string]$Description)

    $text = Get-Content -LiteralPath $Path -Raw
    if ($text -notmatch "<controls:$TypeName(?:\s|/|>)") {
        throw "$Description does not instantiate compiled dictionary '$TypeName'."
    }
}

$foundation = Join-Path $repoRoot 'src\Ether.DesignSystem.Foundation\Themes\Foundation.xaml'
$controlsGeneric = Join-Path $repoRoot 'src\Ether.DesignSystem.Controls\Themes\Generic.xaml'
$controlsEntry = Join-Path $repoRoot 'src\Ether.DesignSystem.Controls\Themes\DesignSystem.xaml'
$galleryApp = Join-Path $repoRoot 'samples\Ether.DesignSystem.Gallery\App.xaml'
$fixtureApps = @(
    (Join-Path $repoRoot 'tests\Ether.DesignSystem.ConsumerFixtures\Unpackaged\App.xaml'),
    (Join-Path $repoRoot 'tests\Ether.DesignSystem.ConsumerFixtures\Packaged\App.xaml')
)

$frozenSources = @(
    'ms-appx:///Ether.DesignSystem.Foundation/Resources/Tokens/EtherPrimitives.xaml',
    'ms-appx:///Ether.DesignSystem.Foundation/Resources/Tokens/EtherColors.xaml',
    'ms-appx:///Ether.DesignSystem.Foundation/Resources/Tokens/EtherSpacing.xaml',
    'ms-appx:///Ether.DesignSystem.Foundation/Resources/Tokens/EtherTypography.xaml',
    'ms-appx:///Ether.DesignSystem.Foundation/Resources/Tokens/EtherIconGeometries.xaml'
)
Assert-SetEquals (Get-MergedDictionarySources $foundation) $frozenSources 'Foundation.xaml'
Assert-SetEquals (Get-MergedDictionarySources $controlsGeneric) @('ms-appx:///Ether.DesignSystem.Foundation/Themes/Foundation.xaml',
    'ms-appx:///Ether.DesignSystem.Controls/Controls/Inputs/EtherButton.xaml',
    'ms-appx:///Ether.DesignSystem.Controls/Controls/Inputs/EtherCheckbox.xaml',
    'ms-appx:///Ether.DesignSystem.Controls/Controls/Inputs/EtherRadioButton.xaml',
    'ms-appx:///Ether.DesignSystem.Controls/Controls/Inputs/EtherProgressBar.xaml',
    'ms-appx:///Ether.DesignSystem.Controls/Controls/Inputs/EtherSteeringBar.xaml',
    'ms-appx:///Ether.DesignSystem.Controls/Controls/Inputs/EtherSlider.xaml',
    'ms-appx:///Ether.DesignSystem.Controls/Controls/Navigation/EtherMasthead.xaml',
    'ms-appx:///Ether.DesignSystem.Controls/Controls/Inputs/EtherSegmentedControl.xaml',
    'ms-appx:///Ether.DesignSystem.Controls/Controls/Inputs/EtherDropdown.xaml',
    'ms-appx:///Ether.DesignSystem.Controls/Controls/Inputs/EtherInput.xaml',
    'ms-appx:///Ether.DesignSystem.Controls/Resources/Foundations/EtherCard.xaml',
    'ms-appx:///Ether.DesignSystem.Controls/Controls/Inputs/EtherIntelligenceButton.xaml') 'Controls Generic.xaml'
Assert-CompiledDictionary $controlsGeneric 'EtherScrollBar' 'Controls Generic.xaml'
Assert-CompiledDictionary $controlsGeneric 'EtherSwitch' 'Controls Generic.xaml'
Assert-SetEquals (Get-MergedDictionarySources $controlsEntry) @('ms-appx:///Ether.DesignSystem.Controls/Themes/Generic.xaml') 'Controls DesignSystem.xaml'

$controlsEntrySource = 'ms-appx:///Ether.DesignSystem.Controls/Themes/DesignSystem.xaml'
foreach ($app in @($galleryApp) + $fixtureApps) {
    $sources = Get-MergedDictionarySources $app
    if ($sources -cnotcontains $controlsEntrySource) {
        throw "$app does not merge the public Controls DesignSystem.xaml entry point."
    }
    $forbidden = @($sources | Where-Object { $_ -match 'Ether\.DesignSystem\.Foundation|Resources/Tokens|Themes/Generic\.xaml' })
    if ($forbidden.Count -gt 0) {
        throw "$app bypasses the Controls public resource entry point: $($forbidden -join ', ')."
    }
}

Write-Host 'Resource graph audit passed: app -> Controls DesignSystem -> Controls Generic -> Foundation -> five frozen token dictionaries.'
