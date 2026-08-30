# 第三方（内部团队）可消费性验证与发布计划

日期：2026-08-30
分支：`codex/refine-components`
范围：`Ether.DesignSystem.Foundation` / `Ether.DesignSystem.Controls` / `Ether.DesignSystem.Interactions`（`0.1.0-preview.1`，TFM `net8.0-windows10.0.19041.0`）
读者：本库维护者。本文档只是计划，不包含任何代码改动。

## 0. 目标与已确认的前提

### 0.1 目标

证明三个 NuGet 包能被一个**与本仓库毫无关系的消费者项目**正确消费，且满足三条硬性要求：

1. 消费方式与微软官方完全一致（对齐 WinUI 3 Gallery 与 Microsoft Learn 的 templated-control / NuGet 消费路径，不走任何私有捷径）；
2. 第三方引用后，所有 UI 固定不变（有可执行的回归门禁，而不是口头承诺）；
3. 所有公开 property / API 都能以官方方式（XAML 标记 + C#）正常使用。

"不许作弊"的操作定义：**验证用的消费者项目不得拥有任何真实消费者拿不到的东西**——不得有 `InternalsVisibleTo` 特权、不得继承本仓库的 MSBuild 配置、不得依赖仓库内的隐式版本注入；验证断言不得为了通过而放宽到失去意义。

### 0.2 已确认的前提（用户已拍板，计划据此定稿）

1. **仅公司内部使用，经 GitHub Packages（私有仓库）分发，不公开发布。** "第三方"= 公司内部的兄弟团队：他们没有本仓库的构建配置、拿不到 internal 成员、在自己的 XAML 里写标记、期望升级包后 UI 不变样。GitHub Packages 的 NuGet registry **要求消费者带 token 认证才能 restore（同组织也一样）**——兄弟团队接入前要配 PAT（`read:packages`）和 `nuget.config`，这是接入的第一道坎，必须纳入验证与文档范围。公开发布相关的一切（nuget.org 校验、许可证表达式、NuGet 包签名）不适用，已从计划移除；"专有/仅限内部使用"声明降为防误公开的可选卫生项（§4.C5）。
2. **消费方机器只有 x64。** 而 `Directory.Build.props` 声明 `Platforms=x86;x64;arm64`、`RuntimeIdentifiers=win-x86;win-x64;win-arm64`，CI 却只跑 `-p:Platform=x64`，ARM64 从未在 CI 构建过——这是在承诺一个从未验证过的支持范围，必须收口（§4.B4，推荐收窄声明）。
3. **packaged（MSIX）与 unpackaged 两种消费形态都要继续验证。** 理由：**`.pri` 资源加载在 packaged 与 unpackaged 下行为不同**，库在一种形态下正常、另一种形态下资源找不到，是 WinUI 3 库最经典的真实失败模式之一（§2.2 的官方 issue 佐证）。仓库已具备两套 fixture（`Packaged/` 含 `Package.appxmanifest`，`Verify-MsixPackage.ps1` 会构建出真 MSIX），但该脚本未接进 CI（§4.B5 处理）。**MSIX 签名不在本库范围**——签名是兄弟团队发布自己 App 时的事，本库始终只是 NuGet 包，计划中不为库安排任何签名工作。

---

## 1. 现状核实

以下事实全部在本分支上直接读源码核实过（2026-08-30）。

### 1.1 已有的真实证据（值得保留的资产）

`scripts/Verify-ConsumerFixtures.ps1`（1116 行）已经做到：

- 真 `dotnet pack` 出三个 nupkg → 写入本地 feed（`artifacts/consumer-fixtures/local-feed`）；
- 两套独立消费者项目（`tests/Ether.DesignSystem.ConsumerFixtures/{Unpackaged,Packaged}`）用 `NuGet.Config`（`<clear/>` + 本地 feed + nuget.org）restore，脚本断言 csproj 内**没有** `ProjectReference`（`Assert-NoProjectReference`），并断言 Foundation 只能作为传递依赖到达（`Assert-FoundationFlowsTransitively`）；
- 包结构断言：`lib/net8.0-windows10.0.19041/*.dll` + `.pri`、`Themes/*.xbf`、Foundation 的 `contentFiles` 字体/SVG 与 `buildTransitive/*.targets`、Controls **无** `buildTransitive` 泄漏、nuspec 声明 Foundation 依赖；
- 启动真实 WinUI 宿主（Unpackaged exe），验证 1501 个公开可写属性 getter/setter、482 个视觉属性的变更+布局+`RenderTargetBitmap` 逐项证据、37 个 Ether DP 的 SetValue/GetValue/回调/JSON envelope、12 个交互适配器、资源键、字体、SVG、RTL、UIA、2.25 文本缩放、本地化、OS 高对比度、13 控件 × Light/Dark 截图矩阵；
- 冻结的 token 源文件哈希（`EtherPrimitives/EtherColors/EtherSpacing/EtherTypography/EtherIconGeometries.xaml`）。

这套骨架是对的：**打真包、独立 restore、真宿主运行**正是官方消费路径。问题在于下面的缺口让它"看起来像第三方，实际不是"。

### 1.2 缺口清单（逐条核实，按新前提下的优先级排序）

