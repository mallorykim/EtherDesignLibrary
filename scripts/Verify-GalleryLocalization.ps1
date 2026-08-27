[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$galleryRoot = Join-Path $repoRoot 'samples\Ether.DesignSystem.Gallery'
$reswPath = Join-Path $galleryRoot 'Strings\en-US\Resources.resw'
$stringsPath = Join-Path $galleryRoot 'GalleryStrings.cs'
$catalogPath = Join-Path $galleryRoot 'ComponentCatalog.cs'
$homePath = Join-Path $galleryRoot 'Views\HomePage.xaml'
$mainWindowPath = Join-Path $galleryRoot 'MainWindow.xaml.cs'
$ciPath = Join-Path $repoRoot '.github\workflows\build.yml'
$labelPage = Join-Path $galleryRoot 'Views\Controls\LabelFilterChipPage.xaml'

function Assert-Contains {
    param([string]$Text, [string]$Pattern, [string]$Description)
    if ($Text -notmatch $Pattern) {
        throw "$Description is missing required pattern '$Pattern'."
    }
}

function Assert-NotContains {
    param([string]$Text, [string]$Pattern, [string]$Description)
    if ($Text -match $Pattern) {
        throw "$Description must not contain pattern '$Pattern'."
    }
}

foreach ($path in @($reswPath, $stringsPath, $catalogPath, $homePath, $mainWindowPath, $ciPath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required Gallery localization file is missing: $path"
    }
}

if (Test-Path -LiteralPath $labelPage -PathType Leaf) {
    throw 'Label / Filter Chip is out of this localization slice; LabelFilterChipPage.xaml must not be present.'
}

$strings = Get-Content -LiteralPath $stringsPath -Raw
Assert-Contains $strings 'class GalleryStrings' 'GalleryStrings helper'
Assert-Contains $strings 'ResourceLoader\.GetForViewIndependentUse' 'GalleryStrings uses ResourceLoader'
Assert-Contains $strings 'CatalogKey' 'GalleryStrings catalog key helper'

$catalog = Get-Content -LiteralPath $catalogPath -Raw
Assert-Contains $catalog 'DisplayName' 'ComponentEntry exposes DisplayName'
Assert-Contains $catalog 'DisplayHeader' 'CatalogCategory exposes DisplayHeader'
Assert-NotContains $catalog 'LabelFilterChipPage' 'Catalog does not include Label / Filter Chip'

$homeXaml = Get-Content -LiteralPath $homePath -Raw
Assert-Contains $homeXaml 'Text="\{x:Bind DisplayName\}"' 'Home cards bind DisplayName'

$mainWindow = Get-Content -LiteralPath $mainWindowPath -Raw
Assert-Contains $mainWindow 'entry\.DisplayName' 'Nav items use DisplayName'
Assert-Contains $mainWindow 'GalleryMainWindow\.ThemeDark\.Content' 'Theme toggle Dark string is localized'

$resw = Get-Content -LiteralPath $reswPath -Raw
$requiredKeys = @(
    'Catalog.Home',
    'Catalog.Foundations',
    'Catalog.Controls',
    'Catalog.Surfaces',
    'Catalog.Navigation',
    'Catalog.DataDisplay',
    'Catalog.Button',
    'Catalog.ProgressBar',
    'Catalog.Masthead',
    'Catalog.Card',
    'GalleryOutput.Clicked',
    'GalleryOutput.Selected',
    'GalleryOutput.Checked',
    'GalleryOutput.TextEmpty',
    'GalleryOutput.StateOn',
    'GalleryOutput.StateOff',
    'GalleryOutput.Value',
    'GalleryOutput.ValuePercent',
    'GalleryOutput.StopSingular',
    'GalleryOutput.StopPlural',
    'GalleryMainWindow.ThemeDark.Content',
    'GalleryViewsControlsButtonPageOutput.OutputText',
    'GalleryViewsDataDisplayProgressBarPageOutput.OutputText'
)
foreach ($key in $requiredKeys) {
    Assert-Contains $resw ([regex]::Escape("name=`"$key`"")) "Resources.resw contains '$key'"
}

$viewPages = Get-ChildItem -LiteralPath (Join-Path $galleryRoot 'Views') -Recurse -Filter '*Page.xaml'
foreach ($page in $viewPages) {
    if ($page.BaseName -in @('HomePage', 'ComponentPage')) {
        continue
    }

    $xaml = Get-Content -LiteralPath $page.FullName -Raw
    if ($xaml -notmatch '<views:ComponentPage\s+x:Uid="([^"]+)"') {
        throw "$($page.Name) ComponentPage is missing x:Uid."
    }
    $uid = $Matches[1]
    Assert-Contains $resw ([regex]::Escape("name=`"$uid.Title`"")) "$($page.Name) Title is in Resources.resw"
    Assert-Contains $resw ([regex]::Escape("name=`"$uid.Description`"")) "$($page.Name) Description is in Resources.resw"
}

$outputPages = @(
    @{ Rel = 'Views\Controls\ButtonPage.xaml.cs'; Key = 'GalleryOutput.Clicked' },
    @{ Rel = 'Views\Controls\CheckboxPage.xaml.cs'; Key = 'GalleryOutput.Checked' },
    @{ Rel = 'Views\Controls\RadioButtonPage.xaml.cs'; Key = 'GalleryOutput.Selected' },
    @{ Rel = 'Views\Controls\InputPage.xaml.cs'; Key = 'GalleryOutput.TextEmpty' },
    @{ Rel = 'Views\Controls\DropdownPage.xaml.cs'; Key = 'GalleryOutput.SelectedEmpty' },
    @{ Rel = 'Views\Controls\ToggleSwitchPage.xaml.cs'; Key = 'GalleryOutput.StateOn' },
    @{ Rel = 'Views\Controls\SliderPage.xaml.cs'; Key = 'GalleryOutput.ValuePercent' },
    @{ Rel = 'Views\Controls\SteeringBarPage.xaml.cs'; Key = 'GalleryOutput.StopPlural' },
    @{ Rel = 'Views\DataDisplay\ProgressBarPage.xaml.cs'; Key = 'GalleryOutput.Value' }
)
foreach ($page in $outputPages) {
    $code = Get-Content -LiteralPath (Join-Path $galleryRoot $page.Rel) -Raw
    Assert-Contains $code 'GalleryStrings\.' "$($page.Rel) uses GalleryStrings"
    Assert-Contains $code ([regex]::Escape($page.Key)) "$($page.Rel) uses $($page.Key)"
}

$ci = Get-Content -LiteralPath $ciPath -Raw
Assert-Contains $ci 'Verify-GalleryLocalization\.ps1' 'CI wires Verify-GalleryLocalization.ps1'

Write-Host 'Gallery localization contract passed: catalog, output, theme, page Title/Description, and resw keys are present.'
