[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    [switch]$SkipRuntimeSmoke
)

# Verifies a real, unpackaged x64 WinUI 3 consumer outside this repository.
#
# Official WinUI resource-consumption references:
# - https://learn.microsoft.com/windows/apps/winui/winui3/desktop-winui3-app-with-basic-interop
#   (the WinUI app template puts XamlControlsResources in App.xaml).
# - https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.resourcedictionary.mergeddictionaries
#   (application-level ResourceDictionary.MergedDictionaries is the supported sharing mechanism).
# - https://learn.microsoft.com/windows/apps/winui/winui3/xaml-templated-controls-csharp-winui-3
#   (a control library's default styles live at Themes/Generic.xaml).

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$packageVersion = '0.1.0-preview.1'
$windowsAppSdkVersion = '2.3.1'
$platformProperty = '-p:Platform=x64'
$artifactsRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot 'artifacts'))
$localFeed = Join-Path $repoRoot 'artifacts\consumer-fixtures\external-local-feed'
$temporaryRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("Ether.DesignSystem.ExternalConsumer-{0}" -f [guid]::NewGuid().ToString('N'))
$succeeded = $false

function Invoke-DotNet {
    param(
        [Parameter(Mandatory)][string[]]$Arguments,
        [Parameter(Mandatory)][string]$LogPath
    )

    & dotnet @Arguments 2>&1 | Tee-Object -FilePath $LogPath | Out-Host
    return $LASTEXITCODE
}

function Write-Utf8File {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Content
    )

    Set-Content -LiteralPath $Path -Value $Content -Encoding utf8
}

