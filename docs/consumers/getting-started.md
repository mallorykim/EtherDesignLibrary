# 接入 Ether Design System（内部团队）

本文面向公司内部 WinUI 3 应用团队。当前发布线为预览版
`0.1.0-preview.1`，仅通过私有 GitHub Packages 分发；不要将包或 PAT
配置公开到外部仓库。

## 已验证的支持范围

- 目标框架：`net8.0-windows10.0.19041.0`
- 最低 Windows 平台版本：`10.0.17763.0`
- 架构：仅 `x64`
- 宿主：已验证 unpackaged 与 packaged（MSIX **构建/产出**）两种形态；
  MSIX 的**安装与运行时**尚未验证，仍不完整。
- 包：`Ether.DesignSystem.Foundation`、`Ether.DesignSystem.Controls`、
  `Ether.DesignSystem.Interactions`，当前均使用 `0.1.0-preview.1`。

外部消费者验证已证明：只显式引用上述三个 Ether 包即可；**不需要**为了
使用 Ether 而额外显式引用 `Microsoft.WindowsAppSDK`。如果应用本身已经引用
它，也受支持。

## 1. 配置 GitHub Packages 访问

### 创建并保管 PAT

GitHub Packages 即使在同一组织内也要求认证。为 restore 准备一个拥有
`read:packages` 权限的 PAT，并将其作为秘密保管；不要把 PAT 明文写进仓库、
`nuget.config` 或日志。

以下是 GitHub classic PAT 的常用创建路径：GitHub **Settings** →
**Developer settings** → **Personal access tokens** → **Tokens (classic)** →
**Generate new token (classic)**，勾选 `read:packages`。若组织启用了 SSO，
还需要按组织策略授权该 token。

> 注意：仓库的外部消费者脚本验证了包 restore/build/runtime 与资源合并，
> 但不会连接真实 GitHub Packages，也不会代替组织的 PAT 发放或 SSO 流程。上
> 述 token 创建界面和 SSO 步骤需以贵组织当前 GitHub 策略为准。

在当前 PowerShell 会话中设置凭据（不要把真实 token 粘进脚本文件）：

```powershell
$env:GITHUB_PACKAGES_USERNAME = '<你的 GitHub 用户名>'
$env:GITHUB_PACKAGES_PAT = '<拥有 read:packages 的 PAT>'
```

在解决方案根目录创建或更新 `nuget.config`。将 `<PACKAGE_OWNER>` 替换为
**发布这些包的 GitHub 组织或用户**，不是仓库名；请从发布公告或包管理员处
获取准确值。

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

`ClearTextPassword` 在这里保存的是环境变量引用，而不是 token 本身。不要将
替换后的真实 PAT 提交到 Git。真实 GitHub feed、包所有者和组织认证尚未由本
仓库的外部消费者脚本端到端演练；它使用本地 feed 验证包消费。因此首次接入应
以发布团队提供的 owner 与组织认证要求为准。

## 2. 最小 WinUI 3 应用

以下项目属性、三包引用、`App.xaml` 资源合并和控件 XAML 均从
[`scripts/Verify-ExternalConsumer.ps1`](../../scripts/Verify-ExternalConsumer.ps1)
的成功变体 A 提炼。该变体以**仅三个 Ether 包**完成 restore、x64 build 和
运行时标记验证。

从 WinUI 3 应用模板创建项目后，将项目文件调整为以下关键配置（保留模板已经
需要的其他设置即可）：

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
    <PackageReference Include="Ether.DesignSystem.Foundation" Version="0.1.0-preview.1" />
    <PackageReference Include="Ether.DesignSystem.Controls" Version="0.1.0-preview.1" />
    <PackageReference Include="Ether.DesignSystem.Interactions" Version="0.1.0-preview.1" />
    <Manifest Include="$(ApplicationManifest)" />
  </ItemGroup>
