# Integrating the Ether Design System (Internal Teams)

This document is for internal company WinUI 3 application teams. The current release line
is the preview `0.1.0-preview.7`, distributed only through private GitHub Packages; do not
expose the package or PAT configuration in an external repository.

## Verified support scope

- Target framework: `net8.0-windows10.0.19041.0`
- Minimum Windows platform version: `10.0.17763.0`
- Architecture: `x64` only
- Hosting: both unpackaged and packaged (MSIX **build/output**) forms have been verified;
  MSIX **installation and runtime** have not yet been verified and remain incomplete.
- Packages: `Ether.DesignSystem.Foundation`, `Ether.DesignSystem.Controls`, and
  `Ether.DesignSystem.Interactions`, all currently at `0.1.0-preview.7`.

External consumer verification has proven that referencing only the three Ether packages
above is sufficient; you do **not** need to explicitly reference `Microsoft.WindowsAppSDK`
just to use Ether. If your application already references it, that is also supported.

## 1. Configuring GitHub Packages access

### Create and store a PAT

GitHub Packages requires authentication even within the same organization. Prepare a PAT
with `read:packages` permission for restore, and keep it as a secret; do not write the PAT
in plaintext into the repository, `nuget.config`, or logs.

The typical path to create a GitHub classic PAT is: GitHub **Settings** →
**Developer settings** → **Personal access tokens** → **Tokens (classic)** →
**Generate new token (classic)**, and check `read:packages`. If your organization has SSO
enabled, you will also need to authorize the token according to your organization's policy.

> Note: the repository's external-consumer script verifies package restore/build/runtime
> and resource merging, but it does not connect to real GitHub Packages and does not
> replace your organization's PAT issuance or SSO process. The token creation UI and SSO
> steps above should follow your organization's current GitHub policy.

Set the credentials in the current PowerShell session (do not paste the real token into a
script file):

```powershell
$env:GITHUB_PACKAGES_USERNAME = '<your GitHub username>'
$env:GITHUB_PACKAGES_PAT = '<a PAT with read:packages>'
```

Create or update `nuget.config` at the root of your solution. Replace `<PACKAGE_OWNER>`
with **the GitHub organization or user that publishes these packages**, not the repository
name; get the exact value from the release announcement or the package administrator.

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="github-ether"
         value="https://nuget.pkg.github.com/<PACKAGE_OWNER>/index.json"
         protocolVersion="3" />
    <add key="nuget.org"
         value="https://api.nuget.org/v3/index.json"
         protocolVersion="3" />
  </packageSources>
  <packageSourceCredentials>
    <github-ether>
      <add key="Username" value="%GITHUB_PACKAGES_USERNAME%" />
      <add key="ClearTextPassword" value="%GITHUB_PACKAGES_PAT%" />
    </github-ether>
  </packageSourceCredentials>
</configuration>
```

`ClearTextPassword` here holds an environment-variable reference, not the token itself. Do
not commit the substituted, real PAT to Git. The real GitHub feed, package owner, and
organization authentication have not yet been exercised end-to-end by this repository's
external-consumer script; it verifies package consumption against a local feed instead. For
your first integration, therefore, defer to the owner and organization authentication
requirements provided by the publishing team.

## 2. A minimal WinUI 3 application

The project properties, three package references, `App.xaml` resource merging, and control
XAML below are all distilled from the successful Variant A of
[`scripts/Verify-ExternalConsumer.ps1`](../../scripts/Verify-ExternalConsumer.ps1). That
variant completes restore, x64 build, and runtime marker verification using **only the three
Ether packages**.

After creating a project from the WinUI 3 application template, adjust the project file to
the following key configuration (keep any other settings the template already requires):

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
    <TargetPlatformMinVersion>10.0.17763.0</TargetPlatformMinVersion>
    <Platforms>x64</Platforms>
    <PlatformTarget>x64</PlatformTarget>
    <RuntimeIdentifiers>win-x64</RuntimeIdentifiers>
    <UseWinUI>true</UseWinUI>
    <ApplicationManifest>app.manifest</ApplicationManifest>
    <EnableMsixTooling>true</EnableMsixTooling>
    <WindowsPackageType>None</WindowsPackageType>
    <SelfContained>false</SelfContained>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Ether.DesignSystem.Foundation" Version="0.1.0-preview.7" />
    <PackageReference Include="Ether.DesignSystem.Controls" Version="0.1.0-preview.7" />
    <PackageReference Include="Ether.DesignSystem.Interactions" Version="0.1.0-preview.7" />
    <Manifest Include="$(ApplicationManifest)" />
  </ItemGroup>
</Project>
```

`UseWinUI` and `EnableMsixTooling` are part of the verified consumer project and should be
kept even though this example is an unpackaged host. The `app.manifest` at the project root
can use the same content as the verification project:

```xml
<?xml version="1.0" encoding="utf-8"?>
<assembly manifestVersion="1.0" xmlns="urn:schemas-microsoft-com:asm.v1">
  <assemblyIdentity version="1.0.0.0" name="Contoso.Inventory.app" />
  <trustInfo xmlns="urn:schemas-microsoft-com:asm.v2">
    <security><requestedPrivileges xmlns="urn:schemas-microsoft-com:asm.v3"><requestedExecutionLevel level="asInvoker" uiAccess="false" /></requestedPrivileges></security>
  </trustInfo>
  <compatibility xmlns="urn:schemas-microsoft-com:compatibility.v1"><application><supportedOS Id="{8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a}" /></application></compatibility>
</assembly>
```

### Resource dictionaries you must merge

Merge both the standard WinUI resources and Ether's resource entry point in `App.xaml`.
Omitting the second one causes control styles, resources, or packaged resource paths to
fail to resolve correctly.

```xml
<Application
    x:Class="Contoso.Inventory.App"
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
```

### Using the controls

Use the newly public namespace in your page or window XAML:

```xml
<Window
    x:Class="Contoso.Inventory.MainWindow"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:ether="using:Ether.DesignSystem.Controls"
    Title="Ether consumer">
    <!-- IsTabStop="True" makes this ScrollViewer a focus target, so a click on empty space lands
         focus here (invisibly) and blurs a focused EtherInput. Direct clicks on controls still
         focus them. Without it, WinUI leaves focus on the input because a press on a non-focusable
         panel has no focus target. See the "focused EtherInput doesn't blur" row in Troubleshooting. -->
    <ScrollViewer IsTabStop="True" UseSystemFocusVisuals="False">
        <StackPanel Padding="24" Spacing="12">
            <ether:EtherButton
                Variant="Tertiary"
                Size="Small"
                Content="Hello from external XAML">
                <ether:EtherButton.LeftIcon>
                    <SymbolIcon Symbol="Accept" />
                </ether:EtherButton.LeftIcon>
            </ether:EtherButton>

            <ether:EtherProgressBar
                Minimum="0" Maximum="100" Value="65"
                Title="Download"
                ShowTitle="True" ShowValue="True" />

            <ether:EtherInput
                Text="External text"
                PlaceholderText="External placeholder" />
        </StackPanel>
    </ScrollViewer>
</Window>
```