function New-ExternalConsumerProject {
    param(
        [Parameter(Mandatory)][string]$Variant,
        [Parameter(Mandatory)][bool]$IncludeWindowsAppSdk
    )

    $projectRoot = Join-Path $temporaryRoot $Variant
    New-Item -ItemType Directory -Path $projectRoot -Force | Out-Null

    $packageReferences = @(
        ('    <PackageReference Include="Ether.DesignSystem.Foundation" Version="' + $packageVersion + '" />'),
        ('    <PackageReference Include="Ether.DesignSystem.Controls" Version="' + $packageVersion + '" />'),
        ('    <PackageReference Include="Ether.DesignSystem.Interactions" Version="' + $packageVersion + '" />')
    )
    if ($IncludeWindowsAppSdk) {
        $packageReferences += ('    <PackageReference Include="Microsoft.WindowsAppSDK" Version="' + $windowsAppSdkVersion + '" />')
    }

    # These are valid, intentionally empty MSBuild projects. They terminate upward discovery
    # before this repository's Directory.Build.props / Directory.Packages.props can participate.
    Write-Utf8File (Join-Path $temporaryRoot 'Directory.Build.props') '<Project />'
    Write-Utf8File (Join-Path $temporaryRoot 'Directory.Build.targets') '<Project />'
    Write-Utf8File (Join-Path $temporaryRoot 'Directory.Packages.props') '<Project />'

    $feedUri = ([System.Uri]$localFeed).AbsoluteUri
    Write-Utf8File (Join-Path $temporaryRoot 'nuget.config') @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="Ether-local" value="$feedUri" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
"@

    Write-Utf8File (Join-Path $projectRoot 'ExternalConsumer.csproj') @"
<!-- A clean consumer: every PackageReference version is explicit; no central package management. -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
    <TargetPlatformMinVersion>10.0.17763.0</TargetPlatformMinVersion>
    <Platforms>x64</Platforms>
    <PlatformTarget>x64</PlatformTarget>
    <RuntimeIdentifiers>win-x64</RuntimeIdentifiers>
    <UseWinUI>true</UseWinUI>
    <RootNamespace>Ether.DesignSystem.ExternalConsumer</RootNamespace>
    <ApplicationManifest>app.manifest</ApplicationManifest>
    <EnableMsixTooling>true</EnableMsixTooling>
    <WindowsPackageType>None</WindowsPackageType>
    <SelfContained>false</SelfContained>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
$($packageReferences -join [Environment]::NewLine)
    <Manifest Include="`$(ApplicationManifest)" />
  </ItemGroup>
</Project>
"@

    Write-Utf8File (Join-Path $projectRoot 'app.manifest') @'
<?xml version="1.0" encoding="utf-8"?>
<assembly manifestVersion="1.0" xmlns="urn:schemas-microsoft-com:asm.v1">
  <assemblyIdentity version="1.0.0.0" name="Ether.DesignSystem.ExternalConsumer.app" />
  <trustInfo xmlns="urn:schemas-microsoft-com:asm.v2">
    <security><requestedPrivileges xmlns="urn:schemas-microsoft-com:asm.v3"><requestedExecutionLevel level="asInvoker" uiAccess="false" /></requestedPrivileges></security>
  </trustInfo>
  <compatibility xmlns="urn:schemas-microsoft-com:compatibility.v1"><application><supportedOS Id="{8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a}" /></application></compatibility>
</assembly>
'@

    # This is the standard WinUI 3 app resource arrangement used by Microsoft's templates.
    # The Ether URI deliberately exercises packaged Generic.xaml/XBF/PRI library resources.
    Write-Utf8File (Join-Path $projectRoot 'App.xaml') @'
<Application
    x:Class="Ether.DesignSystem.ExternalConsumer.App"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />
                <ResourceDictionary Source="ms-appx:///Ether.DesignSystem.Controls/Themes/DesignSystem.xaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
'@

    Write-Utf8File (Join-Path $projectRoot 'App.xaml.cs') @'
using System.Reflection;
using System.Text.Json;
using Ether.DesignSystem.Interactions;
using Microsoft.UI.Xaml;

namespace Ether.DesignSystem.ExternalConsumer;

public partial class App : Application
{
    private MainWindow? _window;

    public App() => InitializeComponent();

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            _ = InteractionEvent.Create(
                "external-consumer-started",
                new InteractionContext("external-consumer"),
                "external-consumer",
                new { });

            _window = new MainWindow();
            _window.Activate();
            await _window.WaitForLayoutAsync();

            WriteMarker(new
            {
                marker = "ETHER_EXTERNAL_CONSUMER",
                outcome = "success",
                assemblies = new Dictionary<string, bool>
                {
                    ["Ether.DesignSystem.Foundation"] = Assembly.Load("Ether.DesignSystem.Foundation") is not null,
                    ["Ether.DesignSystem.Controls"] = typeof(Ether.DesignSystem.Controls.EtherButton).Assembly is not null,
                    ["Ether.DesignSystem.Interactions"] = typeof(InteractionEvent).Assembly is not null,
                },
                resourceKeys = _window.GetResolvedResourceKeys(),
                xamlProperties = _window.GetXamlPropertyAssertions(),
                layout = _window.GetLayoutAssertion(),
            });
        }
        catch (Exception exception)
        {
            WriteMarker(new
            {
                marker = "ETHER_EXTERNAL_CONSUMER",
                outcome = "failure",
                error = exception.ToString(),
            });
        }
        finally
        {
            _window?.Close();
            Exit();
        }
    }

    private static void WriteMarker(object marker)
    {
        var path = Environment.GetEnvironmentVariable("ETHER_EXTERNAL_CONSUMER_RESULT_PATH");
        if (string.IsNullOrWhiteSpace(path))
            throw new InvalidOperationException("ETHER_EXTERNAL_CONSUMER_RESULT_PATH was not set.");

        File.WriteAllText(path, JsonSerializer.Serialize(marker));
    }
}
'@

    Write-Utf8File (Join-Path $projectRoot 'MainWindow.xaml') @'
