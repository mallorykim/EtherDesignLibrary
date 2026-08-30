# 接入 Ether Design System（内部团队）

本文面向公司内部 WinUI 3 应用团队。当前发布线为预览版
`0.1.0-preview.1`，仅通过私有 GitHub Packages 分发；不要将包或 PAT
配置公开到外部仓库。

## 已验证的支持范围

- 目标框架：`net8.0-windows10.0.19041.0`
- 最低 Windows 平台版本：`10.0.17763.0`
- 架构：仅 `x64`
- 宿主：已验证 unpackaged 与 packaged（MSIX）两种形态。
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

## 4. 已知边界

- 仅支持 `x64`。请将应用构建为 `x64`，不要把 x86 或 ARM64 当作受支持目标。
- `EtherInput` 继承了 `TextBox` 的 `Header` / `HeaderTemplate`，但 Ether 模板
  **不渲染** header 或 description slot。设置这两个属性不会显示内容，也没有
  运行时提示。请使用外部 `TextBlock`、表单标签或自己的表单容器提供标签与说明。
- 当前版本是 preview，公开 API、样式和资源契约仍可能变化。升级前请阅读对应
  发布说明并在应用中回归关键页面。
- `Ether.DesignSystem.Controls.Primitives.HandContentControl` 虽因 XAML 资源解析
  而公开，但它是模板实现支撑类型，不是受支持的设计系统控件契约；请使用命名的
  `Ether*` 控件。
- Controls 与 Foundation 在当前 preview 发布线按同一版本使用；升级时应一起升级
  两者。Interaction 包也应与发布公告给出的版本组合保持一致。

## 5. 版本纪律

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