`xmlns:ether="using:Ether.DesignSystem.Controls"` must match the text above exactly; do not
use the old `EtherSandbox.Controls` namespace.

The corresponding minimal startup code is below. The verification script uses the same
`MainWindow` creation and activation path, with only extra wrapping for runtime result
marker logic.

```csharp
// App.xaml.cs
using Microsoft.UI.Xaml;

namespace Contoso.Inventory;

public partial class App : Application
{
    private Window? _window;

    public App() => InitializeComponent();

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
```

```csharp
// MainWindow.xaml.cs
using Microsoft.UI.Xaml;

namespace Contoso.Inventory;

public sealed partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();
}
```

Then run:

```powershell
dotnet restore --configfile .\nuget.config
dotnet build -c Debug -p:Platform=x64
```

## 3. Troubleshooting

| Symptom | Common cause | Resolution |
| --- | --- | --- |
| `401` / `403` | PAT is missing, expired, lacks `read:packages`, or organization SSO is not authorized | Reset the environment variables and confirm the PAT has `read:packages` permission; if your organization uses SSO, authorize the token per your organization's requirements. |
| `404` | The owner segment of the GitHub Packages source is wrong, or the package ID / version does not exist | Verify the owner in `https://nuget.pkg.github.com/<PACKAGE_OWNER>/index.json`, as well as the three package IDs and published version. Do not put the repository name in the owner position. |
| `NU1301` | The feed is unreachable: blocked by network/proxy policy, or the source URL is wrong | Confirm the feed is reachable from a browser or your organization's network environment; verify the URL, proxy, and certificate policy in `nuget.config`, then restore again. |
| After a repackage, the consumer still appears to run the old code; `restore` succeeds but the build reports errors such as an unresolved old namespace | NuGet caches by **package ID + version number**; reusing an already-published version silently hits the old package | The correct fix is to publish a new version and upgrade the version in `PackageReference`. For local troubleshooting only, you can locate the global directory with `dotnet nuget locals global-packages --list` and delete **only that package ID / old version's** directory; do not clear the entire NuGet cache. |
| `XamlParseException`, or the `ms-appx:///Ether.DesignSystem.Controls/...` resource cannot be found | `DesignSystem.xaml` was not merged, or the URI is misspelled | Put both `XamlControlsResources` and `ms-appx:///Ether.DesignSystem.Controls/Themes/DesignSystem.xaml` into `App.xaml`'s `ResourceDictionary.MergedDictionaries`, then clean and rebuild. |
| `WMC0001: Unknown type 'EtherButton'` | `xmlns` uses the wrong namespace, or the package did not restore correctly | Use `xmlns:ether="using:Ether.DesignSystem.Controls"`; confirm restore succeeded and the `Ether.DesignSystem.Controls` version is present in the assets file. |
| A focused `EtherInput` doesn't blur when you click empty space (its focus border stays) | Standard WinUI `TextBox` behavior — this is **not** Ether-specific: a pointer press on a non-focusable panel (`Grid`/`Border`/`StackPanel`) has no focus target, so focus stays on the input | Make the host itself a focus target: add `IsTabStop="True" UseSystemFocusVisuals="False"` to the root `ScrollViewer` (or wrap content in a `ContentControl IsTabStop="True" UseSystemFocusVisuals="False"`), so a click on empty space lands focus there and blurs the input. Direct clicks on real controls still focus them; the only cost is one extra, invisible Tab stop. |
| An `EtherButton` looks taller than expected when placed beside a taller control (e.g. an `EtherIntelligenceButton`) in the same horizontal row | A `StackPanel` sizes the row to its tallest child; the button's `VerticalAlignment` then decides whether it fills that height | Fixed in the control (it now carries `VerticalAlignment="Center"`), so upgrade to the latest package. If you pin an older version, set `VerticalAlignment="Center"` on the button, or keep the tall control in its own row. |
| Restore succeeds, but the package installed is not the Ether package (e.g. a public nuget.org package with the same name, or the version content does not match expectations) | `nuget.config` has both the `github-ether` and `nuget.org` sources present; if either source fails to resolve or the search order behaves unexpectedly, NuGet may satisfy the same package ID from **the other source** | Check in `project.assets.json` or the restore log whether each Ether package ID actually resolved from the `github-ether` source (the `source` field); the long-term fix is to add [package source mapping](https://learn.microsoft.com/en-us/nuget/consume-packages/package-source-mapping) to `nuget.config`, explicitly pinning `Ether.DesignSystem.*` to `github-ether` and the remaining package IDs to `nuget.org`, to avoid one source unexpectedly satisfying the other source's package name. |

## 4. Known boundaries

- Only `x64` is supported. Build your application as `x64`; do not treat x86 or ARM64 as
  supported targets.
- The current version is a preview; the public API, styles, and resource contracts may still
  change. Before upgrading, read the corresponding release notes and regression-test key
  pages in your application.

### 4.1 Properties inherited from the base class but not consumed by Ether templates

The properties in the table below are all **inherited from a WinUI base class, publicly
exposed, compile cleanly, and run without error** — but Ether's custom templates do not
reference them at all, so setting them **produces no visible effect on screen** (for
`EtherDropdown.Text` / `IsEditable`, not even functionally). This is not a one-sided claim
made only by the documentation: `scripts/Verify-UnsupportedProperties.ps1` treats
`scripts/UnsupportedProperties.psd1` (the single source of truth) as authoritative and
asserts, property by property, that the corresponding template file genuinely does not
consume that property; this table is also generated from and validated against that same
file, so the two can never drift apart.

These properties currently **do not** throw an exception or give a runtime warning when
set — they simply display nothing, rather than displaying something wrong. `EtherCheckbox` /
`EtherRadioButton` are deliberately two-state controls: `IsChecked=null` is coerced to
`false`, and `IsThreeState=true` is coerced back to `false`; do not rely on an Indeterminate
state. The table below lists only inherited properties that are genuinely unsupported or
silently ineffective.

<!-- UNSUPPORTED-PROPERTIES:START -->
| Control | Property | Inherited from | Workaround |
| --- | --- | --- | --- |
| `EtherDropdown` | `Header` | `ComboBox` | Use an external `TextBlock` or form container to place a label above the control. |
| `EtherDropdown` | `HeaderTemplate` | `ComboBox` | Same as `Header`: build the label content outside the control. |
| `EtherDropdown` | `Description` | `ComboBox` | Place another `TextBlock` below the control as descriptive text. |
| `EtherDropdown` | `PlaceholderForeground` | `ComboBox` | `PlaceholderText` can be used for the unselected state; this color property is not bound in the template. If you need a fixed placeholder color, use an external placeholder visual. |
| `EtherDropdown` | `Text` | `ComboBox` | `EtherDropdown` is a **selection-type control** (the Figma source has no editable combo-box variant) and does not offer an editable mode. If you need free-text input, use `EtherInput` instead, or style a native editable `ComboBox` yourself. |
| `EtherDropdown` | `IsEditable` | `ComboBox` | Same as `Text`: editable mode is not supported by design. Setting it to `true` will not make the control accept input — the template lacks the `"EditableText"` part that `ComboBox` internally requires, so this switch silently has no effect. |
| `EtherInput` | `Header` | `TextBox` | This is an existing design decision (see the `EtherInput.cs` comment: "does not add ... header/description slots"), not an oversight. Wrap `EtherInput` with an external label/description layout. |
| `EtherInput` | `HeaderTemplate` | `TextBox` | Same as `Header`. |
| `EtherInput` | `Description` | `TextBox` | Same as `Header`: place another `TextBlock` below the control. |
<!-- UNSUPPORTED-PROPERTIES:END -->

> Note: `ToggleSwitch` in the currently used `Microsoft.WindowsAppSDK 2.3.1` **does not
> have** a `Description` property (confirmed via reflection: `Microsoft.UI.Xaml.Controls.
> ToggleSwitch` only has `Header` / `HeaderTemplate`, no `Description`), so the table above
> does not list an `EtherSwitch.Description` row — it is not even an "existing but
> ineffective property"; it is a property that does not exist at all.
- `Ether.DesignSystem.Controls.Primitives.HandContentControl` is public because XAML
  resource resolution requires it, but it is a supporting type for template implementation,
  not a supported design-system control contract; use the named `Ether*` controls instead.
- Controls and Foundation are used at the same version on the current preview release line;
  upgrade both together. The Interactions package should also stay aligned with the version
  combination given in the release announcement.
- Beyond the 9 properties in the table above, each control also inherits a large number of
  WinUI base-class properties. Most of these are indeed appearance-related properties
  (`Background` / `BorderBrush` / `BorderThickness` / `CornerRadius` / `Padding` /
  `Foreground` / `FontSize`, etc.) — the design system **deliberately does not allow these to
  be overridden**: control templates reference design tokens directly instead of
  `{TemplateBinding ...}`, so that every consumer sees a consistent visual language;
  "setting it has no effect" is a feature, not an omission. But **that is not the whole
  story**: some properties' templates actually do use `{TemplateBinding ...}` (the visual
  probe simply failed to detect a pixel difference under its test conditions — for example,
  `EtherInput.PlaceholderText` only renders when the control is empty and unfocused), or are
  handled by the WinUI base class outside the template (for example `TextBox`'s
  `AcceptsReturn` / `IsReadOnly` / `CharacterCasing`) — these properties actually **do take
  effect** and should not be treated as "unsupported". This distinction (together with a
  small number of confirmed-ineffective low-level platform properties, such as `Clip` /
  `CompositeMode`) is fully recorded in the `AcknowledgedSilent` list of
  `scripts/UnsupportedProperties.psd1`, filed into five categories — `design-system-owned`
  (confirmed unbound, deliberately locked), `consumed-visually-stable` (confirmed bound, the
  probe just did not detect a pixel difference), `behavioral` (base-class behavior takes
  effect, cannot be tested by pixel difference), `platform-noop` (a low-level platform DP
  with no consumer expectation), and `needs-review` (not yet certain, pending manual review)
  — each with an attached `Reason`. The `local-runtime` gate
  `scripts/Verify-SilentPropertyCoverage.ps1` automatically cross-checks every run's runtime
  evidence in both directions (a newly appearing, unregistered silently-ineffective property
  fails the gate), and additionally performs a `TemplateBinding` cross-check to make sure the
  `design-system-owned` / `consumed-visually-stable` labels themselves are not mislabeled.