| 优先级 | 原编号 | 缺口 | 核实结果 |
|---|---|------|---------|
| 1 | 缺口 2 | fixture 继承仓库构建配置 | **属实，兄弟团队接入第一天最可能撞上的墙，价值最高的待修项**。fixture 位于仓库树内，自动继承根部 `Directory.Build.props`（TFM、`Platforms`、`UseWinUI` 等全部来自这里——fixture csproj 里连 `TargetFramework` 都没写）和 `Directory.Packages.props`（CPM，`Microsoft.WindowsAppSDK` 2.3.1 由中央版本喂入，fixture 的 `<PackageReference Include="Microsoft.WindowsAppSDK" />` 无版本号，Ether 包引用用 CPM 语义的 `VersionOverride`）。**后果：我们的 nupkg 自身声明的依赖从未在干净环境中被检验**；兄弟团队没有这些文件，restore 时解析的是包声明的依赖——那份声明若漏/错，他们第一天就 restore 失败，而门禁永远发现不了。 |
| 2 | 缺口 1 | fixture 有 `InternalsVisibleTo` 特权 | **属实**。`src/Ether.DesignSystem.Controls/Ether.DesignSystem.Controls.csproj:33-34` 对两个 fixture 开放 internals；`tests/.../RuntimeVerification.Masthead.cs:140,171,186` 实际调用了 `internal` 方法 `EtherMasthead.RefreshCaptionIconColorForCurrentState`（`src/.../EtherMasthead.xaml.cs:471`）。验收可以在公开表面不足时全绿，兄弟团队却调不到。 |
| 3 | 缺口 3 | "UI 固定不变"零保障 | **属实**。`Assert-ScreenshotMarker` 只断言：文件存在、>128 字节、尺寸>0、light≠dark 哈希。没有任何黄金基线，把所有控件外观改掉也能通过——升级包后 UI 变样是消费方最直接的痛点。已知障碍：截图存在跨运行渲染噪声（`dropdown-light.png` 实测在 2 个哈希间交替），基线不能用字节比对。仓库里已有现成容差算法：`RuntimeVerification.AttachedVisualProperties.cs:200,207` 的 `ChannelToleranceLevels = 1` + `SignificantPixelFraction = 0.0002`。 |
| 4 | 缺口 5 | XAML 标记消费覆盖不足 | **大方向属实，原表述需修正**（见 1.3）。兄弟团队写的就是 `<ether:EtherButton Variant="Tertiary"/>`。 |
| 5 | 缺口 4 | "1501 属性可用"言过其实 | **属实**。`RuntimeVerification.FullPropertyCode.cs:86-87`：在游离（detached）新实例上 `GetValue` 后把**同一个当前值原样写回**（`property.SetValue(detached, value)`）。只证明 setter 不抛异常，不证明值生效。诚实表述：482 个有可观察效果证据，其余约 1019 个仅证明可调用。 |
| 6 | 缺口 6 | Interactions 无 API 稳定性保护 | **属实**。`src/Ether.DesignSystem.Interactions/Ether.DesignSystem.Interactions.csproj` 没有 `Microsoft.CodeAnalysis.PublicApiAnalyzers`、没有 `RS0016;RS0017` WarningsAsErrors，公开表面可静默漂移。**附加发现**：它也缺 `RepositoryUrl`、`RepositoryType`、`EnablePackageValidation`（Controls 和 Foundation 有）。 |
| 7 | 缺口 8 | CI 覆盖不全 | **属实，需细化**（见 1.3）。含两个子项：运行时门禁无强制机制（8a）、平台声明与 CI 脱节 + `Verify-MsixPackage.ps1` 未接 CI（8b）。 |
| 低 | 缺口 7 | 包元数据不全 | **事实属实**（无 `PackageLicenseExpression`/`PackageLicenseFile`/`PackageIcon`，仓库根无 LICENSE），**但仅内部使用下不构成阻断**：无对外授权法律问题，nuget.org 校验不适用。降为可选卫生项（§4.C5）：加"专有/仅限内部使用"声明，目的是防将来误公开。 |

### 1.3 对交办事实的两处修正 / 细化

1. **缺口 5 的原表述不准确**。"唯一在标记里设的属性是 `Style=`"不成立：`tests/.../Unpackaged/MainWindow.xaml` 实际在标记里设置了多个 Ether DP 与平台 DP——`EtherProgressBar` 的 `Title`/`ValueContent`/`Value`、`EtherSteeringBar` 的同名三项、`EtherMasthead` 的 `EnableWindowCommands="False"`、`EtherInput.PlaceholderText`、`EtherDropdown.SelectedIndex`、`ToggleSwitch.IsOn` 等。**缺口的真实形状**是：标记覆盖的只有 string / object / double / bool / int 这几类"顺路"的属性，而**枚举类型 DP（`Variant="Tertiary"`、`Size="Small"`）、集合类型 DP（`Stops`、`Labels`）、以及绝大多数 Ether DP 从未走过 XAML 标记路径**。XAML 设 DP 走 XamlMetadataProvider 的类型解析/值转换，与 C# setter 是完全不同的代码路径，枚举和集合正是最容易翻车的类型。缺口成立，范围收窄。
2. **缺口 8 需细化**。`Verify-Arm64Packages.ps1`、`Verify-MsixPackage.ps1`、`Verify-GallerySmoke.ps1` 并非完全无人调用——它们已被 `scripts/Verify-RuntimeGates.ps1` 串联（该脚本自我定位为"本地/自托管桌面 owner"，注明 hosted CI 不得调用）。真实缺口是：(a) **这条本地门禁没有任何机制保证发布前一定跑过**——不在 CI，不在发布流程强制点上，全靠人自觉；(b) CI 构建矩阵只有 x64，`Platforms` 声明的另两个平台连编译都不在 CI 覆盖内（ARM64 从未在 CI 构建过），且 `Verify-MsixPackage.ps1` 未接进 CI。