<Window
    x:Class="Ether.DesignSystem.ExternalConsumer.MainWindow"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:ether="using:Ether.DesignSystem.Controls"
    xmlns:media="using:Microsoft.UI.Xaml.Media"
    Title="External Ether consumer">
    <ScrollViewer>
        <StackPanel x:Name="ProbeRoot" Padding="24" Spacing="12">
            <!-- Enum + string + object-valued properties are all set through XAML markup. -->
            <ether:EtherButton x:Name="ButtonProbe"
                               Variant="Tertiary"
                               Size="Small"
                               Content="Hello from external XAML">
                <ether:EtherButton.LeftIcon><SymbolIcon Symbol="Accept" /></ether:EtherButton.LeftIcon>
            </ether:EtherButton>

            <!-- Numeric + bool + string/object property conversion through XAML markup. -->
            <ether:EtherProgressBar x:Name="ProgressProbe"
                                    Minimum="0" Maximum="100" Value="65"
                                    Title="Download" ValueContent="65%"
                                    ShowTitle="True" ShowValue="True" />

            <!-- An Items collection and public numeric properties set in markup. -->
            <ether:EtherDropdown x:Name="DropdownProbe" SelectedIndex="1" MaxVisibleItems="2" MenuGap="8">
                <ComboBoxItem Content="First" />
                <ComboBoxItem Content="Second" />
            </ether:EtherDropdown>

            <ether:EtherSegmentedControl x:Name="SegmentedProbe" SelectedValue="beta">
                <ether:EtherSegmentPanel Spacing="6">
                    <RadioButton Tag="alpha" Content="Alpha" />
                    <RadioButton Tag="beta" Content="Beta" />
                </ether:EtherSegmentPanel>
            </ether:EtherSegmentedControl>

            <!-- Collection-valued Labels and Stops exercise XAML collection conversion. -->
            <ether:EtherSlider x:Name="SliderProbe"
                               Minimum="0" Maximum="100" Value="50"
                               Title="Volume" ShowTitle="True" ShowLabels="True" SnapToStops="True">
                <ether:EtherSlider.Labels><ether:EtherSliderLabelCollection><x:String>Low</x:String><x:String>Medium</x:String><x:String>High</x:String></ether:EtherSliderLabelCollection></ether:EtherSlider.Labels>
                <ether:EtherSlider.Stops><media:DoubleCollection>0,50,100</media:DoubleCollection></ether:EtherSlider.Stops>
            </ether:EtherSlider>

            <ether:EtherSteeringBar x:Name="SteeringProbe"
                                    Minimum="0" Maximum="100" Value="75"
                                    PreviewStatus="Hover" SmallChange="5" LargeChange="25"
                                    Title="Playback" ValueContent="75%"
                                    ShowTitle="True" ShowValue="True" ShowStops="True" SnapToStops="True">
                <ether:EtherSteeringBar.Stops><media:DoubleCollection>0,25,50,75,100</media:DoubleCollection></ether:EtherSteeringBar.Stops>
            </ether:EtherSteeringBar>

            <ether:EtherMasthead x:Name="MastheadProbe"
                                 EnableWindowCommands="False"
                                 ShowSettings="False" ShowSearch="True"
                                 ShowMenuIcon="False" ShowChevron="True" />

            <ether:EtherInput x:Name="InputProbe" Text="External text" PlaceholderText="External placeholder" />
            <ether:EtherCheckbox x:Name="CheckboxProbe" Content="Checked from XAML" IsChecked="True" />
            <ether:EtherRadioButton x:Name="RadioProbe" GroupName="external" Content="Selected from XAML" IsChecked="True" />
            <ether:EtherIntelligenceButton x:Name="IntelligenceProbe" Content="Ask Ether" />
        </StackPanel>
    </ScrollViewer>
</Window>
'@

    Write-Utf8File (Join-Path $projectRoot 'MainWindow.xaml.cs') @'
using Ether.DesignSystem.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Ether.DesignSystem.ExternalConsumer;

public sealed partial class MainWindow : Window
{
    private readonly TaskCompletionSource _layoutReady = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public MainWindow()
    {
        InitializeComponent();
        ProbeRoot.Loaded += (_, _) => DispatcherQueue.TryEnqueue(() => _layoutReady.TrySetResult());
    }

    public async Task WaitForLayoutAsync()
    {
        await _layoutReady.Task;
        await Task.Delay(100);
    }

    public IReadOnlyList<string> GetResolvedResourceKeys()
    {
        var expected = new[] { "Spacing8", "InterFont", "InstrumentSans", "IconAddCir", "DefaultEtherButtonStyle", "DefaultEtherSliderStyle" };
        return expected.Where(key => Application.Current.Resources.TryGetValue(key, out _)).ToArray();
    }