## 5. Per-control markup reference

Below, each control has a copy-paste-ready markup sample, taken from the `*Proof` elements
in [`tests/Ether.DesignSystem.ConsumerFixtures/Unpackaged/MainWindow.xaml`](../../tests/Ether.DesignSystem.ConsumerFixtures/Unpackaged/MainWindow.xaml)
— this is the exact markup shape actually exercised by the out-of-repo consumer verification
(A1/A4), and it matches the corresponding page in `samples/Ether.DesignSystem.Gallery`. The
namespace prefix is omitted in the samples; after declaring
`xmlns:ether="using:Ether.DesignSystem.Controls"` per §2, just replace `controls:` with your
own prefix.

> **Want the full picture for one control?** Each control also has a dedicated guide —
> overview, the consumer API (all Ether-specific members plus the common inherited ones), binding/wiring, and
> what the design system owns — under [`docs/consumers/components/`](components/README.md). This
> section stays as the quick copy-paste markup reference.

**EtherButton** (matches Gallery `Views/Controls/ButtonPage.xaml`):
```xml
<ether:EtherButton Content="Package button"
                   AutomationProperties.Name="Package button" />
<ether:EtherButton Style="{StaticResource EtherButtonSecondary}"
                   Content="Secondary package button"
                   AutomationProperties.Name="Secondary package button" />
```

**Bind data:** `Content`/`IsEnabled` are plain WinUI `ContentControl`/`Control` properties — a
normal `{x:Bind}` at `Mode=OneWay` is enough; there is no user-editable "selection" state to push
back:
```xml
<ether:EtherButton Content="{x:Bind ViewModel.SaveLabel, Mode=OneWay}"
                   IsEnabled="{x:Bind ViewModel.CanSave, Mode=OneWay}"
                   Style="{StaticResource EtherButtonPrimary}"/>
```

**Wire an action:** `EtherButton` is a `ButtonBase` subclass, so the native `Command`/
`CommandParameter` pair works with no event glue:
```xml
<ether:EtherButton Content="Save"
                   Command="{x:Bind ViewModel.SaveCommand}"
                   CommandParameter="{x:Bind ViewModel.CurrentItem, Mode=OneWay}"
                   Style="{StaticResource EtherButtonPrimary}"/>
```
Proven at `tests/Ether.DesignSystem.ConsumerFixtures/RuntimeVerification.R2.cs:388-398`
(`VerifyButtonCommand`): `Command`/`CommandParameter` fire exactly once per click, with the
expected parameter.

**EtherProgressBar** (`Views/DataDisplay/ProgressBarPage.xaml`):
```xml
<!-- Leave ValueFormat unset for the built-in synced percent of [Minimum, Maximum]. -->
<ether:EtherProgressBar HorizontalAlignment="Stretch"
                        Title="Package download"
                        Value="65"
                        AutomationProperties.Name="Package download progress" />
```

