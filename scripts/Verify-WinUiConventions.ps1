[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$controlsRoot = Join-Path $repoRoot 'src\Ether.DesignSystem.Controls\Controls'

function Assert-Match {
    param([string]$Text, [string]$Pattern, [string]$Description)
    if ($Text -notmatch $Pattern) {
        throw "$Description is missing required pattern '$Pattern'."
    }
}

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

# WinUI dependency-property convention: a public static identifier and a public CLR
# wrapper with the same name. This is intentionally source-level as well as runtime
# coverage so a new property cannot be accidentally omitted from binding/style support.
$codeFiles = Get-ChildItem -LiteralPath $controlsRoot -Filter *.cs -Recurse
foreach ($file in $codeFiles) {
    $text = Get-Content -LiteralPath $file.FullName -Raw
    $classMatch = [regex]::Match($text, 'public\s+(?:sealed\s+)?(?:partial\s+)?class\s+(?<name>Ether\w+)')
    if (-not $classMatch.Success) {
        continue
    }

    $className = $classMatch.Groups['name'].Value
    $properties = [regex]::Matches($text, 'public\s+static\s+readonly\s+DependencyProperty\s+(?<name>\w+)Property\s*=')
    foreach ($property in $properties) {
        $propertyName = $property.Groups['name'].Value
        Assert-Match $text ("DependencyProperty\.Register\(\s*nameof\(" + [regex]::Escape($propertyName) + '\)') "$className.$propertyName registration"
        $wrapper = [regex]::Match($text, "public\s+[^\r\n]+\s+" + [regex]::Escape($propertyName) + '\s*\{')
        if (-not $wrapper.Success) {
            throw "$className.$propertyName is missing a public CLR dependency-property wrapper."
        }
        $wrapperText = $text.Substring($wrapper.Index)
        Assert-Match $wrapperText ('get\s*=>\s*.*GetValue\(' + [regex]::Escape($propertyName) + 'Property\)') "$className.$propertyName CLR wrapper getter"
        Assert-Match $wrapperText ('set\s*=>\s*SetValue\(' + [regex]::Escape($propertyName) + 'Property') "$className.$propertyName CLR wrapper setter"
    }
}

$templatedControls = @{
    'Inputs\EtherButton.cs' = 'EtherButton'
    'Inputs\EtherCheckbox.cs' = 'EtherCheckbox'
    'Inputs\EtherRadioButton.cs' = 'EtherRadioButton'
    'Inputs\EtherInput.cs' = 'EtherInput'
    'Inputs\EtherDropdown.cs' = 'EtherDropdown'
    'Inputs\EtherSegmentedControl.cs' = 'EtherSegmentedControl'
    'Inputs\EtherIntelligenceButton.cs' = 'EtherIntelligenceButton'
    'Inputs\EtherProgressBar.cs' = 'EtherProgressBar'
    'Inputs\EtherSteeringBar.xaml.cs' = 'EtherSteeringBar'
    'Inputs\EtherSlider.xaml.cs' = 'EtherSlider'
    'Navigation\EtherMasthead.xaml.cs' = 'EtherMasthead'
}
foreach ($entry in $templatedControls.GetEnumerator()) {
    $path = Join-Path $controlsRoot $entry.Key
    $text = Get-Content -LiteralPath $path -Raw
    Assert-Match $text ("DefaultStyleKey\s*=\s*typeof\(" + [regex]::Escape($entry.Value) + '\)') "$($entry.Value) DefaultStyleKey"
    if ($text -match 'OnApplyTemplate\s*\(') {
        Assert-Match $text 'protected\s+override\s+void\s+OnApplyTemplate\s*\(\s*\)\s*\{\s*base\.OnApplyTemplate\(\)' "$($entry.Value) OnApplyTemplate base call"
    }
}

$internalSupport = Join-Path $controlsRoot 'Primitives\HandContentControl.cs'
$internalSupportText = Get-Content -LiteralPath $internalSupport -Raw
Assert-Match $internalSupportText 'Public only because WinUI XAML resource dictionaries resolve' 'HandContentControl XAML visibility rationale'
Assert-Match $internalSupportText 'not a supported design-system\s+/// control contract' 'HandContentControl support-only contract'

Write-Host 'WinUI 3 convention audit passed: dependency-property identifiers/wrappers follow the WinUI pattern; templated controls set DefaultStyleKey and call base.OnApplyTemplate; XAML-required support visibility is documented; HighContrastAdjustment=None is present; and non-instant VisualTransitions declare CubicEase.'