    public IReadOnlyDictionary<string, bool> GetXamlPropertyAssertions() => new Dictionary<string, bool>
    {
        ["button.enum.string.icon"] = ButtonProbe.Variant == EtherButtonVariant.Tertiary &&
            ButtonProbe.Size == EtherButtonSize.Small &&
            Equals(ButtonProbe.Content, "Hello from external XAML") && ButtonProbe.LeftIcon is SymbolIcon,
        ["progress.numeric.bool.object"] = ProgressProbe.Minimum == 0 && ProgressProbe.Maximum == 100 &&
            ProgressProbe.Value == 65 && Equals(ProgressProbe.Title, "Download") && Equals(ProgressProbe.ValueContent, "65%") &&
            ProgressProbe.ShowTitle && ProgressProbe.ShowValue,
        ["dropdown.items.numeric"] = DropdownProbe.SelectedIndex == 1 && DropdownProbe.MaxVisibleItems == 2 &&
            DropdownProbe.MenuGap == 8 && DropdownProbe.Items.Count == 2,
        ["segmented.value.panel"] = Equals(SegmentedProbe.SelectedValue, "beta"),
        ["slider.collection.numeric.bool"] = SliderProbe.Minimum == 0 && SliderProbe.Maximum == 100 && SliderProbe.Value == 50 &&
            SliderProbe.ShowTitle && SliderProbe.ShowLabels && SliderProbe.SnapToStops && SliderProbe.Title == "Volume" &&
            SliderProbe.Labels is { Count: 3 } && SliderProbe.Stops is { Count: 3 },
        ["steering.enum.collection.numeric.bool"] = SteeringProbe.Minimum == 0 && SteeringProbe.Maximum == 100 && SteeringProbe.Value == 75 &&
            SteeringProbe.PreviewStatus == SteeringBarPreviewStatus.Hover && SteeringProbe.SmallChange == 5 && SteeringProbe.LargeChange == 25 &&
            SteeringProbe.ShowTitle && SteeringProbe.ShowValue && SteeringProbe.ShowStops && SteeringProbe.SnapToStops &&
            Equals(SteeringProbe.Title, "Playback") && Equals(SteeringProbe.ValueContent, "75%") && SteeringProbe.Stops is { Count: 5 },
        ["masthead.bool"] = !MastheadProbe.EnableWindowCommands && !MastheadProbe.ShowSettings && MastheadProbe.ShowSearch &&
            !MastheadProbe.ShowMenuIcon && MastheadProbe.ShowChevron,
        ["inherited.controls"] = InputProbe.Text == "External text" && CheckboxProbe.IsChecked == true &&
            RadioProbe.IsChecked == true && Equals(IntelligenceProbe.Content, "Ask Ether"),
    };

    public object GetLayoutAssertion() => new
    {
        rootWidth = ProbeRoot.ActualWidth,
        rootHeight = ProbeRoot.ActualHeight,
        buttonWidth = ButtonProbe.ActualWidth,
        buttonHeight = ButtonProbe.ActualHeight,
        rendered = ProbeRoot.ActualWidth > 0 && ProbeRoot.ActualHeight > 0 && ButtonProbe.ActualWidth > 0 && ButtonProbe.ActualHeight > 0,
    };
}
'@

    return $projectRoot
}

function Assert-RuntimeMarker {
    param([Parameter(Mandatory)][string]$MarkerPath)

    if (-not (Test-Path -LiteralPath $MarkerPath -PathType Leaf)) {
        throw "Application exited without writing runtime marker '$MarkerPath'."
    }

    $marker = Get-Content -LiteralPath $MarkerPath -Raw | ConvertFrom-Json
    $expectedResources = @('Spacing8', 'InterFont', 'InstrumentSans', 'IconAddCir', 'DefaultEtherButtonStyle', 'DefaultEtherSliderStyle')
    $missingResources = @($expectedResources | Where-Object { $_ -cnotin @($marker.resourceKeys) })
    $failedProperties = @($marker.xamlProperties.PSObject.Properties | Where-Object { $_.Value -ne $true })
    $missingAssemblies = @('Ether.DesignSystem.Foundation', 'Ether.DesignSystem.Controls', 'Ether.DesignSystem.Interactions' |
        Where-Object { $marker.assemblies.PSObject.Properties[$_].Value -ne $true })

    if ($marker.marker -ne 'ETHER_EXTERNAL_CONSUMER' -or $marker.outcome -ne 'success' -or
        $missingAssemblies.Count -ne 0 -or $missingResources.Count -ne 0 -or
        $failedProperties.Count -ne 0 -or $marker.layout.rendered -ne $true) {
        throw "External consumer runtime assertion failed: $(Get-Content -LiteralPath $MarkerPath -Raw)"
    }
}