The value label always reflects the live `Value` — there is no way to set a static string that
desyncs from it. A consumer chooses whether to show it (`ShowValue`) and, optionally, how to format
it: `ValueFormat`, a composite format string (for example `"{0:0}%"`) applied to `Value`; or a
`ValueContentConverter` (`IValueConverter`) for full control, which takes precedence over
`ValueFormat`. With neither set, the control formats `Value` as a percent of
[`Minimum`, `Maximum`].

**Bind data:** `Value`, `Title`, `ShowTitle`, `ShowValue` are all `OneWay`-friendly display
properties; `ValueFormat` is a composite format string, so set it once rather than binding it
per-value (`EtherProgressBar.cs:60-93`):
```xml
<ether:EtherProgressBar Value="{x:Bind ViewModel.DownloadPercent, Mode=OneWay}"
                        Title="{x:Bind ViewModel.DownloadLabel, Mode=OneWay}"
                        ValueFormat="{}{0:0}%"
                        ShowTitle="True" ShowValue="True"
                        HorizontalAlignment="Stretch"/>
```

**Wire an action:** none — no user interaction. You *can* observe code/binding-driven progress
changes via the inherited `ValueChanged` event (or the Interactions `ObserveRange` adapter).

**EtherCheckbox** (`Views/Controls/CheckboxPage.xaml`):
```xml
<ether:EtherCheckbox Content="Package checkbox"
                     AutomationProperties.Name="Package checkbox" />
```

**Bind data:** `IsChecked` is `bool?` and supports `TwoWay`. `EtherCheckbox` is deliberately
two-state (§4.1: `null` coerces to `false`, `IsThreeState=true` is rejected), so bind a plain
`bool` VM property unless you intentionally want to read the transient `null`:
```xml
<ether:EtherCheckbox Content="Send me updates"
                     IsChecked="{x:Bind ViewModel.SubscribeToUpdates, Mode=TwoWay}"/>
```
Proven at `R2.cs:68-76` (`VerifyTwoWayBindings`).

**Wire an action:** `EtherCheckbox` is a `ToggleButton`, so `Command`/`CommandParameter` fire on
user activation (a click/toggle), not on a programmatic/bound `IsChecked` change — use
`Checked`/`Unchecked` to observe every state change:
```xml
<ether:EtherCheckbox Content="Send me updates"
                     Command="{x:Bind ViewModel.ToggleSubscriptionCommand}"
                     CommandParameter="{x:Bind ViewModel.SubscriptionId}"/>
```
Proven at `R2.cs:379, 400-412` (`VerifyToggleCommand`). Most apps only need one of `IsChecked`
TwoWay or `Command` — add `Command` only when something besides the bound state (telemetry, a
save) must run on toggle.

**EtherRadioButton** (`Views/Controls/RadioButtonPage.xaml`):
```xml
<ether:EtherRadioButton GroupName="package-radio"
                        Content="Package radio"
                        AutomationProperties.Name="Package radio button" />
```

**Bind data:** `IsChecked` TwoWay (same two-state coercion as `EtherCheckbox`) plus `GroupName` for
mutual exclusion:
```xml
<ether:EtherRadioButton GroupName="delivery-speed" Content="Standard"
                        IsChecked="{x:Bind ViewModel.IsStandardDelivery, Mode=TwoWay}"/>
<ether:EtherRadioButton GroupName="delivery-speed" Content="Express"
                        IsChecked="{x:Bind ViewModel.IsExpressDelivery, Mode=TwoWay}"/>
```
Proven at `R2.cs:78-86` (TwoWay) and group-name mutual exclusion (`RadioButtonVerification.
GroupNameMutualExclusionVerified`).

**Wire an action:** `EtherRadioButton`'s UIA actuation is `SelectionItem.Select()`, not `Toggle()`,
but it still fires `Command`/`CommandParameter` on user activation (including re-activating an
already-selected radio), not on a programmatic `IsChecked` change:
```xml
<ether:EtherRadioButton GroupName="delivery-speed" Content="Express"
                        Command="{x:Bind ViewModel.SelectDeliverySpeedCommand}"
                        CommandParameter="Express"/>
```
Proven at `R2.cs:384, 419-431` (`VerifySelectionItemCommand`).

**EtherInput** (`Views/Controls/InputPage.xaml`):
```xml
<ether:EtherInput PlaceholderText="Package input"
                  HorizontalAlignment="Stretch"
                  AutomationProperties.Name="Package input" />
```

**Bind data:** `Text` is `string` and supports `TwoWay`:
```xml
<ether:EtherInput PlaceholderText="Search"
                  Text="{x:Bind ViewModel.SearchQuery, Mode=TwoWay}"
                  HorizontalAlignment="Stretch"/>
```
Proven at `R2.cs:88-96` (`VerifyTwoWayBindings`).

**Wire an action:** `EtherInput` is a `TextBox` and has no `Command`; bind the native
`TextChanged` event straight to a view-model method with `{x:Bind}` — no extra package required:
```xml
<ether:EtherInput PlaceholderText="Search"
                  Text="{x:Bind ViewModel.SearchQuery, Mode=TwoWay}"
                  TextChanged="{x:Bind ViewModel.OnQueryChanged}"/>
```
`OnQueryChanged` may take the standard `(object sender, TextChangedEventArgs e)` signature or no
parameters at all — `{x:Bind}` supports both event-handler shapes.

**EtherDropdown** (`Views/Controls/DropdownPage.xaml`):
```xml
<ether:EtherDropdown SelectedIndex="0"
                     HorizontalAlignment="Stretch"
                     AutomationProperties.Name="Package dropdown">
    <ComboBoxItem Content="10 Minutes"/>
    <ComboBoxItem Content="30 Minutes"/>
    <ComboBoxItem Content="1 Hour"/>
</ether:EtherDropdown>
```

**Bind data:** `ItemsSource` plus `SelectedItem` or `SelectedValue` (+ `SelectedValuePath`), all
`TwoWay`-capable — these are inherited `Selector`/`ComboBox` properties, not Ether-added ones:
```xml
<ether:EtherDropdown ItemsSource="{x:Bind ViewModel.DurationOptions}"
                     DisplayMemberPath="Label"
                     SelectedValuePath="Id"
                     SelectedValue="{x:Bind ViewModel.SelectedDurationId, Mode=TwoWay}"
                     HorizontalAlignment="Stretch"/>
```
Proven at `R2.cs:98-116` (`VerifyTwoWayBindings`, `SelectedItem`/`SelectedValue`). Configure
`SelectedValuePath` (or a stable ID on the item model) before treating `SelectedValue` as a
business identifier — see `src/Ether.DesignSystem.Interactions/README.md:9`.