</Project>
```

`UseWinUI` 和 `EnableMsixTooling` 是已验证消费者项目的一部分，即使该示例
是 unpackaged 宿主也应保留。项目根目录的 `app.manifest` 可使用验证项目中
的同款内容：

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

### 必须合并资源字典

在 `App.xaml` 中合并 WinUI 标准资源和 Ether 的资源入口。漏掉第二项时，控件
的样式、资源或打包资源路径不会被正确解析。

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

### 使用控件

在页面或窗口 XAML 中使用新的公开命名空间：

```xml
<Window
    x:Class="Contoso.Inventory.MainWindow"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:ether="using:Ether.DesignSystem.Controls"
    Title="Ether consumer">
    <ScrollViewer>
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
                Title="Download" ValueContent="65%"
                ShowTitle="True" ShowValue="True" />

            <ether:EtherInput
                Text="External text"
                PlaceholderText="External placeholder" />
        </StackPanel>
    </ScrollViewer>
</Window>
```

`xmlns:ether="using:Ether.DesignSystem.Controls"` 必须与上面的文本完全一致；
不要使用旧的 `EtherSandbox.Controls` 命名空间。

对应的最小启动代码如下。验证脚本使用相同的 `MainWindow` 创建与激活路径，
只额外包裹了运行时结果标记逻辑。

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

然后执行：

```powershell
dotnet restore --configfile .\nuget.config
dotnet build -c Debug -p:Platform=x64
```

## 3. 故障排查

| 症状 | 常见原因 | 处理方式 |
| --- | --- | --- |
| `401` / `403` | PAT 缺失、过期、没有 `read:packages`，或组织 SSO 未授权 | 重新设置环境变量，确认 PAT 的 `read:packages` 权限；若组织使用 SSO，请按组织要求授权 token。 |
| `404` | GitHub Packages 源的 owner 段写错，或包 ID / 版本不存在 | 核对 `https://nuget.pkg.github.com/<PACKAGE_OWNER>/index.json` 中的 owner，以及三个包 ID 和发布版本。不要把仓库名填到 owner 位置。 |
| `NU1301` | feed 无法访问：网络/代理策略阻断，或源地址错误 | 在浏览器或组织网络环境确认该 feed 可达；核对 `nuget.config` 的 URL、代理和证书策略，再重新 restore。 |
| 重新打包后消费者仍像在用旧代码；`restore` 成功但 build 出现旧命名空间等错误 | NuGet 按 **包 ID + 版本号** 缓存；复用已发布的版本会静默命中旧包 | 正确处理是发布新版本，并在 `PackageReference` 中升级版本。仅用于本机排障时，可由 `dotnet nuget locals global-packages --list` 找到全局目录后，删除**仅该包 ID / 旧版本**的目录；不要清空整个 NuGet 缓存。 |
| `XamlParseException`，或 `ms-appx:///Ether.DesignSystem.Controls/...` 资源找不到 | 未合并 `DesignSystem.xaml`，或 URI 拼写错误 | 将 `XamlControlsResources` 和 `ms-appx:///Ether.DesignSystem.Controls/Themes/DesignSystem.xaml` 一起放入 `App.xaml` 的 `ResourceDictionary.MergedDictionaries`，然后清理并重新 build。 |
| `WMC0001: Unknown type 'EtherButton'` | `xmlns` 使用了错误命名空间，或包没有正确 restore | 使用 `xmlns:ether="using:Ether.DesignSystem.Controls"`；确认 restore 成功且 `Ether.DesignSystem.Controls` 的版本存在于 assets 文件。 |
| restore 成功，但装到的不是 Ether 包（例如同名的公共 nuget.org 包，或版本内容对不上预期） | `nuget.config` 里 `github-ether` 与 `nuget.org` 两个源共存，任一源解析失败或搜索顺序意外时，NuGet 可能从**另一个源**满足同名包 ID | 在 `project.assets.json` 或 restore 日志中核对每个 Ether 包 ID 实际解析到的源（`source` 字段）是否为 `github-ether`；长期做法是给 `nuget.config` 加 [package source mapping](https://learn.microsoft.com/en-us/nuget/consume-packages/package-source-mapping)，把 `Ether.DesignSystem.*` 显式钉死到 `github-ether`，把其余包 ID 钉死到 `nuget.org`，避免任一源意外满足另一源的包名。 |

## 4. 已知边界

- 仅支持 `x64`。请将应用构建为 `x64`，不要把 x86 或 ARM64 当作受支持目标。
- 当前版本是 preview，公开 API、样式和资源契约仍可能变化。升级前请阅读对应
  发布说明并在应用中回归关键页面。

### 4.1 继承自基类、但 Ether 模板不消费的属性

下表里的属性都**继承自 WinUI 基类、对外公开、编译通过、运行不报错**——但
Ether 的自定义模板完全不引用它们，所以设置之后**界面上什么也不会发生**（对
`EtherDropdown.Text` / `IsEditable` 而言，是连功能也不会发生）。这不是文档
单方面的说法：`scripts/Verify-UnsupportedProperties.ps1` 以
`scripts/UnsupportedProperties.psd1`（唯一事实来源）为准，逐条断言对应模板
文件里确实没有消费这个属性；这张表也从同一份文件生成/校验，二者不会各写各的。

这些属性目前**不会**在设置时抛异常或给出运行时提示——它们只是不显示内容，
不是显示错误的内容。`EtherCheckbox` / `EtherRadioButton` 是有意设计的两态控件：
`IsChecked=null` 会被强制为 `false`，`IsThreeState=true` 会被强制回 `false`；
请不要依赖 Indeterminate 状态。下面的表只列出真正不支持或静默无效的继承属性。

<!-- UNSUPPORTED-PROPERTIES:START -->
| 控件 | 属性 | 继承自 | 替代做法 |
| --- | --- | --- | --- |
| `EtherDropdown` | `Header` | `ComboBox` | 用外部 `TextBlock` 或表单容器在控件上方放标签。 |
| `EtherDropdown` | `HeaderTemplate` | `ComboBox` | 同 `Header`：标签内容放在控件外部构建。 |
| `EtherDropdown` | `Description` | `ComboBox` | 在控件下方再放一个 `TextBlock` 作为说明文字。 |
| `EtherDropdown` | `PlaceholderForeground` | `ComboBox` | `PlaceholderText` 可用于未选择状态；此颜色属性没有绑定到模板。需要固定占位颜色时，请使用外部占位视觉。 |
| `EtherDropdown` | `Text` | `ComboBox` | `EtherDropdown` 是**选择型控件**（Figma 源没有可编辑组合框变体），不提供可编辑模式。需要自由文本输入请改用 `EtherInput`，或自行样式化原生可编辑 `ComboBox`。 |
| `EtherDropdown` | `IsEditable` | `ComboBox` | 同 `Text`：设计上不支持可编辑模式。设为 `true` 不会让控件可输入——模板里没有 `ComboBox` 内部需要的 `"EditableText"` 部件，所以这个开关静默不生效。 |
| `EtherInput` | `Header` | `TextBox` | 这是既有的设计决策（见 `EtherInput.cs` 备注："does not add ... header/description slots"），不是遗漏。请用外部标签/说明布局包裹 `EtherInput`。 |
| `EtherInput` | `HeaderTemplate` | `TextBox` | 同 `Header`。 |
| `EtherInput` | `Description` | `TextBox` | 同 `Header`：在控件下方另放一个 `TextBlock`。 |
<!-- UNSUPPORTED-PROPERTIES:END -->

> 说明：`ToggleSwitch` 在当前使用的 `Microsoft.WindowsAppSDK 2.3.1` 里**没有**
> `Description` 属性（已用反射确认：`Microsoft.UI.Xaml.Controls.ToggleSwitch`
> 只有 `Header` / `HeaderTemplate`，没有 `Description`），因此上表没有列出
> `EtherSwitch.Description` 这一行——它连"存在但无效的属性"都不是，是根本不
> 存在的属性。
- `Ether.DesignSystem.Controls.Primitives.HandContentControl` 虽因 XAML 资源解析
  而公开，但它是模板实现支撑类型，不是受支持的设计系统控件契约；请使用命名的
  `Ether*` 控件。
- Controls 与 Foundation 在当前 preview 发布线按同一版本使用；升级时应一起升级
  两者。Interaction 包也应与发布公告给出的版本组合保持一致。
- 除上表这 9 个属性外，各控件还继承了大量 WinUI 基类属性。其中多数确实是
  外观类属性（`Background` / `BorderBrush` / `BorderThickness` / `CornerRadius` /
  `Padding` / `Foreground` / `FontSize` 等）——这些是**设计系统故意不开放覆盖**
  的：控件模板固定引用设计令牌而非 `{TemplateBinding ...}`，为的是让所有消费方
  看到一致的视觉语言，"设置了没反应"是特性而不是遗漏。但**并非全部如此**：还有
  一部分属性模板其实做了 `{TemplateBinding ...}`（只是画面探针在其测试条件下
  没能测出像素差异——例如 `EtherInput.PlaceholderText` 只在控件为空且未获焦时
  渲染），或由 WinUI 基类在模板之外自行处理（例如 `TextBox` 的 `AcceptsReturn`
  / `IsReadOnly` / `CharacterCasing`）——这些属性其实是**生效的**，不应被当成
  "不支持"。以上区分（连同少数确认无效的底层平台属性，如 `Clip` /
  `CompositeMode`），完整登记在 `scripts/UnsupportedProperties.psd1` 的
  `AcknowledgedSilent` 列表中，按 `design-system-owned`（确认未绑定，故意锁定）/
  `consumed-visually-stable`（确认已绑定，只是探针未测出像素差异）/
  `behavioral`（基类行为生效，不可用像素差异测试）/ `platform-noop`（底层平台
  DP，无消费方期待）/ `needs-review`（尚不确定，待人工复核）五类归档，每条都附
  `Reason`。`local-runtime` 门禁 `scripts/Verify-SilentPropertyCoverage.ps1` 针对
  每次运行时证据自动双向核对（新出现的、未登记的静默失效属性会让门禁失败），
  并额外做 `TemplateBinding` 交叉检查确保 `design-system-owned` /
  `consumed-visually-stable` 的标签本身没有标错。

## 5. 各控件标记参考

下面每个控件给出一段可以直接复制的标记示例，取自
[`tests/Ether.DesignSystem.ConsumerFixtures/Unpackaged/MainWindow.xaml`](../../tests/Ether.DesignSystem.ConsumerFixtures/Unpackaged/MainWindow.xaml)
的 `*Proof` 元素——这些标记是仓库外消费者验证（A1/A4）实际运行过的形态，与
`samples/Ether.DesignSystem.Gallery` 对应页面的写法一致。命名空间前缀在示例中
省略，按第 2 节声明 `xmlns:ether="using:Ether.DesignSystem.Controls"` 后把
`controls:` 换成你自己的前缀即可。

**EtherButton**（对应 Gallery `Views/Controls/ButtonPage.xaml`）：
```xml
<ether:EtherButton Content="Package button"
                   AutomationProperties.Name="Package button" />
<ether:EtherButton Style="{StaticResource EtherButtonSecondary}"
                   Content="Secondary package button"
                   AutomationProperties.Name="Secondary package button" />
```

*(English from here — this three-part pattern is new in this pass; see §6 for the full MVVM
write-up.)*

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

**EtherProgressBar**（`Views/DataDisplay/ProgressBarPage.xaml`）：
```xml
<ether:EtherProgressBar HorizontalAlignment="Stretch"
                        Title="Package download"
                        ValueContent="65%"
                        Value="65"
                        AutomationProperties.Name="Package download progress" />
```

**Bind data:** `Value`, `Title`, `ValueContent`, `ShowTitle`, `ShowValue` are all `OneWay`-friendly
display properties (`EtherProgressBar.cs:60-93`):
```xml
<ether:EtherProgressBar Value="{x:Bind ViewModel.DownloadPercent, Mode=OneWay}"
                        Title="{x:Bind ViewModel.DownloadLabel, Mode=OneWay}"
                        ValueContent="{x:Bind ViewModel.DownloadPercentText, Mode=OneWay}"
                        ShowTitle="True" ShowValue="True"
                        HorizontalAlignment="Stretch"/>
```

**Wire an action:** none. `EtherProgressBar` is a read-only status indicator with no interaction
surface — there is nothing to "wire".

**EtherCheckbox**（`Views/Controls/CheckboxPage.xaml`）：
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
every check/uncheck, exactly like a native `CheckBox`:
```xml
<ether:EtherCheckbox Content="Send me updates"
                     Command="{x:Bind ViewModel.ToggleSubscriptionCommand}"
                     CommandParameter="{x:Bind ViewModel.SubscriptionId}"/>
```
Proven at `R2.cs:379, 400-412` (`VerifyToggleCommand`). Most apps only need one of `IsChecked`
TwoWay or `Command` — add `Command` only when something besides the bound state (telemetry, a
save) must run on toggle.

**EtherRadioButton**（`Views/Controls/RadioButtonPage.xaml`）：
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
but it still fires `Command`/`CommandParameter` once per selection:
```xml
<ether:EtherRadioButton GroupName="delivery-speed" Content="Express"
                        Command="{x:Bind ViewModel.SelectDeliverySpeedCommand}"
                        CommandParameter="Express"/>
```
Proven at `R2.cs:384, 419-431` (`VerifySelectionItemCommand`).

**EtherInput**（`Views/Controls/InputPage.xaml`）：
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

**EtherDropdown**（`Views/Controls/DropdownPage.xaml`）：
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

**EtherSegmentedControl**（`Views/Controls/SegmentedControlPage.xaml`）——段项推荐用
`EtherSegmentRadioButton`（套用 `EtherSegment` 样式，放进 `EtherSegmentPanel`）：具备完整
的 hover / pressed / checked 反馈，和 Gallery 效果一致：
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
普通 `RadioButton` 套用 `EtherSegment` 样式仍会显示选中态的药丸背景，但不会有 hover / pressed 反馈。

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

**EtherTabNavigation**（`Views/Navigation/TabNavigationPage.xaml`）：
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
`PrepareContainerForItemOverride`, so data-bound tabs cannot show a per-item icon — use inline
`EtherTabItem` if icons are required.

**Wire an action:** no `Command`; bind the native `SelectionChanged` event:
```xml
<ether:EtherTabNavigation ItemsSource="{x:Bind ViewModel.Sections}"
                          SelectionChanged="{x:Bind ViewModel.OnSectionSelectionChanged}"/>
```

**EtherIntelligenceButton**（`Views/Controls/IntelligenceButtonPage.xaml`）：
```xml
<ether:EtherIntelligenceButton Content="Package intelligence"
                               AutomationProperties.Name="Package intelligence button"/>
```

**Bind data:** same as `EtherButton` — `Content`/`IsEnabled`, `Mode=OneWay`.

**Wire an action:** `EtherIntelligenceButton` is also a `ButtonBase`; `Command`/`CommandParameter`
work identically:
```xml
<ether:EtherIntelligenceButton Content="Ask Ether"
                               Command="{x:Bind ViewModel.AskCommand}"/>
```
Proven at `R2.cs:378, 388-398` (`VerifyButtonCommand`).

**EtherSteeringBar**（`Views/Controls/SteeringBarPage.xaml`）：
```xml
<ether:EtherSteeringBar HorizontalAlignment="Stretch"
                        Title="Package playback"
                        ValueContent="65%"
                        Value="65"
                        AutomationProperties.Name="Package steering bar"/>
```

**Bind data:** `Value` TwoWay, plus `StepFrequency`, `Stops`, `SnapToStops` for stepped/snapped
ranges:
```xml
<ether:EtherSteeringBar Value="{x:Bind ViewModel.Playback, Mode=TwoWay}"
                        Title="Playback" ValueContent="{x:Bind ViewModel.PlaybackText, Mode=OneWay}"
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

**EtherSlider**（`Views/Controls/SliderPage.xaml`）：
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

**EtherMasthead**（`Views/Navigation/MastheadPage.xaml`）——`EnableWindowCommands="False"`
是内嵌到普通面板中演示时的写法；真实标题栏用法保留默认值：
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
The optional menu/settings/search icon slots are decorative only (no built-in click hook) — wire a
real action via `ObserveMasthead` (§7 below) or your own icon overlay if you need one.

**EtherSwitch**（`Views/Controls/ToggleSwitchPage.xaml`）——不是独立类型，是套在原生
`ToggleSwitch` 上的样式：
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

**EtherCard**（`Views/Surfaces/CardPage.xaml`）——同样不是独立类型，是套在
`Border` 上的一组样式，`Normal`/`Intelligence`/`Callout` 三种：
```xml
<Border Style="{StaticResource EtherCardNormal}">
    <Border Style="{StaticResource EtherCardNormalBody}">
        <TextBlock Text="Card" Style="{StaticResource headers/h3}"
                   Foreground="{ThemeResource text/primary}"/>
    </Border>
</Border>
```

**Bind data / wire an action:** none. `EtherCard` is a set of keyed `Style`s on `Border`, not a
type with its own dependency properties — put bound content (a title, body text, an icon) in the
`Border`'s children the same way you would with a plain `Border`. There is no Ether-specific
property to bind and no interaction surface to wire; a card is a static surface, not a control.

**EtherScrollBar**（`Views/Foundations/ScrollBarPage.xaml`）——合并
`DesignSystem.xaml` 后对原生 `ScrollBar`/`ScrollViewer` 隐式生效，不需要额外
`Style=`：
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

> **persistent-only 模式**：这是常驻显示的滚动条——模板没有定义
> `ScrollingIndicatorStates`/`NoIndicator` 等分组，因此不会像原生 `ScrollBar`
> 那样闲置自动隐藏、hover 展开或禁用时淡出，始终以 6 px 覆盖层呈现
> （`EtherScrollBar.xaml:10-13`）。如果应用需要 auto-hide / indicator 行为，
> 不要依赖这份隐式样式——显式指定其他 `Style=`（原生默认样式或自定义样式）。

## 6. Binding to a view-model (MVVM patterns)

*(This section, and §7 below, are in English — the rest of this document is Chinese for the
internal team, but the patterns here are exactly what a consumer copies into their own
view-model, so they are kept in the language the code itself uses.)*

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

## 8. 版本纪律

发布方必须让**任何内容变化**对应新的包版本，绝不复用已经打过或发布过的版本。
NuGet 的缓存键是包 ID 加版本号；复用版本号会让消费者（以及验证设施）在
`restore` 成功的情况下静默拿到旧 DLL。

消费者升级时，应更新 `PackageReference` 的版本并执行 restore，而不是把清空
缓存当作正常升级手段。清理单个本地包目录仅是定位缓存问题的应急措施，不能替代
正确的版本发布。

## 依据与待组织确认项

- **已实测依据**：`scripts/Verify-ExternalConsumer.ps1` 的 Variant A 使用这里的
  三包引用、`net8.0-windows10.0.19041.0`、`x64`、`UseWinUI`、
  `EnableMsixTooling`、两项 `App.xaml` 合并以及
  `using:Ether.DesignSystem.Controls`；其 restore、build 与运行时标记均通过。
- **已实测兼容项**：同一脚本的 Variant B 额外显式引用
  `Microsoft.WindowsAppSDK` 也通过，因此已有该引用的应用无需删除它。
- **需组织确认**：GitHub Packages 的实际 `<PACKAGE_OWNER>`、PAT 发放渠道、
  token 是否需要 SSO 授权及代理设置不在本仓库测试范围内。首次真实 feed 接入应
  由发布团队提供这些值并完成一次 restore 验证。