function Get-ZipEntrySha256 {
    param(
        [Parameter(Mandatory)][string]$PackagePath,
        [Parameter(Mandatory)][string]$EntryPath
    )

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [System.IO.Compression.ZipFile]::OpenRead($PackagePath)
    try {
        $entry = @($archive.Entries | Where-Object { $_.FullName -ceq $EntryPath })[0]
        if ($null -eq $entry) {
            throw "Package '$PackagePath' does not contain expected entry '$EntryPath'."
        }

        $stream = $entry.Open()
        try {
            $sha256 = [System.Security.Cryptography.SHA256]::Create()
            try {
                return [System.Convert]::ToHexString($sha256.ComputeHash($stream)).ToLowerInvariant()
            }
            finally {
                $sha256.Dispose()
            }
        }
        finally {
            $stream.Dispose()
        }
    }
    finally {
        $archive.Dispose()
    }
}

function Get-GlobalPackagesPath {
    $output = @(& dotnet nuget locals global-packages --list 2>&1)
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet nuget locals global-packages --list failed with exit code $LASTEXITCODE. Output: $($output -join [Environment]::NewLine)"
    }

    $line = @($output | ForEach-Object { $_.ToString() } |
        Where-Object { $_ -match '^\s*global-packages:\s*(.+?)\s*$' } |
        Select-Object -First 1)[0]
    if ([string]::IsNullOrWhiteSpace($line) -or $line -notmatch '^\s*global-packages:\s*(.+?)\s*$') {
        throw "Could not determine the global NuGet packages directory from: $($output -join [Environment]::NewLine)"
    }

    return [System.IO.Path]::GetFullPath($Matches[1].Trim())
}