**Wire an action:** no `Command`; bind the native `SelectionChanged` event to a view-model method:
```xml
<ether:EtherDropdown ItemsSource="{x:Bind ViewModel.DurationOptions}"
                     DisplayMemberPath="Label"
                     SelectionChanged="{x:Bind ViewModel.OnDurationChanged}"/>
```
The trigger only ever shows plain text (`DisplayMemberPath`/`ToString()`); a rich `ItemTemplate`
renders inside the open menu only, not on the closed trigger (`EtherDropdown.cs:42-43`).

**EtherSegmentedControl** (`Views/Controls/SegmentedControlPage.xaml`) — the recommended
segment item is `EtherSegmentRadioButton` (apply the `EtherSegment` style, inside an
`EtherSegmentPanel`): it has full hover / pressed / checked feedback, matching the Gallery's
behavior:
```xml
<ether:EtherSegmentedControl AutomationProperties.Name="Package segmented control">
    <ether:EtherSegmentPanel>
        <ether:EtherSegmentRadioButton Style="{StaticResource EtherSegment}"
                     GroupName="package-segment"
                     Content="A"
                     IsChecked="True"/>
        <ether:EtherSegmentRadioButton Style="{StaticResource EtherSegment}"
                     GroupName="package-segment"
                     Content="B"/>
    </ether:EtherSegmentPanel>
</ether:EtherSegmentedControl>
```
A plain `RadioButton` with the `EtherSegment` style applied still shows the pill background
for the checked state, but has no hover / pressed feedback.

**Bind data:** the data-driven contract — `ItemsSource` + `ItemTemplate`/`DisplayMemberPath` +
`SelectedIndex`/`SelectedItem`/`SelectedValue` (all `TwoWay`-capable). Inline
`EtherSegmentRadioButton` segments (above) remain fully supported and can be mixed across
different instances:
```xml
<ether:EtherSegmentedControl AutomationProperties.Name="Time range"
                             ItemsSource="{x:Bind ViewModel.Periods}"
                             DisplayMemberPath="Label"
                             SelectedItem="{x:Bind ViewModel.SelectedPeriod, Mode=TwoWay}"/>
```
Proven at `R2.cs:118-136` (TwoWay `SelectedValue`/`SelectedIndex`) and `R2.cs:213-224`
(`VerifyDataPaths`: `ItemsSource` generates one `EtherSegmentRadioButton` per item,
`DisplayMemberPath` shapes their content, and `SelectedItem`/`SelectedValue` stay synchronized).

**Wire an action:** `SelectionChanged` fires on every selection change, programmatic or
user-driven, and carries the old/new value:
```xml
<ether:EtherSegmentedControl ItemsSource="{x:Bind ViewModel.Periods}"
                             DisplayMemberPath="Label"
                             SelectionChanged="{x:Bind ViewModel.OnPeriodSelectionChanged}"/>
```
For the `ItemsSource`-generated path specifically (inline segments already had a native
`RadioButton.Command`), an optional `SelectionCommand`/`SelectionCommandParameter` pair is also
available, firing **only on user-initiated selection** — never on a programmatic
`SelectedIndex`/`SelectedItem`/`SelectedValue` assignment:
```xml
<ether:EtherSegmentedControl ItemsSource="{x:Bind ViewModel.Periods}"
                             DisplayMemberPath="Label"
                             SelectionCommand="{x:Bind ViewModel.SelectPeriodCommand}"/>
```
`SelectionCommandParameter`, when set, is passed instead of `SelectedValue`; execution is guarded
by `CanExecute`, mirroring `ButtonBase.Command` semantics (fires on the user's pick, not on a
programmatic state assignment). Proven at
`tests/Ether.DesignSystem.ConsumerFixtures/RuntimeVerification.R2.cs`
(`VerifySegmentedControlSelectionCommand`): exactly one execution with the expected parameter on a
user pick, zero additional executions on a programmatic `SelectedIndex` change, and zero
executions when `CanExecute` returns `false`.

**EtherTabNavigation** (`Views/Navigation/TabNavigationPage.xaml`):
```xml
<ether:EtherTabNavigation SelectedIndex="0" AutomationProperties.Name="Package tab navigation">
    <ether:EtherTabItem Content="Overview" Icon="Home"/>
    <ether:EtherTabItem Content="Details" Icon="Document"/>
    <ether:EtherTabItem Content="History" Icon="Clock"/>
</ether:EtherTabNavigation>
```

**Bind data:** `ItemsSource` + `SelectedIndex`/`SelectedItem`, both `TwoWay`-capable —
`EtherTabNavigation` is a thin `ListView` subclass, so these are the inherited `Selector`
properties:
```xml
<ether:EtherTabNavigation ItemsSource="{x:Bind ViewModel.Sections}"
                          SelectedIndex="{x:Bind ViewModel.SelectedSectionIndex, Mode=TwoWay}"/>
```
Proven at `R2.cs:168-186` (`SelectedIndex`/`SelectedItem` TwoWay) and `R2.cs:205-211`
(`VerifyDataPaths`: `ItemsSource` drives item count and selection resolves to the right item).
**Known limitation:** `Icon` is a per-container property on inline `EtherTabItem`
(`EtherTabItem.cs:28-41`); the `ItemsSource`-generated path has no
`PrepareContainerForItemOverride`, so data binding cannot populate the dedicated `EtherTabItem.Icon`
slot — render an icon inside a (non-string model) `ItemTemplate` instead; the built-in `Icon` slot is
inline-`EtherTabItem` only.

**Wire an action:** no `Command`; bind the native `SelectionChanged` event:
```xml
<ether:EtherTabNavigation ItemsSource="{x:Bind ViewModel.Sections}"
                          SelectionChanged="{x:Bind ViewModel.OnSectionSelectionChanged}"/>
```

**EtherIntelligenceButton** (`Views/Controls/IntelligenceButtonPage.xaml`):
```xml
<ether:EtherIntelligenceButton Content="Package intelligence"
                               AutomationProperties.Name="Package intelligence button"/>
```

**Icons:** `LeftIcon` / `RightIcon` are strongly-typed `IconElement` slots, exactly like `EtherButton`.
`LeftIcon` defaults to the built-in sparkles glyph; `RightIcon` defaults to none. Replace the leading
icon, add a trailing one, or clear either with `{x:Null}`:
```xml
<!-- Keep the sparkles, add a trailing chevron -->
<ether:EtherIntelligenceButton Content="Ask Ether">
    <ether:EtherIntelligenceButton.RightIcon>
        <FontIcon Glyph="&#xE76C;"/>
    </ether:EtherIntelligenceButton.RightIcon>
</ether:EtherIntelligenceButton>

<!-- No leading icon at all -->
<ether:EtherIntelligenceButton Content="Plain" LeftIcon="{x:Null}"/>
```
The slots collapse automatically when empty (driven by `LeftIconStates`/`RightIconStates`), so
layout stays tight whether or not an icon is present.

