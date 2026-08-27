[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$controlsRoot = Join-Path $repoRoot 'src\Ether.DesignSystem.Controls\Controls'

$expectedStyleFiles = @{
    'Inputs\EtherButton.xaml'              = 'DefaultEtherButtonStyle'
    'Inputs\EtherCheckbox.xaml'            = 'DefaultEtherCheckboxStyle'
    'Inputs\EtherRadioButton.xaml'         = 'DefaultEtherRadioButtonStyle'
    'Inputs\EtherInput.xaml'               = 'DefaultEtherInputStyle'
    'Inputs\EtherDropdown.xaml'            = 'DefaultEtherDropdownStyle'
    'Inputs\EtherSegmentedControl.xaml'    = 'DefaultEtherSegmentedControlStyle'
    'Inputs\EtherIntelligenceButton.xaml'  = 'DefaultEtherIntelligenceButtonStyle'
    'Inputs\EtherSteeringBar.xaml'         = 'DefaultEtherSteeringBarStyle'
    'Inputs\EtherSlider.xaml'              = 'DefaultEtherSliderStyle'
    'Inputs\EtherProgressBar.xaml'         = 'DefaultEtherProgressBarStyle'
    'Inputs\EtherSwitch.xaml'              = 'EtherSwitch'
    'Inputs\EtherScrollBar.xaml'           = 'ScrollBar'
    'Navigation\EtherMasthead.xaml'        = 'DefaultEtherMastheadStyle'
}

foreach ($relative in $expectedStyleFiles.Keys) {
    $path = Join-Path $controlsRoot $relative
    $text = Get-Content -LiteralPath $path -Raw
    if ($text -notmatch 'HighContrastAdjustment"\s+Value="None"') {
        throw "$relative must set HighContrastAdjustment=None so custom High Contrast brushes are not overdrawn."
    }
}

$xamlFiles = Get-ChildItem -LiteralPath $controlsRoot -Filter *.xaml -Recurse
foreach ($file in $xamlFiles) {
    $text = Get-Content -LiteralPath $file.FullName -Raw
    $selfClosing = [regex]::Matches($text, '<VisualTransition\b[^>]*GeneratedDuration="(?!0:0:0(?:\.0)?)[^"]+"\s*/>')
    if ($selfClosing.Count -gt 0) {
        throw "$($file.Name) has a non-instant VisualTransition without GeneratedEasingFunction."
    }

    $openTransitions = [regex]::Matches($text, '(?s)<VisualTransition\b[^>]*GeneratedDuration="(?<duration>[^"]+)"(?<body>.*?</VisualTransition>)')
    foreach ($transition in $openTransitions) {
        $duration = $transition.Groups['duration'].Value
        if ($duration -eq '0:0:0' -or $duration -eq '0') {
            continue
        }

        if ($transition.Groups['body'].Value -notmatch 'GeneratedEasingFunction') {
            throw "$($file.Name) VisualTransition duration '$duration' is missing GeneratedEasingFunction."
        }
    }
}

Write-Host 'WinUI 3 convention audit passed: HighContrastAdjustment=None on default styles, and non-instant VisualTransitions declare CubicEase.'