function Clear-CurrentEtherPackagesFromGlobalCache {
    param([Parameter(Mandatory)][string]$GlobalPackagesPath)

    $globalRoot = [System.IO.Path]::GetFullPath($GlobalPackagesPath)
    $separatorChars = [char[]]@([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    $globalRoot = $globalRoot.TrimEnd($separatorChars)
    $globalPrefix = $globalRoot + [System.IO.Path]::DirectorySeparatorChar
    $packageIds = @(
        'ether.designsystem.foundation',
        'ether.designsystem.controls',
        'ether.designsystem.interactions'
    )

    foreach ($packageId in $packageIds) {
        $packagePath = [System.IO.Path]::GetFullPath((Join-Path $globalRoot (Join-Path $packageId $packageVersion)))
        if (-not $packagePath.StartsWith($globalPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to remove package cache path outside global packages directory: '$packagePath'."
        }

        if (-not (Test-Path -LiteralPath $packagePath)) {
            Write-Host "Global package cache entry is already absent: $packagePath"
            continue
        }

        try {
            Remove-Item -LiteralPath $packagePath -Recurse -Force -ErrorAction Stop
        }
        catch {
            throw "Unable to remove cached Ether package '$packagePath': $($_.Exception.Message)"
        }

        if (Test-Path -LiteralPath $packagePath) {
            throw "Cached Ether package directory remains after removal attempt: '$packagePath'."
        }

        Write-Host "Removed cached Ether package: $packagePath"
    }
}

function Assert-RestoredControlsPackageMatchesLocalFeed {
    param(
        [Parameter(Mandatory)][string]$Variant,
        [Parameter(Mandatory)][string]$ProjectRoot,
        [Parameter(Mandatory)][string]$GlobalPackagesPath
    )

    $assetsPath = Join-Path $ProjectRoot 'obj\project.assets.json'
    if (-not (Test-Path -LiteralPath $assetsPath -PathType Leaf)) {
        throw "$Variant restore did not produce '$assetsPath'."
    }

    $assets = Get-Content -LiteralPath $assetsPath -Raw | ConvertFrom-Json
    $libraryKey = "Ether.DesignSystem.Controls/$packageVersion"
    $library = $assets.libraries.PSObject.Properties[$libraryKey].Value
    if ($null -eq $library) {
        throw "$Variant assets file did not resolve '$libraryKey'."
    }

    $target = @($assets.targets.PSObject.Properties |
        Where-Object { $_.Name -like 'net8.0-windows10.0.19041*' } |
        Select-Object -First 1)[0]
    if ($null -eq $target) {
        throw "$Variant assets file has no net8.0-windows10.0.19041 target."
    }

    $targetLibrary = $target.Value.PSObject.Properties[$libraryKey].Value
    $libraryAsset = @($targetLibrary.compile.PSObject.Properties.Name |
        Where-Object { $_ -like 'lib/*/Ether.DesignSystem.Controls.dll' } |
        Select-Object -First 1)[0]
    if ([string]::IsNullOrWhiteSpace($libraryAsset)) {
        throw "$Variant assets file has no compile asset for '$libraryKey'."
    }

    $restoredDll = Join-Path (Join-Path $GlobalPackagesPath $library.path) ($libraryAsset -replace '/', '\\')
    if (-not (Test-Path -LiteralPath $restoredDll -PathType Leaf)) {
        throw "$Variant resolved Controls assembly is absent from the global package directory: '$restoredDll'."
    }

    $packagePath = Join-Path $localFeed "Ether.DesignSystem.Controls.$packageVersion.nupkg"
    if (-not (Test-Path -LiteralPath $packagePath -PathType Leaf)) {
        throw "Current-run Controls package was not found in local feed: '$packagePath'."
    }

    $expectedHash = Get-ZipEntrySha256 $packagePath $libraryAsset
    $restoredHash = (Get-FileHash -LiteralPath $restoredDll -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($restoredHash -cne $expectedHash) {
        throw "$Variant resolved Controls DLL does not match this run's local package. Restored=$restoredHash; Packed=$expectedHash."
    }

    Write-Host "$Variant verified current-run Controls package: $libraryAsset SHA-256=$restoredHash"
}

function Invoke-ExternalConsumerVariant {
    param(
        [Parameter(Mandatory)][string]$Variant,
        [Parameter(Mandatory)][bool]$IncludeWindowsAppSdk
    )

    $projectRoot = New-ExternalConsumerProject $Variant $IncludeWindowsAppSdk
    $projectPath = Join-Path $projectRoot 'ExternalConsumer.csproj'
    $restoreLog = Join-Path $projectRoot 'restore.log'
    $buildLog = Join-Path $projectRoot 'build.log'

    Clear-CurrentEtherPackagesFromGlobalCache $globalPackagesPath
    # Do not pass --packages here. WinUI's XAML markup compiler relies on the global-package
    # layout for WindowsAppSDK projections; an isolated package directory causes WMC9999 when
    # resolving Microsoft.InteractiveExperiences.Projection. Clearing only Ether's current
    # package versions retains that layout while forcing restore to extract this run's packages.
    $restoreExitCode = Invoke-DotNet @('restore', $projectPath, '--configfile', (Join-Path $temporaryRoot 'nuget.config')) $restoreLog
    $result = [ordered]@{ Variant = $Variant; RestoreExitCode = $restoreExitCode; PackageVerification = 'not-run'; BuildExitCode = -1; Runtime = 'not-run'; ProjectRoot = $projectRoot }

    if ($restoreExitCode -ne 0) {
        Write-Host "$Variant failed (restore=$restoreExitCode). Logs: $restoreLog" -ForegroundColor Yellow
        return [pscustomobject]$result
    }

    try {
        Assert-RestoredControlsPackageMatchesLocalFeed $Variant $projectRoot $globalPackagesPath
        $result.PackageVerification = 'passed'
    }
    catch {
        $result.PackageVerification = 'failed'
        Write-Host "$Variant failed package freshness verification: $($_.Exception.Message)" -ForegroundColor Yellow
        return [pscustomobject]$result
    }

    $result.BuildExitCode = Invoke-DotNet @('build', $projectPath, '-c', $Configuration, $platformProperty, '--no-restore') $buildLog
    if ($result.BuildExitCode -ne 0) {
        Write-Host "$Variant failed (restore=$restoreExitCode, build=$($result.BuildExitCode)). Logs: $restoreLog ; $buildLog" -ForegroundColor Yellow
        return [pscustomobject]$result
    }

    if ($SkipRuntimeSmoke) {
        $result.Runtime = 'skipped'
        Write-Host "$Variant restore + x64 build passed; runtime smoke skipped."
        return [pscustomobject]$result
    }

    $exe = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'bin') -Filter 'ExternalConsumer.exe' -Recurse |
        Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1)[0]
    if ($null -eq $exe) {
        throw "$Variant build passed but its executable was not found under $projectRoot\bin."
    }

    $markerPath = Join-Path $projectRoot 'runtime-result.json'
    $previousMarkerPath = [Environment]::GetEnvironmentVariable('ETHER_EXTERNAL_CONSUMER_RESULT_PATH', 'Process')
    $process = $null
    try {
        [Environment]::SetEnvironmentVariable('ETHER_EXTERNAL_CONSUMER_RESULT_PATH', $markerPath, 'Process')
        $process = Start-Process -FilePath $exe.FullName -WorkingDirectory $exe.DirectoryName -WindowStyle Hidden -PassThru
        if (-not $process.WaitForExit(30000)) {
            throw "$Variant application did not exit within 30 seconds."
        }
        Assert-RuntimeMarker $markerPath
        $result.Runtime = 'passed'
        Write-Host "$Variant restore + x64 build + runtime marker passed."
    }
    finally {
        if ($null -ne $process -and -not $process.HasExited) {
            Stop-Process -Id $process.Id -Force
        }
        [Environment]::SetEnvironmentVariable('ETHER_EXTERNAL_CONSUMER_RESULT_PATH', $previousMarkerPath, 'Process')
    }

    return [pscustomobject]$result
}

if ($temporaryRoot.StartsWith($repoRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "External consumer root must be outside the repository: $temporaryRoot"
}
if (-not $localFeed.StartsWith($artifactsRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to clear a local feed outside artifacts: $localFeed"
}

try {
    if (Test-Path -LiteralPath $localFeed) {
        Remove-Item -LiteralPath $localFeed -Recurse -Force
    }
    & (Join-Path $PSScriptRoot 'Pack-PreviewPackages.ps1') -Configuration $Configuration -OutputDirectory $localFeed
    if ($LASTEXITCODE -ne 0) {
        throw "Pack-PreviewPackages.ps1 failed with exit code $LASTEXITCODE."
    }

    New-Item -ItemType Directory -Path $temporaryRoot -Force | Out-Null
    Write-Host "External consumer root (outside repository): $temporaryRoot"
    $globalPackagesPath = Get-GlobalPackagesPath
    Write-Host "Global NuGet packages directory: $globalPackagesPath"
    $variantAError = $null
    try {
        $variantA = Invoke-ExternalConsumerVariant 'VariantA-EtherOnly' $false
    }
    catch {
        $variantAError = $_
        $variantA = [pscustomobject]@{ Variant = 'VariantA-EtherOnly'; RestoreExitCode = -1; PackageVerification = 'failed'; BuildExitCode = -1; Runtime = 'failed'; ProjectRoot = $temporaryRoot }
        Write-Warning "Variant A terminated unexpectedly: $($_.Exception.Message)"
    }

    $variantBError = $null
    try {
        $variantB = Invoke-ExternalConsumerVariant 'VariantB-ExplicitWindowsAppSDK' $true
    }
    catch {
        $variantBError = $_
        $variantB = [pscustomobject]@{ Variant = 'VariantB-ExplicitWindowsAppSDK'; RestoreExitCode = -1; PackageVerification = 'failed'; BuildExitCode = -1; Runtime = 'failed'; ProjectRoot = $temporaryRoot }
        Write-Warning "Variant B terminated unexpectedly: $($_.Exception.Message)"
    }
    Write-Host "External consumer summary: A restore=$($variantA.RestoreExitCode), package=$($variantA.PackageVerification), build=$($variantA.BuildExitCode), runtime=$($variantA.Runtime); B restore=$($variantB.RestoreExitCode), package=$($variantB.PackageVerification), build=$($variantB.BuildExitCode), runtime=$($variantB.Runtime)"

    if ($null -ne $variantAError -or $variantA.RestoreExitCode -ne 0 -or $variantA.PackageVerification -ne 'passed' -or $variantA.BuildExitCode -ne 0 -or $variantA.Runtime -notin @('passed', 'skipped')) {
        throw 'Variant A (Ether packages only) must restore, verify the current-run package, and build successfully; see retained logs above.'
    }

    if ($null -ne $variantBError -or $variantB.RestoreExitCode -ne 0 -or $variantB.PackageVerification -ne 'passed' -or $variantB.BuildExitCode -ne 0 -or $variantB.Runtime -notin @('passed', 'skipped')) {
        throw 'Variant B (explicit Microsoft.WindowsAppSDK) must restore, verify the current-run package, and build successfully; see retained logs above.'
    }
    $succeeded = $true
}
finally {
    if ($succeeded) {
        Remove-Item -LiteralPath $temporaryRoot -Recurse -Force
        Write-Host "External consumer verification passed; cleaned temporary root $temporaryRoot"
    }
    else {
        Write-Warning "External consumer verification failed; retained temporary root for diagnosis: $temporaryRoot"
    }
}