**Bind data:** same as `EtherButton` — `Content`/`IsEnabled`, `Mode=OneWay`.

**Wire an action:** `EtherIntelligenceButton` is also a `ButtonBase`; `Command`/`CommandParameter`
work identically:
```xml
<ether:EtherIntelligenceButton Content="Ask Ether"
                               Command="{x:Bind ViewModel.AskCommand}"/>
```
Proven at `R2.cs:378, 388-398` (`VerifyButtonCommand`).

**EtherSteeringBar** (`Views/Controls/SteeringBarPage.xaml`):
```xml
<!-- Leave ValueFormat unset: the value label shows the built-in percent of
     [Minimum, Maximum] and stays in sync as Value changes. -->
<ether:EtherSteeringBar HorizontalAlignment="Stretch"
                        Title="Package playback"
                        Value="65"
                        AutomationProperties.Name="Package steering bar"/>
```

**The value label** always reflects the live `Value` — there is no way to set a static string that
desyncs from it. It resolves in this order: a `ValueContentConverter` (an `IValueConverter`) formats
the live `Value`, if set; otherwise `ValueFormat`, a composite format string (for example
`"{0:0}%"`) applied to `Value`, if set; otherwise the built-in percent. To customize the text with
full control (not just a format string), set a `ValueContentConverter` — this mirrors
`Slider.ThumbToolTipValueConverter`. The converter's value is `Value` and its parameter is the
control (so it can read `Minimum`/`Maximum`):
```xml
<Page.Resources><local:DecibelConverter x:Key="DecibelConverter"/></Page.Resources>
...
<ether:EtherSteeringBar Value="{x:Bind ViewModel.Playback, Mode=TwoWay}"
                        ValueContentConverter="{StaticResource DecibelConverter}"
                        HorizontalAlignment="Stretch"/>
```

**Bind data:** `Value` TwoWay, plus `StepFrequency`, `Stops`, `SnapToStops` for stepped/snapped
ranges. If you just need custom units without a converter, set `ValueFormat` once — it is a
composite format string, not a per-value binding target, so it needs no `ValueChanged` handler to
stay in sync:
```xml
<ether:EtherSteeringBar Value="{x:Bind ViewModel.Playback, Mode=TwoWay}"
                        Title="Playback" ValueFormat="{}{0:0} dB"
                        HorizontalAlignment="Stretch"/>
```
Proven at `R2.cs:138-146` (`VerifyTwoWayBindings`).

**Wire an action:** `ValueChanged` — **it fires on every drag tick**, not once per commit, so it
is a poor fit for an `ICommand` (this is why P3 did not add one here; see
`EtherSteeringBar.xaml.cs:381-384, 476-487`). Debounce in the handler if you only care about the
settled value:
```xml
<ether:EtherSteeringBar Value="{x:Bind ViewModel.Playback, Mode=TwoWay}"
                        ValueChanged="{x:Bind ViewModel.OnPlaybackChanged}"/>
```

**EtherSlider** (`Views/Controls/SliderPage.xaml`):
```xml
<ether:EtherSlider HorizontalAlignment="Stretch"
                   Value="65"
                   AutomationProperties.Name="Package slider"/>
```

**Bind data:** `Value` TwoWay, plus `StepFrequency`, `Stops`, `SnapToStops`:
```xml
<ether:EtherSlider Value="{x:Bind ViewModel.Volume, Mode=TwoWay}"
                   HorizontalAlignment="Stretch"/>
```
Same `RangeBase`-derived TwoWay contract as `EtherSteeringBar` (`R2.cs:138-146` pattern).

**Wire an action:** `ValueChanged` — same every-tick caveat as `EtherSteeringBar` above:
```xml
<ether:EtherSlider Value="{x:Bind ViewModel.Volume, Mode=TwoWay}"
                   ValueChanged="{x:Bind ViewModel.OnVolumeChanged}"/>
```

**EtherMasthead** (`Views/Navigation/MastheadPage.xaml`) — `EnableWindowCommands="False"` is
used here only to demo the control embedded in a plain panel; real title-bar usage should
keep the default value:
```xml
<ether:EtherMasthead EnableWindowCommands="False"
                     AutomationProperties.Name="Package masthead"/>
```

**Bind data:** `ShowSettings`/`ShowSearch`/`ShowMenuIcon`/`ShowChevron`/`EnableWindowCommands`, all
`OneWay`:
```xml
<ether:EtherMasthead ShowSettings="{x:Bind ViewModel.CanConfigure, Mode=OneWay}"
                     ShowSearch="True"
                     AutomationProperties.Name="App masthead"/>
```

**Wire an action:** `ActionInvoked` reports which caption action fired
(`MastheadAction.Minimize`/`MaximizeRestore`/`Close`); it fires **before** the host window command
executes and cannot be cancelled (`EtherMasthead.xaml.cs:120, 543/561/585`):
```xml
<ether:EtherMasthead ActionInvoked="{x:Bind ViewModel.OnMastheadAction}"/>
```
The optional menu/settings/search/chevron icon slots are decorative only (no built-in click hook);
`ObserveMasthead` observes only the caption buttons (`ActionInvoked`), not these slots — place your
own interactive control beside the masthead if one of them needs to act.

**EtherSwitch** (`Views/Controls/ToggleSwitchPage.xaml`) — not a standalone type; it is a
style applied on top of the native `ToggleSwitch`:
```xml
<ToggleSwitch Style="{StaticResource EtherSwitch}"
              IsOn="True"
              OffContent="Off"
              OnContent="On"
              AutomationProperties.Name="Package toggle switch"/>
```

**Bind data:** `IsOn` TwoWay (native `ToggleSwitch` property; `EtherSwitch` is a keyed `Style`, not
a type):
```xml
<ToggleSwitch Style="{StaticResource EtherSwitch}"
              IsOn="{x:Bind ViewModel.NotificationsEnabled, Mode=TwoWay}"
              OffContent="Off" OnContent="On"/>
```
Proven at `R2.cs:148-156` (`VerifyTwoWayBindings`).

**Wire an action:** no `Command` on `ToggleSwitch`; bind the native `Toggled` event:
```xml
<ToggleSwitch Style="{StaticResource EtherSwitch}"
              IsOn="{x:Bind ViewModel.NotificationsEnabled, Mode=TwoWay}"
              Toggled="{x:Bind ViewModel.OnNotificationsToggled}"/>
```