### 1.4 计划过程中的新发现（原清单之外）

- **N1（重要，发布后不可逆）**：`Ether.DesignSystem.Controls` 的 `RootNamespace` 是 `Ether.DesignSystem.Controls`（csproj:4），消费者 XAML 必须写 `xmlns:ether="using:Ether.DesignSystem.Controls"`——与包名 `Ether.DesignSystem.Controls` 不一致，且暴露内部代号 "Sandbox"。包一旦被兄弟团队接入，改命名空间就是 breaking change——**发布前是唯一的零成本改名窗口**（决策见 §4.A5）。
- **N2**：仓库根残留 `EtherComponentSandbox.csproj` / `EtherComponentSandbox.slnx` / `app.manifest` / 根部 `Assets/`、`Fonts/` 等 sandbox 时代文件。不进包，但混淆"什么是产品"（§4.C5）。
- **N3**：`Directory.Build.props.example` 已自我声明废弃，可删（§4.C5）。

---

## 2. 官方基准：微软怎么做

计划中每一项的做法都对照以下官方来源，避免自造非标准路径。内部分发不改变任何消费机制——GitHub Packages 只是 `nuget.config` 里的另一个 source（多一步 PAT 认证），restore/build/run 路径与公开包完全一致。

### 2.1 Templated control 授权约定（Microsoft Learn）