**EtherCard** (`Views/Surfaces/CardPage.xaml`) — likewise not a standalone type; it is a set
of styles applied on top of `Border`, in three variants: `Normal`/`Intelligence`/`Callout`:
```xml
<Border Style="{StaticResource EtherCardNormal}">
    <Border Style="{StaticResource EtherCardNormalBody}">
        <TextBlock Text="Card" Style="{StaticResource headers/h3}"
                   Foreground="{ThemeResource text/primary}"/>
    </Border>
</Border>
```

**Bind data / wire an action:** none. `EtherCard` is a set of keyed `Style`s on `Border`, not a
type with its own dependency properties — put bound content in the
`Border`'s single `Child` (wrap multiple elements in a panel) the same way you would with a plain `Border`. There is no Ether-specific
property to bind and no interaction surface to wire; a card is a static surface, not a control.

**EtherTooltip** (`Views/Surfaces/TooltipPage.xaml`) — likewise not a standalone type; it is a
keyed `Style` applied on top of `Border`, painting the inverse-surface tooltip chrome (dark on
light, light on dark):
```xml
<Border Style="{StaticResource EtherTooltip}">
    <TextBlock Text="Tooltip"
               Style="{StaticResource body/s-regular}"
               Foreground="{ThemeResource EtherTooltipForegroundBrush}"
               TextWrapping="Wrap"/>
</Border>
```

**Bind data / wire an action:** none. `EtherTooltip` is a keyed `Style` on `Border`, not a type
with its own dependency properties — `MaxWidth="240"`, `BorderThickness="2"`, and `HorizontalAlignment="Left"`/`VerticalAlignment="Top"` (so it hugs its content rather than stretching) are baked into the `Border` style, while the
child `TextBlock`'s `body/s-regular` style, `EtherTooltipForegroundBrush` foreground, and
`TextWrapping="Wrap"` are markup you apply, so pair the `TextBlock`'s `Text` with `TextWrapping="Wrap"` the way the
sample above does and longer copy wraps on its own. There is no Ether-specific property to bind
and no interaction surface to wire; this is a static visual surface, not the WinUI `ToolTip`/
`ToolTipService` — it carries no hover/dismiss/placement behavior of its own.

**EtherPanelTabs** (`Views/Navigation/PanelTabsPage.xaml`) — not a standalone type either; it
reuses the same `EtherSegmentedControl`/`EtherSegmentPanel`/`EtherSegmentRadioButton` skeleton as
the `EtherSegmentedControl` entry above, only with a different keyed `Style` on the host and on
each segment — a translucent track and a near-black/near-white selected pill instead of the
brand-blue one:
```xml
<controls:EtherSegmentedControl Style="{StaticResource EtherPanelTabs}"
                                AutomationProperties.Name="Package panel tabs">
    <controls:EtherSegmentPanel>
        <controls:EtherSegmentRadioButton Style="{StaticResource EtherPanelTabSegment}"
                     GroupName="package-panel-tabs"
                     Content="Overview"
                     IsChecked="True"/>
        <controls:EtherSegmentRadioButton Style="{StaticResource EtherPanelTabSegment}"
                     GroupName="package-panel-tabs"
                     Content="Details"/>
    </controls:EtherSegmentPanel>
</controls:EtherSegmentedControl>
```

**Bind data / wire an action:** the identical contract to `EtherSegmentedControl` above —
`ItemsSource`/`ItemTemplate`/`DisplayMemberPath`/`SelectedIndex`/`SelectedItem`/`SelectedValue`
(all `TwoWay`-capable), `SelectionChanged`, and the user-initiated-only `SelectionCommand`/
`SelectionCommandParameter` pair. Swapping `Style="{StaticResource EtherPanelTabs}"` on the host
and `Style="{StaticResource EtherPanelTabSegment}"` on each **inline** segment for
`EtherSegmentedControl`'s defaults is the visual difference; the selection/command contract is
shared. **Note:** `ItemsSource`-generated segments resolve the `EtherSegment` resource **key** from the
control/ancestor/application scope (there is no per-item style property), so by default a data-driven
Panel Tabs uses the blue segmented-control skin. Either author inline `EtherSegmentRadioButton`s with
`EtherPanelTabSegment`, or define a scoped `EtherSegment` resource (based on `EtherPanelTabSegment`)
in the control's/page's `Resources` so generated segments pick up the panel-tab skin.
Also note `ItemTemplate` applies to non-string model items only; a string `ItemsSource` renders the
raw string.

**EtherScrollBar** (`Views/Foundations/ScrollBarPage.xaml`) — once `DesignSystem.xaml` is
merged, this applies implicitly to the native `ScrollBar`/`ScrollViewer`; no extra `Style=`
is needed:
```xml
<ScrollViewer Width="240" Height="200"
              VerticalScrollBarVisibility="Visible"
              HorizontalScrollBarVisibility="Visible">
    <Border Width="400" Height="400"/>
</ScrollViewer>
```

**Bind data / wire an action:** none. The style applies implicitly to native `ScrollBar`/
`ScrollViewer` and adds no Ether-specific properties or events. If you need to observe scroll
position, bind the native `ScrollViewer` APIs (`ViewChanged`, `VerticalOffset`, ...) directly —
nothing here is Ether-owned.

> **persistent-only mode**: this is an always-visible scrollbar — the template does not
> define groups such as `ScrollingIndicatorStates`/`NoIndicator`, so it does not auto-hide
> when idle, expand on hover, or fade out when disabled the way a native `ScrollBar` does; it
> is always rendered as a 6 px overlay (`EtherScrollBar.xaml:10-13`). If your application
> needs auto-hide / indicator behavior, do not rely on this implicit style — explicitly
> specify a different `Style=` (the native default style, or a custom one).

## 6. Binding to a view-model (MVVM patterns)

### 6.1 `x:Bind` TwoWay to an `INotifyPropertyChanged` view-model

`x:Bind` defaults to `Mode=OneTime` — a very common WinUI mistake is binding an editable property
without writing `Mode=TwoWay` and then wondering why user edits never reach the view-model. Every
DP TwoWay contract quoted in §5 above is proven against exactly this view-model shape
(`tests/Ether.DesignSystem.ConsumerFixtures/RuntimeVerification.R2.cs:17-45`, `TwoWayViewModel`):

```csharp
public sealed class MyPageViewModel : INotifyPropertyChanged
{
    private string _searchQuery = string.Empty;
    public string SearchQuery
    {
        get => _searchQuery;
        set { if (_searchQuery != value) { _searchQuery = value; OnPropertyChanged(); } }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
```

```xml
<ether:EtherInput Text="{x:Bind ViewModel.SearchQuery, Mode=TwoWay}"/>
```

### 6.2 Button-family `Command`/`CommandParameter`

`EtherButton`, `EtherIntelligenceButton`, `EtherCheckbox`, `EtherRadioButton` — all four are
proven end to end (`R2.cs:374-431`). See each control's entry in §5 above for the exact markup.

### 6.3 Event → view-model method: `{x:Bind ViewModel.OnSomething}`

For controls that expose an event but no `Command` (`EtherDropdown`, `EtherTabNavigation`,
`EtherSlider`, `EtherSteeringBar`, `EtherSwitch`, `EtherInput`), binding the event straight to a
view-model method with `{x:Bind}` is the WinUI-native answer — **no extra NuGet package
required**. The handler may match the event's real signature or take no parameters:

```xml
<ether:EtherDropdown SelectionChanged="{x:Bind ViewModel.OnDurationChanged}"/>
```

```csharp
public void OnDurationChanged(object sender, SelectionChangedEventArgs e) { /* ... */ }
// or simply: public void OnDurationChanged() { /* ... */ }
```

### 6.4 `Microsoft.Xaml.Interactivity` as a consumer-side alternative

If you prefer a Behaviors-style declarative binding instead of a code-behind event handler, the
`Microsoft.Xaml.Interactivity` package (`EventTriggerBehavior` + `InvokeCommandAction`) is a
well-known WinUI pattern:

```xml
<ether:EtherDropdown>
    <interactivity:Interaction.Behaviors>
        <interactivity:EventTriggerBehavior EventName="SelectionChanged">
            <interactivity:InvokeCommandAction Command="{x:Bind ViewModel.DurationChangedCommand}"/>
        </interactivity:EventTriggerBehavior>
    </interactivity:Interaction.Behaviors>
</ether:EtherDropdown>
```

**This is a consumer-side choice, not an Ether dependency.** `Microsoft.Xaml.Interactivity` is not
referenced by `Directory.Packages.props` and no fixture in this repository exercises it — add it
to your own application if you want this pattern, and verify it there. `CommunityToolkit.Mvvm`
(`[ObservableProperty]`/`[RelayCommand]`) is a similarly common consumer-side choice for §6.1/§6.2's
view-model boilerplate; the Ether packages depend on neither.

### 6.5 `ItemsSource` — three proven paths, two honest limitations

`EtherDropdown`, `EtherSegmentedControl`, and `EtherTabNavigation` all support a data-driven
`ItemsSource` path (see each control's §5 entry above for markup and fixture references). Two
limitations to know before relying on them:

- **`EtherTabNavigation` data-bound tabs have no per-item icon.** `Icon` lives on the inline
  `EtherTabItem` container (`EtherTabItem.cs:28-41`); the `ItemsSource` path generates plain
  containers with no `PrepareContainerForItemOverride`, so it cannot populate one. Use inline
  `EtherTabItem` if you need icons.
- **`EtherDropdown`'s closed trigger only ever shows text.** `DisplayMemberPath`/`ToString()`
  drives the trigger; a rich `ItemTemplate` only renders inside the open menu
  (`EtherDropdown.cs:42-43`).

### 6.6 Common pitfalls

- `{Binding}` reads `DataContext`; `{x:Bind}` reads the page/control's own code-behind (or an
  explicit `ViewModel...` path) at **compile time** — the two are not interchangeable without
  adjusting the path.
- A `DataTemplate` that uses `x:Bind` must declare `x:DataType`, or the compiler cannot resolve
  the binding.
- `EtherCheckbox.IsChecked`/`EtherRadioButton.IsChecked` are `bool?`, not `bool` — bind to a
  `bool?` view-model property, or convert, if you also need to represent "unset".

## 7. Interactions adapter (optional backend telemetry)

This is a **separate, optional path** from the MVVM binding in §6 — use it only when you need to
forward a control interaction to a backend outbox as a versioned event envelope, not as a
replacement for ordinary data binding.

`Ether.DesignSystem.Interactions`'s `ControlInteractionAdapter` wraps a control's native event in
a JSON-safe `InteractionEvent` and raises `InteractionProduced`. Subscribe once per control
instance and enqueue the envelope into your own durable outbox (an `IInteractionSink` or
equivalent):

```csharp
using Ether.DesignSystem.Interactions;

var adapter = new ControlInteractionAdapter();
adapter.InteractionProduced += (_, args) =>
{
    // Write args.Interaction to your application's durable outbox; a backend adapter
    // publishes it later using its own transport and authentication policy.
    _ = outboxSink.EnqueueAsync(args.Interaction);
};

var context = new InteractionContext(CorrelationId: Guid.NewGuid().ToString(), Screen: "Orders");
var subscription = adapter.ObserveButton(
    saveButton, "order.save.requested", context, componentId: "orders.save-button");

// Dispose the subscription (e.g. in Page.Unloaded) to detach the event handler.
subscription.Dispose();
```

`ControlInteractionAdapter` exposes one `Observe*` method per control shape —
`ObserveButton`/`ObserveToggle`/`ObserveSelection`/`ObserveDropdown`/`ObserveInput`/
`ObserveSegmentedControl`/`ObserveSteeringBar`/`ObserveRange`/`ObserveMasthead`/`ObserveSwitch`,
plus a generic `ObserveProperty` for one explicitly chosen dependency property
(`src/Ether.DesignSystem.Interactions/ControlInteractionAdapter.cs:21-182`). Use business event
names (`order.submit.requested`), not UI event names (`click`), and supply
`idempotencyKeyFactory` for write operations so retries preserve one key. See
`src/Ether.DesignSystem.Interactions/README.md` for the full contract, including the
`SelectedValuePath` guidance for dropdowns.

## 8. Versioning discipline

Publishers must ensure that **any content change** corresponds to a new package version, and
must never reuse a version that has already been tagged or published. NuGet's cache key is
package ID plus version number; reusing a version number causes consumers (and verification
infrastructure) to silently receive the old DLL even though `restore` succeeds.

When upgrading, consumers should update the `PackageReference` version and run restore,
rather than treating clearing the cache as a normal upgrade mechanism. Deleting a single
local package directory is only an emergency measure for pinpointing a caching problem — it
is not a substitute for a proper version release.

## Evidence and items pending organizational confirmation

- **Verified by test**: Variant A of `scripts/Verify-ExternalConsumer.ps1` uses the three
  package references, `net8.0-windows10.0.19041.0`, `x64`, `UseWinUI`,
  `EnableMsixTooling`, the two `App.xaml` merges, and `using:Ether.DesignSystem.Controls`
  documented here; its restore, build, and runtime marker all pass.
- **Verified compatible**: Variant B of the same script, which additionally references
  `Microsoft.WindowsAppSDK` explicitly, also passes; so applications that already have this
  reference do not need to remove it.
- **Needs organizational confirmation**: the actual `<PACKAGE_OWNER>` for GitHub Packages,
  the PAT issuance channel, whether the token needs SSO authorization, and proxy settings
  are outside this repository's test scope. For the first real-feed integration, the
  publishing team should provide these values and complete one restore verification.