[Build XAML templated controls (WinUI 3)](https://learn.microsoft.com/en-us/windows/apps/winui/winui3/xaml-templated-controls-winui-3) 确立的官方约定：

- 构造函数设 `DefaultStyleKey = typeof(X)`；
- 默认样式必须在 `Themes/Generic.xaml`，**文件夹名与文件名是框架硬性要求**；
- DP 声明模式：`public static readonly DependencyProperty XProperty = DependencyProperty.Register(nameof(X), typeof(T), typeof(Owner), new PropertyMetadata(default, OnXChanged))` + CLR wrapper 走 `GetValue`/`SetValue`；
- 模板交互经 `OnApplyTemplate` 取 template part；官方消费示例就是**XAML 标记设属性**（`<local:BgLabelControl Background="Red" Label="Hello, World!"/>`）。

仓库现有 `scripts/Verify-WinUiConventions.ps1` 已在 CI 上检查这类约定，方向正确，保留并继续收紧。

### 2.2 WinUI 3 库经 NuGet 消费（Microsoft Learn + microsoft-ui-xaml issues）

- [C# 组件 NuGet 消费 walkthrough](https://learn.microsoft.com/en-us/windows/apps/develop/platform/csharp-winrt/create-winrt-component-winui-cswinrt) 明确了 C# WinUI 组件被别的 App SDK 应用消费的官方路径，并承认 NuGet 引用形态有已知限制；
- [microsoft-ui-xaml #7830](https://github.com/microsoft/microsoft-ui-xaml/issues/7830) 与 [#10970](https://github.com/microsoft/microsoft-ui-xaml/issues/10970) 记录了 WinUI 3 控件库打 NuGet 的官方已知坑（`Generic.xbf` 定位失败、`ms-appx:///{Assembly}/Themes/Generic.xaml` 解析失败等），且**packaged 与 unpackaged 下 `.pri` 资源解析行为不同**是其中反复出现的主题。这正说明两点：端到端消费验证必须保留并强化（构建成功推断不出"能跑"）；packaged/unpackaged 双形态都必须验（§0.2 前提 3 的依据）。
- 本库当前的包布局（`lib/{tfm}/{AssemblyName}/Themes/*.xbf` + `.pri` 与 dll 同层）与 Windows App SDK 自身及 CommunityToolkit.WinUI 系列包一致，属于已被生态验证的做法，**不是作弊**；Foundation 用 `contentFiles` + `buildTransitive` targets 把字体/SVG 落到宿主输出，也是 NuGet 官方的 asset 传递机制。

### 2.3 WinUI 3 Gallery 的样例组织（microsoft/WinUI-Gallery）

[WinUI Gallery 仓库](https://github.com/microsoft/WinUI-Gallery)的模式：每个控件一个 sample page，页面用 `ControlExample` 宿主同时展示**活控件 + 生成它的 XAML 标记与 codebehind**，控件目录由 `ControlInfoData.json` 元数据驱动。对本库的含义：

- `samples/Ether.DesignSystem.Gallery` 已按每控件一页组织（`Views/Controls/*Page.xaml`），CI 有 `Verify-GalleryControlExample.ps1`/`Verify-GalleryLocalization.ps1` 把关，方向与 Gallery 一致；
- Gallery 展示的标记就是"官方推荐的消费写法"。因此**Gallery 页面里出现过的每一种标记写法，都必须能在纯 NuGet 消费者里原样工作**——这是 §4.A4 XAML 矩阵的取材来源。

### 2.4 MSBuild 配置边界（Microsoft Learn）

[Customize your build（Directory.Build.props 查找规则）](https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-by-directory)：MSBuild 从项目目录向上找**第一个** `Directory.Build.props` 即停，找不到就没有。把消费者项目物理放在仓库树之外，继承便由构造保证为零——这是 §4.A1 第一层的官方机制基础，不是 hack。

---

## 3. 计划总览

三档优先级。每项给出：要证明什么 / 怎么做 / 成功判据 / 为什么符合官方约定。

```
A. 发布阻断（不做完不许向内部 feed push）
  A1  两层真实消费验证：仓库外消费者（CI 层）+ GitHub Packages 彩排层   [缺口2，第一优先]
  A2  移除 InternalsVisibleTo，fixture 去特权化                        [缺口1]
  A3  UI 黄金基线门禁（容差比对）                                       [缺口3]
  A4  XAML 标记消费矩阵（全部公开 DP 的官方写法）                        [缺口5]
  A5  RootNamespace 决策（Ether.DesignSystem.Controls → ?）                   [N1，需维护者拍板]

B. 发布前必须（tag 之前完成）
  B1  属性验证诚实化 + setter 升级为 round-trip                         [缺口4]
  B2  Interactions 接入 PublicApiAnalyzers                             [缺口6]
  B3  发布门禁流程化：RuntimeGates + GitHub Packages 彩排成为放行条件    [缺口8a]
  B4  平台声明收口：Platforms 与 CI 现实对齐（只支持 x64）               [缺口8b]
  B5  MSIX 双形态验证接入 CI（Verify-MsixPackage.ps1）                  [缺口8b]
  B6  消费方接入文档（GitHub Packages 认证 + 与 Gallery 一致的用法）

C. 发布后可做 / 可选
  C1  OS 版本矩阵（17763 floor 实测）
  C2  API 参考文档站
  C3  Appium/UIA 真输入交互自动化
  C4  WindowsAppSDK 版本兼容矩阵
  C5  卫生项：内部专有声明、Interactions 元数据对齐、仓库残留清理
  C6  自托管 Windows runner（把本地运行时门禁自动化）
```

---

## 4. 分项计划

### A1 — 两层真实消费验证 【发布阻断，第一优先，整个计划价值最高的一项】

- **要证明什么**：(第一层) 我们的 nupkg **自身声明的依赖**足以让一个与仓库零关系的项目 restore/build/run——不靠仓库 CPM 暗中喂版本、不靠 `Directory.Build.props` 暗中喂 TFM；(第二层) 兄弟团队**照文档**就能从 GitHub Packages 拉到包并跑起来，包括认证这道坎。
- **怎么做**：

  **第一层（进 CI，每次跑，无网络无密钥依赖）**：
  1. **消费者项目移出仓库树**：`Verify-ConsumerFixtures.ps1` 在验证时把 fixture 项目文件与共享的 `RuntimeVerification*.cs` **复制**到仓库树之外的工作区（如 `$env:TEMP\ether-consumer-<guid>\`），在那里 restore/build/run。仓库树之外意味着按 MSBuild 查找规则（§2.4）**由构造保证**不继承 `Directory.Build.props`/`Directory.Packages.props`；工作区内再放一份空的哨兵 `Directory.Build.props`/`Directory.Packages.props`（`<Project/>` + `ManagePackageVersionsCentrally=false`）作为双保险（防范极端情况下盘根存在同名文件）。源文件仍在仓库受版本管理，复制只是执行形态——这与"第三方拿到源码模板自己建项目"同构，不构成特权。
  2. **fixture csproj 写成真消费者的样子**：以 Windows App SDK 官方模板（Blank App, Packaged / Unpackaged）为蓝本——显式 `TargetFramework=net8.0-windows10.0.19041.0`、`TargetPlatformMinVersion`、`Platforms`、`UseWinUI`（模板由 SDK 隐式设定的除外）、**所有 `PackageReference` 带显式 `Version`**（`Microsoft.WindowsAppSDK` 写死版本；Ether 包引用把 CPM 语义的 `VersionOverride` 改为普通 `Version`）。
  3. **restore 源**：工作区自带 `NuGet.Config`——`<clear/>` + 本地文件夹 feed（pack 输出）+ nuget.org（上游依赖）。
  4. **反继承断言**：脚本对工作区项目跑 `dotnet msbuild -getProperty:ManagePackageVersionsCentrally,TargetFramework,Platforms`，断言 CPM=false、TFM 来自项目自身；并断言 csproj 中每个 `PackageReference` 有显式 `Version`。
  5. **一次性 mutation 灵敏度验证**（进本项验收记录，不进 CI）：临时把 Controls csproj 的 WindowsAppSDK 引用标 `PrivateAssets="all"`（让 nuspec 漏掉该依赖），重跑门禁，确认第一层 restore/build **失败**；恢复后确认通过。结论写进脚本注释——证明门禁真的在检验 nuspec 依赖声明。

  **第二层（发布彩排，手动或定期，B3 的放行条件之一）**：
  6. 真把三个包 push 到 GitHub Packages 私有 feed（预发布版本号或专用彩排 feed）；
  7. 在一台干净机器（或至少干净用户环境/容器化 SDK 环境）上，只按 §4.B6 的接入文档操作：配 PAT（`read:packages`）+ `nuget.config` → `dotnet restore` → build → 运行 fixture 断言全绿；
  8. 彩排结果（restore 日志、marker JSON、机器环境）归档到 `artifacts/release-evidence/{version}/`。
- **成功判据**：第一层 mutation 测试红→绿；工作区 `project.assets.json` 中 WindowsAppSDK 版本来源=项目显式声明 ∩ 包依赖范围，而非 CPM；现有全部运行时断言在新形态下仍通过；第二层在干净环境凭文档走通一次并留证。
- **为何官方**：消费者拿到的就是 VS 模板 + `dotnet add package` + feed 认证；第一层把 fixture 精确对齐到那个形态，第二层把 feed 与认证也对齐。`Directory.Build.props` 就近即停是 MSBuild 文档记载的行为（§2.4）。
- **工作量**：第一层 2–3 天（含两套 fixture 调通与 mutation 验证）；第二层 1 天（依赖 GitHub Packages feed 就绪）。
- **依赖**：无。第一个做——它改变 fixture 形态，A3/A4 都应落在新形态上。

### A2 — 移除 `InternalsVisibleTo`，fixture 去特权化 【发布阻断】

- **要证明什么**：验证消费者与兄弟团队拥有完全相同的可见性——只看得到公开 API。只要 `InternalsVisibleTo` 存在，任何"第三方能用"的结论都带星号：验收可能在公开表面不足时照样全绿。
- **怎么做**：
  1. 删除 `src/Ether.DesignSystem.Controls/Ether.DesignSystem.Controls.csproj:33-34` 的两条 `InternalsVisibleTo`；
  2. 处理唯一的 internal 消费点 `RuntimeVerification.Masthead.cs`（调用 `RefreshCaptionIconColorForCurrentState` 断言 caption 图标主题跟随）。二选一，推荐 (a)：
     (a) 改写为**公开可观察路径**：切换 `RootGrid.RequestedTheme` 后等 compositor settle（复用 AttachedVisualProperties 的稳定采样法），对 caption 按钮区域做 `RenderTargetBitmap` 像素断言（Light/Dark 下图标颜色分别命中期望值）——测的是消费者真正看得见的行为；
     (b) 若像素断言在该场景确实不可稳定，把这条移入仓库内的单元/集成测试项目（那里可正当拥有 internals），fixture 中降级为公开行为断言。**不允许**"保留 IVT 说以后再删"。
  3. `Verify-ConsumerFixtures.ps1` 增加静态断言：对 `src/**/*.csproj` grep `InternalsVisibleTo`，凡指向 `*ConsumerFixtures*` 即 fail（防回归）。
- **成功判据**：`src` 下零 `InternalsVisibleTo` 指向 fixture；fixture 全量编译通过且无 internal 成员引用；Masthead 主题断言仍在、且破坏 `ActualThemeChanged` 逻辑时仍会红（临时 mutation 验证灵敏度）。
- **为何官方**：微软自己的库测试（WinUI、Toolkit）区分"白盒单测（可 IVT）"与"端到端样例/包验证（纯公开 API）"，消费者证据必须属于后者。
- **工作量**：0.5–1.5 天。
- **依赖**：建议在 A1 第一层之后（落在新 fixture 形态上）。

### A3 — UI 黄金基线门禁 【发布阻断】

- **要证明什么**：硬性要求 2——兄弟团队升级包后 UI 固定不变。当前对外观的唯一约束是"light≠dark"，等于没有。
- **怎么做**：
  1. **基线资产**：把当前（设计验收过的）13 控件 × {Light, Dark} 逐控件 `RenderTargetBitmap` PNG 提交到 `tests/Ether.DesignSystem.ConsumerFixtures/Baselines/`，附 `baseline-manifest.json`（每张图的像素尺寸、生成机器 OS build、显示缩放、生成日期、来源 commit）。整页截图噪声源多（窗口 chrome 等），**基线只用逐控件捕获**；整页截图保留现有弱断言。
  2. **比对算法**：不用字节/哈希比对（已实测跨运行渲染噪声：`dropdown-light.png` 在 2 个哈希间交替）。复用仓库内已被 482 属性审计验证过的算法：逐像素 4 通道差 > `ChannelToleranceLevels(=1)` 记为变化像素，变化像素数超过 `max(MinimumSignificantPixelCount, 像素总数 × SignificantPixelFraction(=0.0002))` 即判失败。**尺寸必须逐像素相等**（尺寸变化不进容差）。比对实现放在 fixture 托管代码（与截图同进程、同像素格式），脚本只核对结论，避免第二套 PNG 解码器差异。
  3. **失败输出**：落三件套（baseline / actual / 差异热力图 PNG）到 `artifacts/`，供人眼裁决。
  4. **基线更新流程**：`-UpdateVisualBaselines` 显式开关才允许重写；基线 PNG 受 git 管，更新以二进制 diff 出现在 PR + PR 描述附前后对比图。默认路径下基线只读。
  5. **环境钉死**：基线门禁运行在与生成基线同类环境（本地/自托管桌面，与 RuntimeGates 同机策略）；manifest 记录环境，比对前校验匹配（缩放因子必须 100%，不匹配直接 fail 并提示，而不是产出不可比的图）。
  6. **双保险（源侧冻结)**：仿照现有 token 哈希冻结，把 `Themes/Generic.xaml`、`Themes/DesignSystem.xaml` 及各控件样式 XAML 加入"变更需显式确认"的哈希清单（哈希变了必须同步更新基线或说明为何视觉无变化）。源冻结抓"改了源"，像素基线抓"渲染结果变了"，互为补充。
- **成功判据**：mutation 验证——临时改一个控件的填充色 brush，门禁必须红；恢复后连续 3 次全量运行绿（容差吸收已知噪声、又没吞掉真实变更）。
- **为何官方**：视觉回归基线 + 容差比对是 WinUI/Win2D/Toolkit 生态测试基础设施的通行做法；捕获用公开 API `RenderTargetBitmap`，且在消费者宿主进程内完成，不引入私有渲染路径。
- **工作量**：2–3 天。
- **依赖**：A1 第一层、A2 先行（基线生成应发生在去特权化后的 fixture 上）。

### A4 — XAML 标记消费矩阵 【发布阻断】

- **要证明什么**：硬性要求 3 在**官方消费语法**下成立：每个公开 Ether DP 都能用 XAML 字面量标记设置并生效（枚举、bool、double、string、集合），因为这才是 Gallery 展示、兄弟团队实际书写的形态（`<ether:EtherButton Variant="Tertiary"/>`）。
- **怎么做**：
  1. 在两套 fixture 的 XAML 中新增 "MarkupMatrix" 区（必须在视觉树内实例化），为**全部 37 个 Ether DP** 各放至少一个用标记字面量设为非默认值的实例，重点覆盖：枚举（`Variant="Tertiary"`、`Size="Small"`）、bool（`ShowLabels="False"` 等）、double/int、string、**集合类 DP 用 property element 语法**（`<controls:EtherSlider.Stops>…`）；再抽样若干继承的平台 DP（`CornerRadius`、`Padding` 这类带 type converter 的）。
  2. 运行时断言：对矩阵中每个实例读回 DP 值 == 标记声明的期望值，且控件 `defaultStyleResolved`、可布局、可渲染（复用现有基建）。结果并入 runtime marker JSON，脚本端加计数断言（矩阵条目数 == 37 + 抽样数，防静默缩水）。
  3. 取材对照：凡 `samples/Ether.DesignSystem.Gallery/Views/Controls/*Page.xaml` 出现过的标记写法，必须在矩阵中有等价条目（脚本比对 Gallery 页面 attribute 集合与矩阵覆盖集，纳入 `Verify-GalleryControlExample.ps1` 或新断言）。
- **成功判据**：矩阵计数断言通过；mutation 验证——临时破坏某枚举 DP 的注册（改类型/删枚举成员），XAML 加载必须失败或断言必须红。
- **为何官方**：Learn 的 templated-control 教程与 Gallery 的每个示例都以 XAML 标记消费为第一路径（§2.1/§2.3）；只测 C# setter 恰恰绕过了官方主路径。
- **工作量**：2–3 天。
- **依赖**：A1 第一层（新 fixture 形态）；A5 决策（xmlns 定稿后再写，避免返工）。

### A5 — RootNamespace 决策 【发布阻断（决策本身），改动视决策】

- **问题**：消费者 XAML 必须写 `xmlns:ether="using:Ether.DesignSystem.Controls"`，与包名不符且暴露内部代号 "Sandbox"。包被兄弟团队接入后再改 = breaking change，届时要协调所有消费方——**发布前是唯一的零成本改名窗口**。
- **怎么做**：维护者拍板改/不改（产品命名决策，本计划不代做）。若改名：改 `RootNamespace`/命名空间 → 全仓刷新（Gallery、fixtures、脚本断言、`PublicAPI.Shipped/Unshipped.txt` 全量换列、13 个 L3 合约脚本）。若保留：README 与包描述显式写明 xmlns 写法，避免消费者猜。
- **工作量**：保留 0.1 天；改名 1–2 天（机械但面广）。

### B1 — 属性验证诚实化 + setter round-trip 【发布前必须】

- **要证明什么**：对内声明与证据强度一致；把"仅可调用"升级为"写入生效"。
- **怎么做**：
  1. **文案先行**（0 风险）：`Verify-ConsumerFixtures.ps1` 成功输出、包 README、release notes 统一改为："482 个视觉属性有逐项可观察效果证据（像素/布局/可见性/契约）；另外 1019 个公开可写属性验证了 getter/setter 可调用不抛异常"。删除一切"1501 个属性正常使用"级别的表述。
  2. **setter 升级**：`RuntimeVerification.FullPropertyCode.cs` 的 `TryInvokeOnUnattachedInstance` 从"原值写回"升级为**写非当前值并读回**：按类型生成扰动值（bool 取反、数值 +1（尊重 min/max/NaN 语义）、string 加后缀、enum 取下一成员、引用类型可构造者 new 一个），`SetValue` 后 `GetValue` 断言读回==写入（coercion 属性单列白名单并记录 coerce 后期望）。写不进/读不回的属性进入显式分类清单（平台只读镜像、生命周期绑定等），清单计数进 marker 断言，禁止无声跳过。
- **成功判据**：round-trip 计数 + 分类计数 == 1501 恒等式成立；两轮连续运行 0 差异（维持现有确定性标准）；文案零处夸大。
- **为何官方**：DP 契约（§2.1）的语义就是 set 后 get 返回该值（或 coerce 后的值），round-trip 是对官方契约的直测。
- **工作量**：1.5–2 天（长尾在 coercion 白名单）。

### B2 — Interactions 接入 PublicApiAnalyzers 【发布前必须】

- **怎么做**：给 `Ether.DesignSystem.Interactions.csproj` 加 `Microsoft.CodeAnalysis.PublicApiAnalyzers`（版本走 CPM）、`PublicAPI.Shipped.txt`/`PublicAPI.Unshipped.txt`（首轮用 analyzer 自动生成基线）、`WarningsAsErrors=RS0016;RS0017`，与 Controls/Foundation 完全对齐；顺手补它缺的 `RepositoryUrl`/`RepositoryType`/`EnablePackageValidation`（内部消费同样受益于 SourceLink 溯源与包校验，成本一行级）。
- **成功判据**：Release 构建下故意加公开方法不更新 txt → 构建失败；恢复后通过。
- **为何官方**：与本仓库另两个包及 .NET 团队自身的 API 基线实践一致。
- **工作量**：0.5 天。

### B3 — 发布门禁流程化 【发布前必须】

- **要证明什么**：完整运行时验收（`Verify-RuntimeGates.ps1` = ConsumerFixtures 全量 + GallerySmoke + MSIX + 平台 pack）与 A1 第二层彩排在每次向 GitHub Packages 发布前**必然**跑过，而不是靠自觉。hosted runner 起不了 WinUI GUI 是事实（workflow 注释成立），所以方案是流程强制而非硬搬上 hosted CI。
- **怎么做**（按成本递增，先做 1，2 建议做，3 是终态见 C6）：
  1. **发布脚本强制**：新增 `scripts/Publish-Internal.ps1` 作为唯一发布入口：先完整跑 `Verify-RuntimeGates.ps1`，把 marker JSON + 基线比对结论 + 运行环境写成 `artifacts/release-evidence/{version}/`，校验通过后才执行 `dotnet nuget push` 到 GitHub Packages（`https://nuget.pkg.github.com/{org}/index.json`，PAT 走环境变量/凭据管理器，不落盘）。push 后立即执行 A1 第二层彩排并把证据并入同一目录。文档规定：不经此脚本的 push 视为无效发布。
  2. **留痕**：发布走 tag 触发的 workflow，一个 job 校验并上传 evidence 目录为 artifact（版本号、marker outcome==success、时间戳在 tag 前 48h 内），作为 release 准入检查。
- **成功判据**：删掉 evidence 或 marker outcome ≠ success 时，Publish-Internal 拒绝 push；彩排失败则本次发布作废并回滚（GitHub Packages 支持删除版本）。
- **工作量**：1 天（不含 feed/PAT 首次配置）。

### B4 — 平台声明收口：只支持 x64 【发布前必须】

- **问题**：`Directory.Build.props` 声明 `Platforms=x86;x64;arm64`、`RuntimeIdentifiers=win-x86;win-x64;win-arm64`，但 CI 只构建 x64，ARM64 从未在 CI 构建过——承诺了从未验证的支持范围。消费方已确认只有 x64。
- **怎么做**（推荐前者）：
  - **方案一（推荐）：收窄声明与现实一致**。`Platforms=x64`、`RuntimeIdentifiers=win-x64`；`Verify-Arm64Packages.ps1` 停用（从 `Verify-RuntimeGates.ps1` 串联中移除，脚本可保留在 `scripts/` 并在头部注明"未启用——仅当未来出现 ARM64 消费者时恢复"）；README 明写"当前仅支持 x64"。将来真有 ARM64 需求时，恢复声明的同时必须一并恢复验证——声明与证据同进退。
  - 方案二：保留声明，但 README 与包描述显式标注 x86/arm64 "未验证、不支持"，并在 CI 至少加这两个平台的编译级覆盖。缺点：继续为用不上的平台付维护成本，且"声明但不支持"对消费者仍是坑。
- **成功判据**：声明的每个 `Platforms` 成员都有 CI 级构建证据（方案一下自动满足）；文档、csproj、CI 三处口径一致。
- **工作量**：0.5 天。

### B5 — MSIX 双形态验证接入 CI 【发布前必须】

- **要证明什么**：库在 packaged（MSIX）与 unpackaged 两种宿主形态下都能正确加载——**`.pri` 资源解析在两种形态下走不同路径**（§0.2 前提 3、§2.2），一种形态正常另一种资源找不到是 WinUI 3 库的经典真实故障。
- **怎么做**：
  1. `Verify-MsixPackage.ps1`（构建 Packaged fixture 产出真 MSIX 并断言）**接进 hosted CI** 的 `package-consumers` job——MSIX 的构建与包结构断言不需要 GUI，hosted runner 能跑；若脚本内含安装/启动等 GUI 步骤，拆 `-ProduceOnly` 开关，GUI 部分留在本地 `Verify-RuntimeGates.ps1`；
  2. Packaged fixture 的**运行时**验证（安装 MSIX → 启动 → 跑 marker 断言）作为本地 RuntimeGates 的组成部分（当前 Packaged 只 build 不 run，升级为真跑；A1 第一层的工作区形态同样适用于它）；
  3. 明确边界：本库不做 MSIX 签名——签名属于兄弟团队发布自己 App 的环节；本地验证用测试证书或 `Add-AppxPackage -Register` 旁加载路径即可，README 中说明。
- **成功判据**：CI 上每次产出并断言 MSIX 结构；本地门禁中 packaged fixture 真实安装运行且 marker 全绿；mutation 验证——临时破坏 `.pri` 打包（如移除 lib 下 `.pri`），packaged 或 unpackaged 至少一侧必须红。
- **为何官方**：packaged/unpackaged 双形态就是 Windows App SDK 官方支持的两种部署形态；两种都验是对官方支持矩阵的直接覆盖。
- **工作量**：1–1.5 天（Packaged 真跑是主要成本）。

### B6 — 消费方接入文档 【发布前必须】

- **怎么做**：三个包的 README（已 `PackageReadmeFile`）+ 仓库 `docs/consuming.md` 重写为兄弟团队视角，必须包含：
  1. **GitHub Packages 接入**：`nuget.config` 样例（`<packageSources>` 加 `https://nuget.pkg.github.com/{org}/index.json` + `<packageSourceCredentials>` 用环境变量引用 PAT，明确警告不要把 PAT 明文提交）；PAT 权限范围（`read:packages`，组织若开 SSO 需 authorize）；**常见失败症状与排查表**：401（PAT 缺失/过期/未授权 SSO）、404（feed URL 的 org 段写错、包名拼错）、`Unable to load the service index`（代理/网络）、restore 到了同名公共包（source mapping 建议）；
  2. 安装命令、最小 csproj（对照 A1 第一层的消费者形态，含显式 WindowsAppSDK 版本）、`xmlns` 声明（按 A5 决策）、App.xaml 合并 `DesignSystem.xaml` 的准确写法；
  3. 每控件一段与 Gallery 页面一致的标记示例；packaged/unpackaged 差异说明（含 `.pri` 行为差异一句话科普）；"当前仅支持 x64"（B4）；
  4. README 中每个代码块必须能在 fixture/Gallery 找到等价物（防文档腐烂，可脚本抽查）。
- **成功判据**：A1 第二层彩排**只凭这份文档**在干净环境走通（文档即测试用例）；一个不熟悉本仓库的同事按文档从零建项目跑出 Gallery 首页同款按钮，做一次真人验证并记录。
- **工作量**：1 天。

### C — 发布后可做 / 可选

| # | 事项 | 说明 |
|---|------|------|
| C1 | OS 矩阵 | `TargetPlatformMinVersion=10.0.17763.0` 从未在 17763 实机验证；按消费方实际 OS 基线补一轮。 |
| C2 | API 参考站 | 由 `GenerateDocumentationFile` 的 XML docs 生成（DocFX 等），发内部站点。 |
| C3 | 真输入交互自动化 | 现有交互验证是进程内适配器级；补 Appium/WinAppDriver 级真键鼠/触摸路径（release notes 已自认 "Insights/Appium 未完成"）。 |
| C4 | WASDK 兼容矩阵 | 用高于 2.3.1 的 stable WindowsAppSDK restore 消费者项目，验证包声明的版本范围对消费方升级 WASDK 的兼容性。 |
| C5 | 卫生项（可选） | (a) 仓库根加"专有/仅限公司内部使用"声明文件并在 csproj 标注——目的是防将来误公开，非必需；(b) 清理 `EtherComponentSandbox.*`、根部 `Assets/`/`Fonts/` 残留与已废弃的 `Directory.Build.props.example`。 |
| C6 | 自托管 Windows runner | 把 `Verify-RuntimeGates.ps1`（含 packaged 真跑、基线比对）搬上自托管 interactive runner，彻底去掉 B3 的人肉环节。 |

---

## 5. 依赖顺序与工作量汇总

```
A1第一层 (2–3d) ──┬─→ A2 (0.5–1.5d) ─→ A3 (2–3d) ─→ B1 (1.5–2d) ─→ B3 (1d, 含A1第二层彩排 1d)
                  └─（A5决策，维护者）→ A4 (2–3d) ─→ B6 (1d)
B2 (0.5d, 随时)    B4 (0.5d, 随时)    B5 (1–1.5d, A1第一层后)
```

- 关键路径：A1 第一层 → A2 → A3 → B1 → B3（含彩排），约 8–10.5 人日；
- 全部 A+B：约 **13–17 人日**（单人约 3 周），不含 A5 决策等待与 C 档；
- 最早可发布点：A 档全绿 + B 档全绿 + `Publish-Internal.ps1` 完成一次带 evidence 的真实 GitHub Packages push + 第二层彩排全绿。

---

## 6. 引用来源

- Microsoft Learn — [Build XAML templated controls (WinUI 3)](https://learn.microsoft.com/en-us/windows/apps/winui/winui3/xaml-templated-controls-winui-3)（DefaultStyleKey、Themes/Generic.xaml 硬性命名、DP 声明模式、OnApplyTemplate、标记消费示例）
- Microsoft Learn — [Create a C# component with WinUI controls…](https://learn.microsoft.com/en-us/windows/apps/develop/platform/csharp-winrt/create-winrt-component-winui-cswinrt)（组件 NuGet 消费官方路径与已知限制）
- microsoft/WinUI-Gallery — [仓库](https://github.com/microsoft/WinUI-Gallery)（每控件 sample page、ControlExample、ControlInfoData.json 元数据驱动、页面展示 markup+codebehind）
- microsoft/microsoft-ui-xaml — [#7830](https://github.com/microsoft/microsoft-ui-xaml/issues/7830)、[#10970](https://github.com/microsoft/microsoft-ui-xaml/issues/10970)（WinUI 3 控件库 NuGet 打包已知问题：`Generic.xbf`/`.pri` 在 packaged 与 unpackaged 下的解析差异，佐证端到端双形态消费验证的必要性）
- Microsoft Learn — [Customize your build（Directory.Build.props 查找规则）](https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-by-directory)（就近即停，A1 第一层"仓库树之外由构造保证零继承"的官方机制）

---

## 7. 发布版本纪律

**任何内容变化都必须升版本号，绝不复用已打过的版本号。** NuGet 按包 ID 加版本号缓存；复用版本号会让消费者以及验证脚本静默拿到旧包。本地曾发生过一次：命名空间改名后重新打包但版本号未变，全局缓存命中旧包，导致外部消费者验证全线编译失败，而 `restore` 仍报告成功。
