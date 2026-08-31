# WinUI 3 设计库：全属性可消费审核交接

**交接日期：** 2026-08-29（本文档已修订，见 §10 修订记录；2026-08-30 再次修订，新增 §4.7、§11，见 §10 第 5-6 条）  
**审核分支：** `codex/refine-components`  
**最终状态：** 通过（完整属性代码、视觉、组件 DP 后端消费与 WinUI 3 约定门禁，以及新增的 High Contrast 前景/背景配对门禁），且视觉证据分布已验证为跨运行确定性可复现  
**主要证据：** `artifacts/audit-runs/consumer-runtime-evidence-20260830-002407286/`（对 2026-08-30 修订后的最终代码状态连续两次独立完整运行——`...20260830-001800579`、`...20260830-002407286`——逐属性证据 Method 完全一致；见 §4.7 与 §6）。2026-08-29 当天定稿时还有五次独立运行彼此一致，见修订前记录（`...230136589`、`...230519527`、`...224425998`、`...224946229`、`...225342824`）——这五次运行的代码状态在 §4.7 描述的灵敏度回归引入之后已不再代表当前状态，仅作历史存档。

## 1. 交接结论

本轮已完成 Ether WinUI 3 设计组件库的全属性可消费验收。验证不是以源项目引用执行，而是先打包 `Ether.DesignSystem.Foundation`、`Ether.DesignSystem.Controls`、`Ether.DesignSystem.Interactions`，再由独立的未打包消费者恢复并加载 NuGet 包执行。

最终运行标记为 `success`，并证明：

| 验收项目 | 结果 | 证明方式 |
| --- | ---: | --- |
| 公开可写属性 | 1,501 / 1,501 | 逐项 CLR getter 在真实控件实例上；逐项 setter 在同类型的游离（未附着）实例上——见 §4.2 的准确措辞 |
| 视觉属性 | 482 / 482 | 附着 WinUI 视觉树、变更、布局、`RenderTargetBitmap` 渲染及逐项证据 |
| Ether 自有 DP | 37 / 37 | `SetValue`、CLR getter、`GetValue`、变更回调、JSON envelope |
| 标准交互适配器 | 12 / 12 | 标准 WinUI 业务交互 envelope |
| 控件截图矩阵 | 13 控件 × Light/Dark | 26 张独立控件 PNG，另有整页 Light/Dark/OS High Contrast PNG |
| WinUI 3 约定门禁 | 通过 | `Verify-WinUiConventions.ps1` |
| RTL、UIA、缩放、本地化、高对比度 | 通过 | 消费者运行时门禁 |
| 格式检查 | 通过 | `git diff --check` |

## 2. 原始目标与执行边界

用户要求：审核 AUDIT 分支精修过的控件，确保每个控件、每个属性可被调用、读取、视觉验证，并在业务语义存在时可被后端监听与消费；实现与样本架构对齐 WinUI 3 官方约定和 WinUI 3 Gallery。

“全属性后端消费”采用必要的边界：

1. Ether 自有公开 DP 必须可被观察并产出 JSON 安全的后端 envelope。
2. 标准 WinUI 业务状态（点击、文本、选择、勾选、开关、范围值）由标准交互适配器消费。
3. `FrameworkElement`/`UIElement` 的通用视觉与布局属性必须逐项可设置、读取、附着、布局和渲染，但不默认上传到后端；它们没有稳定业务含义，上传会泄露 UI 实现细节。
4. 需要业务记录的视觉/布局配置可通过 `ObserveProperty` 显式观察，而不是把整个 UI 对象图序列化到后端。

完整规则和后端可靠性要求见：

- [审核说明](../architecture/2026-08-29-winui3-backend-consumable-component-audit.md)

## 3. 调研与对标内容

### WinUI 3 官方约定

审核和自动化门禁按以下 Microsoft 约定实施：

- 自定义可绑定属性使用 `public static readonly DependencyProperty <Name>Property` 和同名 CLR `GetValue`/`SetValue` 包装器。
- 有默认模板的控件在构造函数设置 `DefaultStyleKey`。
- 读取模板部件的控件覆盖 `OnApplyTemplate()` 并优先调用 `base.OnApplyTemplate()`。
- XAML 模板部件与状态通过 `TemplatePart`、`TemplateVisualState` 声明。
- 主题资源、高对比度资源、UIA、RTL 和非即时动画 easing 都进入自动化检查。

参考：

- [Microsoft Learn：自定义依赖属性](https://learn.microsoft.com/en-us/windows/apps/develop/platform/xaml/custom-dependency-properties)
- [Microsoft Learn：WinUI 3 模板控件](https://learn.microsoft.com/en-us/windows/apps/winui/winui3/xaml-templated-controls-csharp-winui-3)
- [Microsoft Learn：控件模板](https://learn.microsoft.com/en-us/windows/apps/develop/platform/xaml/xaml-control-templates)
- [Microsoft WinUI Gallery 源码](https://github.com/microsoft/WinUI-Gallery)

### WinUI 3 Gallery 对标方法

WinUI Gallery 被作为平台行为、可访问性、样本架构的基准，而不是 Ether 视觉 token 的替代品。每个控件按三层检查：

| 层级 | 本轮执行 |
| --- | --- |
| 平台行为 | DP、模板、VisualState、UIA、RTL、键盘/状态约定门禁 |
| Gallery 样本架构 | 独立样本、明确状态、AutomationProperties、XAML/code-behind 可读性 |
| Ether 视觉规范 | Light/Dark/OS High Contrast、LTR/RTL、Disabled/Pressed/Hover、截图与状态断言 |

## 4. 执行过程

1. **库存与分类**：通过反射枚举 **13 个**受审类型（`RuntimeVerification.PublicPropertyInventory.cs` 的 `PublicPropertyInventoryTypes`：`EtherButton`、`EtherCheckbox`、`EtherDropdown`、`EtherInput`、`EtherIntelligenceButton`、`EtherProgressBar`、`EtherRadioButton`、`EtherSegmentedControl`、`EtherSegmentedTrack`、`EtherSegmentPanel`、`EtherSlider`、`EtherSteeringBar`、`EtherMasthead`）的 1,501 个公开可写属性，并分类为 482 个视觉、73 个语义、946 个平台属性。
2. **代码可调用性**：对每一项属性，先在真实控件实例上执行 CLR getter（读取一次，计入 `getterReadCount`）；随后 `TryInvokeOnUnattachedInstance`（`RuntimeVerification.FullPropertyCode.cs`）在**同类型新建的游离实例**上再做一次 `GetValue` 再 `SetValue`——写回的是**读到的同一个当前值**（大多数引用类型是 `SetValue(null)`），并不是一个不同于当前值的新值。这一步验证的是“属性在代码层面可被调用（getter/setter 都不抛异常）”，不是“属性能接受任意新值并生效”；后者由第 3-5 步的附着视觉/DP 断言覆盖。该门禁不依赖设计时 XAML 或项目引用。
3. **附着视觉验证**：每个视觉属性使用独立控件标本，加入真实 `Canvas`/`Border` 视觉树，变更后重新布局并使用 `RenderTargetBitmap` 取指纹。
4. **证据强化**：修正了原先会掩盖对齐/最小最大尺寸的拉伸试样；使用 160×80 的约束基线，使布局变化可见。修正 `IsDropDownOpen` 先前的 `false -> false` 样例，改为真实打开状态。
5. **属性级证据**：每条视觉属性必须有一条唯一证据，方法如下：
   - `pixel-difference`：渲染位图发生变化；
   - `layout-difference`：实际/期望尺寸或相对于表面的原点变化；
   - `visibility-transition`：加载控件进入 `Collapsed` 后恢复 `Visible` 并渲染；
   - `ether-component-dp-contract`：Ether 组件自有 DP 的运行时 `GetValue` 契约；
   - `platform-dp-contract`：WinUI 平台自身 DP（例如 `Control.BackgroundProperty`、`FrameworkElement.WidthProperty`）的运行时 `DependencyObject.GetValue` 契约；
   - `platform-clr-visual-contract`：**理论上**保留给“WinUI 公开一个可写视觉 CLR 属性、但确实没有任何形式（field 或 property）的静态 `DependencyProperty` 标识符”的情形。修复 `ResolveDependencyProperty` 后（见下文“4.5 修订：DP 解析缺陷”），本组件集里唯一落入该桶的是 `EtherMasthead.Scale`——已用反射针对 `Microsoft.UI.Xaml.UIElement` 验证：该类型在这份 WinUI 3 元数据里**没有任何静态字段**，`Scale`/`Rotation`/`RotationAxis` 等 Composition 直通属性确实不经过 `DependencyProperty`，因此这是真实、可验证的分类，不是误判。
6. **后端消费**：37 个 Ether 自有 DP 写入、读取、回调及 JSON envelope；12 条标准交互适配器发出业务 envelope。
7. **视觉回归工件**：除了全页 Light/Dark/High Contrast 图，新增 13 个控件的独立 Light/Dark 截图矩阵；验收脚本强制检查控件 ID、文件存在、尺寸为正、图像不是截断空文件，**以及（本次修订新增）每个控件的 Light/Dark 截图 SHA256 必须不同**，防止某个控件因主题未真正传播到具体实例而产出两张字节相同的图（EtherScrollBar 的真实回归，见下文“4.6 修订：ScrollBar 主题传播缺陷”）。
8. **官方约定与交叉平台门禁**：运行 `Verify-WinUiConventions.ps1`；消费者运行时验证主题、OS High Contrast、RTL、UIA、2.25 文本缩放、本地化、SVG 与资源恢复。

### 4.5 修订：DP 解析缺陷（已修复）

`RuntimeVerification.AttachedVisualProperties.cs` 的 `ResolveDependencyProperty`（原实现只调用 `Type.GetField($"{propertyName}Property", ...)`）只能解析以 `public static readonly DependencyProperty XProperty` **字段**声明的 DP，这是 Ether 自有控件的写法。但 WinUI 3 走 C#/WinRT 投影，平台自身的 DP 标识符（`Control.BackgroundProperty`、`FrameworkElement.WidthProperty`、`Control.FontSizeProperty` 等）在这份元数据里是 **`static DependencyProperty` 属性**（`get_XProperty` 访问器），不是字段——已用反射对 `Microsoft.WinUI.dll` 验证。原实现因此对几乎所有平台 DP 解析失败，把它们错误地归入 `platform-clr-visual-contract` 弱证据档，并配了一段声称“WinUI 并非所有视觉属性都公开 DP 字段”的误导性解释。

修复：`ResolveDependencyProperty` 现在按 `Type.GetField` 后 `Type.GetProperty` 的顺序、沿 `BaseType` 链逐级查找两种声明形式。修复后 `platform-clr-visual-contract` 从 172 条降到 0-1 条（唯一合法残留是 `EtherMasthead.Scale`，见上），`platform-dp-contract` 从 0 条升到 165-184 条区间（随第 4.6 节之后的架构调整波动，见 §6）。DP 层回读断言（`DependencyObject.GetValue` 与 CLR 值一致）修复后在全部受影响属性上均通过，**没有暴露新的真实失败**。

### 4.6 修订：ScrollBar 主题传播缺陷（已修复）

`scrollBar` 控件截图矩阵里 `scrollBar-light.png` 与 `scrollBar-dark.png` 字节完全相同。定位到两处问题：

1. `src/Ether.DesignSystem.Controls/Controls/Inputs/EtherScrollBar.xaml` 第 41-44 行的 Dark `ThemeDictionary` 把 `EtherScrollBarThumbBrush`/`EtherScrollBarThumbHoverBrush` 设成了和 Light 相同的 `Gray400`/`Gray500`，而不是 `src/Ether.DesignSystem.Foundation/Resources/Tokens/EtherColors.xaml` 里已经记录的、Figma 命名对齐的 `scrollbar/background/default`/`scrollbar/background/hover` 令牌的 Dark 值 `Gray600`/`Gray700`。已改为 `Gray600`/`Gray700`。
2. 真正让两张截图**字节完全相同**（而不只是颜色不对）的根因是 `RuntimeVerification.ScrollBar.cs` 里的 `GetScrollBarTemplateBrushColorsAsync`：它除了设置 `themeRoot.RequestedTheme`（其余全部 12 个控件的等价方法都只做这一步），还额外直接设置了 `scrollBar.RequestedTheme = theme`。`RequestedTheme` 一旦被显式赋值就会“粘住”，不再随祖先主题变化而重新继承。由于该方法先探测 Light 再探测 Dark，探测结束后 `ScrollBarProof` 这个实际用于截图的实时控件实例的 `RequestedTheme` 就永久停在 `Dark`——之后 `CaptureThemeScreenshotsAsync` 翻转 `themeRoot.RequestedTheme` 为 Light 时，这个控件已经不再跟随，因此两张截图都渲染成同一个（Dark）主题。已删除这行多余赋值，改为和其余控件一致，只依赖主题继承 + 已有的 `ActualTheme` 轮询等待。
3. 同时发现并修正了两处编码相反的断言，它们此前把这个 bug 当作“预期契约”而不是拿来捕获它：`RuntimeVerification.ScrollBar.cs` 里 `if (!lightTemplateBrushColors.SequenceEqual(...))` 应为 `if (lightTemplateBrushColors.SequenceEqual(...))`（其余全部控件都是后者，要求 Light≠Dark）；`scripts/Verify-ConsumerFixtures.ps1` 里 scrollBar 那一行用的是 `-cne`，应为 `-ceq`（其余全部 12 个控件都是 `-ceq`）。

修复后用反射直接解出的实际渲染像素证实：`scrollBar-light.png` 为 RGB(139,150,161) = `Gray400`，`scrollBar-dark.png` 为 RGB(65,69,83) = `Gray600`，均与预期令牌精确匹配。`scripts/Verify-ConsumerFixtures.ps1` 也新增了对全部 13 个控件（不只是整页截图）的 Light/Dark SHA256 不相等断言，防止同类回归再次被放过。

### 4.7 修订：视觉门禁灵敏度回归（已修复，2026-08-30）

§4.6 定稿之后，同一份 `CaptureVisualFingerprintAsync` 又做了一次改动：为了压制 §6 “确定性证明”里提到的合成器抗锯齿抖动，在哈希前对每个 BGRA8 通道字节做 `& 0xF0`（掩掉低 4 位，256 级降到 16 级）。这个改动本身有三个问题：注释写“掩掉低 2 位”但 `0xF0` 实际掩掉的是低 4 位；截断不是容差（`0x0F` 和 `0x10` 只差 1 级，截断后变成 `0x00` 和 `0x10`，仍然不同，起不到压制 ±1 抖动的作用）；更严重的是它把哈希从 265 条 `pixel-difference` 压到 232 条，其中 20 个属性（`EtherCheckbox`/`EtherRadioButton` 的 `Content`/`ContentTemplate`/`FontFamily`/`FontStyle`/`FontWeight`、`EtherMasthead.IsEnabled`/`Opacity`/`Scale`、`EtherSlider.Labels`/`ShowLabels`、`EtherSteeringBar.Minimum`/`Title`/`ValueContent`/`FontStyle`）从 `pixel-difference` 跌到弱证据档——这些属性覆盖文本内容、字体、不透明度、缩放、禁用态，理应产生可检测的像素变化。

**根因诊断**：给 `AreVisuallyEquivalent`（见下）临时加装诊断，同时输出“零容差下实际有多少字节不同”和“单通道最大差值”，跑一次完整审核后发现两个清晰分离的总体：

- 217 个当前归为弱证据档的属性里，188 个的 before/after 位图在收敛后**逐字节完全相同**（差值 0）——`CaptureSettledVisualFingerprintAsync` 的收敛循环本身已经把合成器噪声压到零，`0xF0` 掩码要解决的“抖动”在这一步之后已经不存在了。
- 前述 20 个属性外加另外几个（`EtherMasthead.Scale`、`EtherSteeringBar.Title`）的最大单通道差值只有 2-5 级，但受影响的字节数有 435-9,708 个——这是文本/字形/透明度在小范围内的次像素抗锯齿/伽马混合位移，是真实信号，只是幅度小；`0xF0`（等效容差 15）会把它们全部吞掉。

**修复**：把“掩码后取哈希”换成“保留原始 BGRA8 缓冲、逐像素做容差 + 显著性比较”（`AreVisuallyEquivalent(VisualSnapshot, VisualSnapshot, out int)`，`tests/Ether.DesignSystem.ConsumerFixtures/RuntimeVerification.AttachedVisualProperties.cs`）：单通道字节差值 `> 1` 级才算“变了”（`ChannelToleranceLevels = 1`，取到能捕获最小真实信号——`EtherMasthead.Opacity` 的差值 2——的下限，因为诊断显示收敛后的噪声基线是精确的 0，不需要更大容差）；变化像素数超过 `Math.Max(12, 像素总数 × 0.0002)`（`MinimumSignificantPixelCount`/`SignificantPixelFraction`）才判定整张位图“变了”，这个下限远高于噪声基线（0）、远低于最小真实信号（约 100+ 像素）。收敛循环（`CaptureSettledVisualFingerprintAsync`）保留不变，只是比较函数从哈希相等换成这个容差比较。

**连带修复：收敛窗口不够宽**。换用容差比较后，`EtherInput.FontFamily`/`EtherInput.HeaderTemplate` 在连续两次独立运行之间会在 `pixel-difference`（132 像素变化）和弱证据档（0 像素变化）之间翻转——不是容差/显著性阈值的边界抖动，而是“变更到底有没有真的渲染出来”这个二元结果本身不稳定。原因是 TextBox 的文本重排/字体替换是异步的，有时会先安静（连续几帧位图不变）再姗姗来迟地把变更渲染出来；旧的 `RequiredStableFingerprintReadings = 3`（连续 3 帧不变即判定收敛）有时会把这个“安静期”误判为已收敛的最终状态，锁定在变更还没落地的中间帧上。`0xF0` 掩码时代这个问题是隐形的——它把这两个属性的真实信号也一起吞掉了，两次运行巧合地都落在“看似不变”的桶里，形成假的确定性。修复方式是把 `RequiredStableFingerprintReadings` 从 3 提到 6；已用两次独立完整运行验证零翻转（见 §6）。

**验收结果**：20 个目标属性全部回到 `pixel-difference`（`EtherMasthead.Scale` 也是——用反射验证过它确实没有 `Microsoft.UI.Xaml.UIElement.ScaleProperty` 静态标识符，`platform-clr-visual-contract` 分类本身没有错，但它的像素变化应该被检出，修复后确实被检出，不再需要退回弱证据档）；`pixelChangedPropertyCount` 回到 265（与 §4.5 修订前、`0xF0` 掩码引入前的基线一致）；连续两次独立完整运行逐属性证据 Method 完全一致（0 处差异，见 §6）。

## 5. 主要代码与测试改动

### 组件与模板

- `EtherButton.xaml`：默认、Secondary、Tertiary 样式和模板改为消费 `Background`、`BorderBrush`、`Foreground`、`CornerRadius`、字体、左右图标等公共视觉属性。
- `EtherCheckbox.xaml`、`EtherRadioButton.xaml`：标签字体/前景与默认样式通过模板绑定和主题资源消费。
- `EtherInput.xaml`：输入框背景、边框、placeholder 前景和默认样式接入模板绑定。
- `EtherDropdown.xaml`：触发器背景、圆角、前景、字体、箭头和默认样式接入模板绑定；运行时验证真实下拉开闭。
- `EtherSegmentPanel.cs`：按 `FlowDirection` 排列子项，修复 RTL 中逻辑第一个项顺序；`EtherSegmentedControl.cs` 和样本使用该面板。
- `EtherSlider.xaml.cs`、`EtherSteeringBar.xaml.cs`、`HandContentControl.cs`：补充控件契约、状态与支持行为；`PublicAPI.Unshipped.txt` 同步公开 API。

### 验收基础设施

- `RuntimeVerification.AttachedVisualProperties.cs`：新增全视觉属性附着验证、位图指纹、布局快照、可见性往返、DP/CLR 契约和逐项证据输出。
- `RuntimeVerification.PublicPropertyClassification.cs`：将全属性分类固定为 `482 / 73 / 946`，总数 1,501。
- `RuntimeVerification.cs`、`RuntimeVerification.Infrastructure.cs`：输出完整 marker，捕获全页及控件级截图矩阵。
- `Verify-ConsumerFixtures.ps1`：强制验证每条视觉属性键与证据键一一对应；仅允许已定义的证据方法；保留每次成功运行的 JSON 与截图证据，不让下一轮清理临时目录时删除。
- `Verify-WinUiConventions.ps1`：强化官方 DP、模板、状态、高对比度和动画约定检查。
- Packaged/Unpackaged 消费者项目与样本：确保运行时严格从 NuGet 包恢复，不存在项目引用绕过。

### 相关构建、CI 与样本改动

修改还涉及 `Ether.DesignSystem.slnx`、CI workflow、预览包/ARM64/MSIX/组件契约脚本，以及 Segmented/Slider Gallery 样本，以使新控件契约进入解决方案和发布验证路径。

**关于改动量的准确说明**：`git diff --name-only` 只统计**已跟踪**文件的改动，本轮统计的 31 个已跟踪文件、约 640 行新增、100 行删除（不含本交接文件）不包含以下仍是 **untracked（未跟踪）** 的新增内容——它们不出现在 `git diff` 统计里，但同样是本轮审核的核心产出，需要用 `git status` 才能看到：

- 整个新项目 `src/Ether.DesignSystem.Interactions/`；
- 整个新测试项目 `tests/Ether.DesignSystem.Interactions.ContractTests/`；
- 5 个核心审核源文件：`RuntimeVerification.AttachedVisualProperties.cs`、`RuntimeVerification.FullPropertyCode.cs`、`RuntimeVerification.PropertyConsumption.cs`、`RuntimeVerification.PublicPropertyInventory.cs`、`RuntimeVerification.PublicPropertyClassification.cs`。

也就是说，实际新增代码量显著大于“31 个已跟踪文件 / 640 行”这个数字所暗示的规模；合入前应同时用 `git status`（列出 untracked 内容）和 `git diff --stat`（已跟踪内容的行数）两者合起来评估改动量。

## 6. 最终运行结果

**本节已随 §4.7、§11 的 2026-08-30 修订更新。** 最终成功运行的 marker（两次连续独立完整运行中的最后一次；前一次见下方“确定性证明”）：

```text
artifacts/audit-runs/consumer-runtime-evidence-20260830-002407286/runtime-result.json
```

关键字段（两次运行完全一致）：

| 字段 | 最终值 |
| --- | ---: |
| `outcome` | `success` |
| `publicPropertyCode.getterReadCount` | 1,501 |
| `publicPropertyCode.setterInvocationCount` | 1,501 |
| `attachedVisualProperties.visualPropertyCount` | 482 |
| `attachedVisualProperties.evidence.Count` | 482 |
| `attachedVisualProperties.pixelChangedPropertyCount` | 265 |
| `propertyConsumption.propertyChangedCallbackCount` | 37 |
| `propertyConsumption.backendPropertyEventCount` | 37 |
| `propertyConsumption.standardInteractionEventCount` | 12 |
| `screenshots.controls.Count` | 13 |
| 控件 Light/Dark PNG 数 | 26 |

本次视觉证据分布：265 条像素差异、12 条布局差异、13 条可见性状态往返、22 条 Ether 组件 DP 契约、170 条 WinUI 平台 DP 契约、0 条 WinUI 平台 CLR 视觉契约。总数为 482，无缺失属性键。`platform-clr-visual-contract` 从 §4.5 修订后的“1 条（`EtherMasthead.Scale`）”降到 0：§4.7 的容差比较修复后，`EtherMasthead.Scale` 的真实像素变化被正确检出，改落进 `pixel-difference`，`platform-clr-visual-contract` 分类桶本身仍然存在（供未来真的出现“无 DP 视觉 CLR 属性”时使用），只是当前组件集里没有属性再落入这一桶。布局差异从 25 降到 12，是同一次修复的连带效果：过去因为像素层面被掩码漏检，一部分“既变了布局又变了像素”的属性退而求其次落进 `layout-difference`，容差比较修复后它们正确地被 `pixel-difference` 先行判定（代码里 `pixel-difference` 判定优先于 `layout-difference`，见 `VerifyAttachedVisualPropertiesAsync`）。

### 确定性证明

早期版本的附着视觉属性审核（`VerifyAttachedVisualPropertiesAsync`）在 mutation 之后只用 `control.UpdateLayout(); await Task.Yield();` 就截取“after”指纹，与 VisualState 的 CubicEase 转场、TextBox 字体重排等异步渲染路径存在竞态，导致同一份代码在不同运行之间产出不同的证据 Method（例如 `EtherIntelligenceButton.IsEnabled`、`EtherSteeringBar.IsEnabled` 在两轮之间从弱证据翻到 `pixel-difference`）。

2026-08-29 定稿时的修复分两层：

1. **构造式收敛，而非猜测等待时长**：`CaptureSettledVisualFingerprintAsync` 连续采样 `RenderTargetBitmap` 指纹（每次间隔一个真实合成帧），直到连续 N 次采样完全相同才认定收敛（当时 N=3，2026-08-30 改为 6，见下）；`before`、`after` 两侧都走同一套收敛逻辑（起点本身也不能是抖的）。若 60 次采样内始终不收敛，直接抛异常终止运行，而不是静默降级成弱证据——这样"抖动"不可能被悄悄放行。
2. **量化指纹，过滤合成器亚像素噪声**：当时用 `& 0xF0` 通道掩码压制抖动——**这一步后来被 §4.7 证明是错误的手段（虽然出发点，即“需要过滤噪声”，是对的）**，已被替换为容差 + 显著性的逐像素比较，见 §4.7。

**2026-08-30 追加修复（随 §4.7 一起验证）**：换成容差比较后，暴露出 `RequiredStableFingerprintReadings = 3`（连续 3 帧不变即收敛）不足以跨过 `EtherInput.FontFamily`/`HeaderTemplate` 的 TextBox 异步重排“安静期”，导致这两个属性在连续运行之间于 `pixel-difference`（132 像素变化）与弱证据档（0 像素变化）之间翻转。已把 `RequiredStableFingerprintReadings` 提到 6，用两次独立完整运行验证零翻转（见下）。

修复后，对 2026-08-30 修订后的最终代码状态连续两次独立完整运行（`...20260830-001800579`、`...20260830-002407286`）逐属性证据 Method **完全一致**（0 处差异，`pixelChangedPropertyCount` 均为 265）。2026-08-29 当天定稿时，对彼时代码状态也曾有五次独立运行彼此完全一致（`...230136589`、`...230519527`、`...224425998`、`...224946229`、`...225342824`）——但那份代码状态已被 §4.7 描述的 `0xF0` 掩码回归取代，现在仅作历史存档，不代表当前分支状态。全部运行均为 `Verify-ConsumerFixtures.ps1 -SkipSolutionBuild` 独立进程执行，非同一进程重放。

## 7. 最终证据与复验入口

| 工件 | 路径 |
| --- | --- |
| 完整运行 marker | `artifacts/audit-runs/consumer-runtime-evidence-20260830-002407286/runtime-result.json` |
| 整页截图 | `artifacts/audit-runs/consumer-runtime-evidence-20260830-002407286/screenshots/consumer-light.png`、`consumer-dark.png`、`consumer-highcontrast.png` |
| 控件截图矩阵 | `artifacts/audit-runs/consumer-runtime-evidence-20260830-002407286/screenshots/controls/` |
| 确定性证明：最终状态的另一次运行 | `artifacts/audit-runs/consumer-runtime-evidence-20260830-001800579/` |
| 历史存档（2026-08-29 定稿时，代码状态已被 §4.7 取代） | `artifacts/audit-runs/consumer-runtime-evidence-20260829-230519527/`、`...230136589/`、`...224425998/`、`...224946229/`、`...225342824/` |
| 审核说明 | `docs/architecture/2026-08-29-winui3-backend-consumable-component-audit.md` |
| 消费者验收脚本 | `scripts/Verify-ConsumerFixtures.ps1` |
| WinUI 约定门禁 | `scripts/Verify-WinUiConventions.ps1` |

复验命令（PowerShell）：

```powershell
& 'C:\Users\yiqizhong\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\powershell\pwsh.exe' `
  -NoProfile -ExecutionPolicy Bypass `
  -File 'C:\Ether lib\scripts\Verify-ConsumerFixtures.ps1' -SkipSolutionBuild

& 'C:\Users\yiqizhong\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\powershell\pwsh.exe' `
  -NoProfile -ExecutionPolicy Bypass `
  -File 'C:\Ether lib\scripts\Verify-WinUiConventions.ps1'
```

成功的消费者验收会自行重新打包、恢复 NuGet 消费者、启动未打包宿主，并在 `artifacts/audit-runs/consumer-runtime-evidence-<timestamp>/` 保留新的证据包。

## 8. 当前进度与后续维护规则

**当前进度：100% — 本轮定义的全属性验收已完成并通过。**

后续若新增或更改组件/属性，必须满足：

1. 更新公开 API 与属性分类；总数或分类数变化必须伴随验收脚本和运行时样例更新。
2. 自有 Ether DP 必须新增 `SetValue`、getter、`GetValue`、回调和 JSON envelope 证据。
3. 新视觉属性必须拥有一条逐项视觉证据，不能仅依赖“无异常”。
4. 新业务交互必须使用标准适配器或显式 `ObserveProperty`，不得直接由控件访问 HTTP/认证/业务层。
5. 更新控件级 Light/Dark 视觉工件；涉及方向或无障碍时更新 RTL、High Contrast、UIA 断言。
6. 在合入前运行消费者验收、WinUI 约定门禁和 `git diff --check`。

## 9. 已知边界（不是未解决问题）

- 通用平台视觉/布局属性不默认进入后端事件流；这是防止泄露 UI 实现细节的有意架构边界。
- 截图不能单独证明完整无障碍合规，因此保留 UIA、RTL、文本缩放和 OS High Contrast 的运行时断言。
- 证据目录是构建产物，未必进入 Git；需要长期归档时应由 CI 上传为构建 artifact。
- 工作区存在本轮相关的未提交改动；交接前不要使用 `git reset --hard` 或覆盖清理，以免丢失审核与组件修正。
- **CI 目前不会执行本文档描述的运行时门禁，这不是遗漏，而是仓库既有的有意设计**：`.github/workflows/build.yml` 的 `package-consumers` job 在第 102 行调用的是 `./scripts/Verify-ConsumerFixtures.ps1 -SkipSolutionBuild -SkipRuntimeSmoke`，`-SkipRuntimeSmoke` 会跳过整段运行时验证——即本文档 §1/§6 表格里的 1,501/482/37/12 全属性核对、控件截图矩阵、RTL/UIA/缩放/本地化/高对比度全部不会在 PR 上跑。workflow 里的注释解释了原因：hosted Windows runner 启动真实 WinUI 窗口不可靠，这类运行时门禁被有意放在本机/自托管环境上，由 `scripts/Verify-RuntimeGates.ps1` 负责。也就是说，本文档记录的验收结果是**本机/自托管门禁的结果，不是 PR 阻断门禁**；PR 合入前若要确保这些属性/视觉/主题回归被捕获，必须有人手动或通过自托管 runner 跑一次 `Verify-ConsumerFixtures.ps1`（不带 `-SkipRuntimeSmoke`）。
- `RuntimeVerification.AttachedVisualProperties.cs` 的 `CreateVisualFixture` 会把 `EtherSegmentedTrack` 替换成 `EtherSegmentedControl` 来跑视觉验证（方法内有注释说明）：`EtherSegmentedTrack` 是一个没有独立模板契约的源码兼容子类，WinUI 的 keyed `ControlTemplate` 是挂在 `EtherSegmentedControl`（基类）上的，所以继承属性要对着能拿到真实模板的类型渲染，才能产出有意义的视觉证据；与此同时，独立的“代码可调用性”门禁（第 4.2 步）仍然会真正实例化 `EtherSegmentedTrack` 本身。换句话说，`EtherSegmentedTrack` 的公开属性在代码层面是对自己的类型验证的，但在视觉层面是对 `EtherSegmentedControl` 的渲染结果验证的。

## 10. 修订记录（2026-08-29，同日修订）

首版交接文档发出后，复核发现 4 处实质性缺陷（均已修复并回归验证）与若干文档表述不准之处，已在本次修订中一并更正：

1. **DP 解析缺陷**（§4.5）：`ResolveDependencyProperty` 原来只查 `GetField`，漏掉了 WinUI 3 WinRT 投影里以 `static property` 形式暴露的平台 DP，导致 172 条平台属性被误判为弱证据档，且配文错误。已修复；`platform-clr-visual-contract` 从 172 降到 0-1（唯一残留 `EtherMasthead.Scale` 已用反射证实是真实分类，不是 bug）。
2. **视觉证据跨运行不确定性**（§6“确定性证明”）：原先固定时长的动画/合成器等待被替换为“采样直到指纹收敛 + 收敛失败即抛异常 + 量化指纹过滤合成器噪声”的构造式方案，连续三次独立完整运行逐属性证据 Method 完全一致。
3. **ScrollBar 主题传播缺陷**（§4.6）：`EtherScrollBar.xaml` 的 Dark 主题色值错误、验证代码里一处显式 `RequestedTheme` 赋值把控件实例“粘”在了错误的主题上、以及两处编码相反（把 bug 当契约）的断言，四处一并修复；`scripts/Verify-ConsumerFixtures.ps1` 新增全部 13 个控件的 Light/Dark SHA256 不相等断言防止同类回归。
4. **文档表述更正**：§4.1 受审类型数从“11 个”更正为实际的 13 个；§1/§4.2 关于 setter 验证方式的措辞改为如实描述“游离实例上取当前值再原样写回”，而不是暗示对新值的有效性验证；§5 补充说明实际改动包含大量 untracked 的新项目/新文件，不止已跟踪的 31 个文件；§9 补充 CI 实际上通过 `-SkipRuntimeSmoke` 跳过了本文档描述的全部运行时门禁（不是 PR 阻断门禁），以及 `EtherSegmentedTrack`→`EtherSegmentedControl` 的视觉标本替换说明。
5. **视觉门禁灵敏度回归**（§4.7，2026-08-30）：§10 第 2 条描述的“量化指纹过滤合成器噪声”修复，实现上用的是 `& 0xF0` 通道掩码，事后证明这个具体手段是错的——不是容差，且把 20 个真实存在视觉变化的属性（文本/字体/透明度/缩放/禁用态）连带压成了弱证据档，`pixelChangedPropertyCount` 从 265 掉到 232。已换成保留原始像素、逐像素容差 + 显著性比较的方案；同时发现并修复了 `RequiredStableFingerprintReadings`（收敛所需连续稳定读数）在换用更灵敏的比较后不足以覆盖 `EtherInput.FontFamily`/`HeaderTemplate` 的 TextBox 异步重排安静期，导致这两个属性在连续运行间翻转（3 提到 6）。修复后 20 个属性回到 `pixel-difference`，`pixelChangedPropertyCount` 回到 265，连续两次独立完整运行逐属性证据 Method 完全一致。
6. **新增 §11：High Contrast 前景/背景配对缺陷**（2026-08-30）：与本文档主线（属性可消费审核）并行的一项独立无障碍缺陷修复——`EtherSegmentedControl`/`EtherDropdown`/`EtherButton`/`EtherMasthead`/`EtherSwitch` 在 High Contrast 主题下把 Hover/Pressed/Selected 背景重绘成不透明的 `SystemColorHighlightColor`，但前景仍停在为 Window/ButtonFace 底配的 `SystemColorWindowTextColor`/`SystemColorButtonTextColor`，导致高对比度下文字/图标不可读。详见 §11。
7. **§11 复核修正**（2026-08-30，同日）：`EtherMasthead` 的图标改色曾经改成 code-behind 订阅 Pointer 事件、手动赋值 `Fill`/`Stroke`，这版实现有真实回归（hover 后切主题颜色不刷新）。尝试过改成纯 XAML 的 `Setter Target="Foreground"`（无 `ElementName`）+ 图标 `{Binding Foreground, ElementName=...}` 方案，但实测在这个 WinUI 3 版本上 `VisualStateManager.GoToState` 进入 `PointerOver` 时会抛出运行时 `COMException (0x800F1000)`，已撤销；最终修复保留 Pointer 事件订阅，新增 `ActualThemeChanged` 处理器在主题变化时按每个按钮当前的 `CommonStates` 状态重新推送已用新主题解析好的色板颜色，并补充回归测试。另外发现 `EtherDropdown.xaml` 混入一处超出 §11 任务范围的重构（触发器 `Background`/`CornerRadius`/`Foreground`/`FontWeight` 改用 `TemplateBinding`），复核确认该重构本身不产生真实回归，实测差异是既有的截图管线噪声，并更正了 §11"回归验证"里过宽的"零回归"表述。详见 §12。

## 11. High Contrast 前景/背景配对缺陷（已修复，2026-08-30）

### 问题

Windows 高对比度主题下，前景色必须与其所在的背景色按固定规则配对，不能混搭：`SystemColorWindowColor` 底配 `SystemColorWindowTextColor`；`SystemColorButtonFaceColor` 底配 `SystemColorButtonTextColor`；**`SystemColorHighlightColor` 底配 `SystemColorHighlightTextColor`**；任意底上的禁用态配 `SystemColorGrayTextColor`。

本仓库的 Light/Dark 主题里，Hover/Pressed/Selected 状态大多用半透明叠加色（例如 `AlphaBrand6`、`Gray25`）实现，叠加很淡，因此模板里前景写死一个值是合理的。但 HighContrast 字典把这些同一批 token 映射成了**不透明**的 `SystemColorHighlightColor`，前提被打破，而前景没有跟着换成配对的 `SystemColorHighlightTextColor`，导致选中态背景发亮、文字却还是原来那个（对 Window/ButtonFace 底调好的）颜色，压在 Highlight 亮底上几乎不可读。`EtherCheckbox`/`EtherRadioButton` 的 Checked 态原本就做对了（`CheckedFill`=Highlight 配 `Glyph`/`InnerDot`=HighlightText），本次缺陷是 Hover/Pressed 这一档漏配。

### 修复

原则是**补前景、不降背景**，与 WinUI 自身 `ComboBoxItem`/`Button` 的做法一致；新增的前景键在 Light/Dark 里取原来那个前景的同值（零视觉变化），只有 HighContrast 字典里才换成 `SystemColorHighlightTextColor`。逐文件改动：

| 文件 | 新增/改动 |
| --- | --- |
| `src/Ether.DesignSystem.Controls/Controls/Inputs/EtherSegmentedControl.xaml` | 新增 `EtherSegmentedControlSegmentForegroundHoverBrush`/`PressedBrush`；`EtherSegmentTemplate` 的 `PointerOver`/`Pressed` 状态新增 `Cp.Foreground` Setter（此前只有 `Checked`/`CheckedPointerOver`/`CheckedPressed` 三态有） |
| `src/Ether.DesignSystem.Controls/Controls/Inputs/EtherDropdown.xaml` | 新增 `EtherDropdownForegroundHoverBrush`/`PressedBrush`（触发器文字+箭头）与 `EtherDropdownItemForegroundActiveBrush`（菜单项，覆盖 `PointerOver`/`Pressed`/`Selected`/`SelectedUnfocused`/`SelectedDisabled`/`SelectedPointerOver`/`SelectedPressed` 七态）；触发器模板给 `TriggerText`/`Arrow` 新增 `Foreground`/`Fill` Setter，菜单项 `ContentPresenter` 补 `x:Name="ItemContent"` 后新增对应 Setter |
| `src/Ether.DesignSystem.Controls/Controls/Inputs/EtherButton.xaml` | 新增 `EtherButtonSecondaryForegroundHoverBrush`/`PressedBrush`；Secondary 模板的 `PointerOver`/`Pressed` 状态新增 `LeftIcon`/`Cp`/`RightIcon` 三个 `Foreground` Setter（Primary 背景在三态下都是同一个 Highlight，本来就不需要改；Tertiary 早已正确处理） |
| `src/Ether.DesignSystem.Controls/Controls/Navigation/EtherMasthead.xaml`/`.xaml.cs` | 新增 `EtherMastheadIconForegroundHoverBrush`/`PressedBrush`。Minimize/Maximize/Restore/Close 四个图标是 `Shape`（`Rectangle`/`Path`）内容，`Fill`/`Stroke` 不参与 WinUI 的 Foreground 属性值继承，且 VisualState.Setter 无法跨模板（`EtherMastheadCaptionButtonStyle` 的状态摸不到宿主图标）触达。**首版实现**（本节最初写下时）在 `EtherMasthead.xaml.cs` 里订阅三个按钮的 `PointerEntered`/`Exited`/`Pressed`/`Released`/`Canceled`/`CaptureLost`，从三个隐藏的 `{ThemeResource}` 色板元素读取已解析好的颜色，直接赋给图标的 `Fill`/`Stroke`——**这版实现有一个真实回归，已在 2026-08-30 复核中发现并改正**：一旦手动赋值过，`Shape.Fill`/`Stroke` 就不再追踪其原始 `{ThemeResource}` XAML 表达式，所以用户 hover 过任意一个标题栏按钮后再切换 Light/Dark/HighContrast 主题，图标颜色会停留在旧主题的颜色，直到下次 hover 才刷新。**修复尝试 1（纯 XAML，已放弃）**：把 Pointer 事件订阅换成 `EtherMastheadCaptionButtonStyle` 的 `PointerOver`/`Pressed` 状态里一个不带 `ElementName` 前缀的 `<Setter Target="Foreground" .../>`（意图是让省略目标名指向"应用了这个模板的控件自身"），配合图标 `{Binding Foreground, ElementName=<CaptionButton>}`。这版**编译通过**（`dotnet build` 0 错误），但**实测在运行时崩溃**：调用 `VisualStateManager.GoToState` 进入 `PointerOver` 时抛出 `System.Runtime.InteropServices.COMException (0x800F1000)`，说明这个 WinUI 3 版本里裸属性名 `Setter Target` 并不能像预期那样解析到模板宿主控件自身的属性——这一版已完全撤销，不在最终代码里。**最终修复（已落地）**：保留 Pointer 事件订阅与三个隐藏色板元素（`IconForegroundDefaultSwatch`/`HoverSwatch`/`PressedSwatch`，其 `Fill` 是活的 `{ThemeResource}`，主题切换时自动重解析），只新增一个 `ActualThemeChanged` 订阅：主题变化时，对三个标题栏按钮各自读取其 `CommonStates` VisualStateGroup 的 `CurrentState.Name`（`ButtonBase` 已经在维护这个状态，不需要额外记录），据此选出对应的色板 `Fill`，重新赋给该按钮的图标 `Fill`/`Stroke`（复用既有的 `SetCaptionIconColor`）。这样无论按钮当前处于 Normal/Hover/Pressed 哪一态，主题切换都会把图标颜色刷新到新主题下该态应有的颜色，不再依赖"下次 hover"。**实现中踩到的第二个坑**：一开始在 `ActualThemeChanged` 处理器里同步读取色板 `Fill` 并重新赋值，实测无效——用一个临时诊断（同一断言失败时，额外手动再调用一次刷新方法、把两次结果都打进异常信息）证实：色板 Rectangle 的 `Fill`（`{ThemeResource}` 表达式）在 `ActualThemeChanged` 事件触发的那一刻**还没有**针对新主题重新解析完，同步读到的仍是旧主题的颜色，"刷新"变成了把同一个旧颜色又赋值了一遍；而在事件处理器之外（比如测试代码里 `await` 过几次 `Task.Delay` 之后）手动调用同一个刷新方法，能正确读到新主题的颜色。修复是把刷新逻辑推迟一个 `DispatcherQueue` tick 再执行（`DispatcherQueue.TryEnqueue(...)`，与本文件里 `AppWindow_Changed` 已经在用的模式一致），让 `ActualThemeChanged` 触发的资源重新解析先落地，再读色板。为了让消费者测试能在不合成真实指针输入的情况下复现这个场景，把内部方法 `RefreshCaptionIconColorForCurrentState`（原为 `private`）改成 `internal`——`Ether.DesignSystem.Controls.csproj` 已经对两个 ConsumerFixtures 程序集声明了 `InternalsVisibleTo`，所以这不新增公开 API 面。新增回归测试见 `tests/Ether.DesignSystem.ConsumerFixtures/RuntimeVerification.Masthead.cs` 的 `VerifyMastheadHoverThemeTrackingAsync`：用 `VisualStateManager.GoToState(minimizeButton, "PointerOver", false)` 把按钮置于 Hover 态，再调用 `masthead.RefreshCaptionIconColorForCurrentState(minimizeButton)`（生产代码里 `ActualThemeChanged` 处理器最终会调用的同一个方法）把图标"预置"成 Light 主题下的 Hover 色，模拟一次真实 hover 本会产生的效果；随后**不再调用上述任何一个**、直接把主题从 Light 切到 Dark，等待 `masthead.ActualTheme` 传播到位并再多等几个 dispatcher tick，断言图标 `Fill` 解析出的颜色必须变化——在修复前的代码（没有 `ActualThemeChanged` 订阅）上，这个断言会失败，因为 `Fill` 会停留在 Light 的 Hover 色；这条回归测试在最终代码上实测通过（`Verify-ConsumerFixtures.ps1 -SkipSolutionBuild` 的运行时冒烟里跑到并通过） |
| `src/Ether.DesignSystem.Controls/Controls/Inputs/EtherSwitch.xaml` | 新增 `EtherSwitchKnobStrokeHoverBrush`/`PressedBrush`；`EtherSwitchTemplate` 的 `PointerOver`/`Pressed` 状态新增 `Knob.BorderBrush` Setter（此处没有文字，但描边需要在 Highlight 填充上保持可辨识） |

`src/Ether.DesignSystem.Foundation/Resources/Tokens/EtherColors.xaml` 未改动：它是纯 token 目录（`background/dropdown/hover`、`text/primary` 等 Figma 语义层 token），本身不把背景和前景配对在一起用，配对缺陷只在**消费方**把两类 token 用在同一视觉元素上时才会发生（本次修的 5 个组件都是各自维护自己的一套本地 `Ether<Component>*Brush`，不消费 `EtherColors.xaml` 的语义层 token）；按 token 命名硬规则不重命名/不新造无依据的颜色值，新增键全部沿用各文件既有的命名风格，颜色值直接复用同文件里已有的 Light/Dark 值或 `SystemColorHighlightTextColor`。

### 新增门禁：`scripts/Verify-HighContrastPairing.ps1`

静态正则扫描 `src/Ether.DesignSystem.Controls/Controls` 下所有含 `<ControlTemplate` 的 XAML 文件（跳过纯 token 目录），两条规则：

- **Rule A（精确）**：同一个 `<VisualState>` 块内，若某个 `Setter` 把 `*.Background` 设成解析为 `SystemColorHighlightColor` 的画刷，则同块内所有 `*.Foreground`/`*.Stroke`/`*.BorderBrush`/`*.Fill` Setter 都不能解析为 `SystemColorWindowTextColor`/`SystemColorButtonTextColor`。
- **Rule B（粗粒度、文件级）**：文件里若存在名字带 `Hover`/`Pressed`/`Selected`/`Checked`/`Active` 且解析为 `SystemColorHighlightColor` 的画刷键，文件里就必须至少存在一个名字带 `Foreground`/`Stroke`/`Glyph`/`Text`/`Icon`/`Fg`/`Dot` 且解析为 `SystemColorHighlightTextColor` 的画刷键。这条是为了兜住 Rule A 看不到的情形——背景是通过静态 XAML 属性或 code-behind（而不是同状态块内的 Setter）变成 Highlight 的，例如 `EtherSegmentedControl` 的 `HoverLayer`/`PressedLayer` 不透明度是代码驱动、`EtherMasthead` 的图标改色是 code-behind 驱动（2026-08-30 复核过一次改成纯 XAML 的方案，但实测在运行时崩溃后已撤销，仍是 code-behind 驱动，只是新增了 `ActualThemeChanged` 处理修掉了主题切换陈旧色的回归——详见 §12 前的正文表格）。

Rule B 有一个已知、显式列出并逐一核实过用途的豁免名单（`$exemptHighlightKeys`）：`EtherScrollBarThumbHoverBrush`（纯拖动滑块，无文字）、`EtherSliderKnobBrush`/`KnobPressedBrush`（纯圆形拖动把手，无文字）、`EtherSteeringBarThumbTopHighlightBrush`/`StopMarkerActiveBrush`（玻璃拇指上的装饰性高光/轨道标记点，无文字）、`EtherIntelligenceButtonBorderHoverBrush`（hover 焦点描边，按钮文字本身待在没有变色的 `ButtonFace` 底上）、`EtherIntelligenceButtonBlueGlowPressedCoreBrush`（装饰性模糊光晕层，不承载文字）。已接入 `.github/workflows/build.yml`（`Verify WinUI 3 control conventions` 步骤之后）。运行 `Verify-HighContrastPairing.ps1` 后确认：全仓库 13 个含 `ControlTemplate` 的组件文件里不再存在“Highlight 亮底 + 非 HighlightText 前景”的配对。

### 回归验证

- `Verify-ConsumerFixtures.ps1 -SkipSolutionBuild` 通过（含 Light/Dark 模板画刷断言、13 控件 Light/Dark 截图 SHA256 不相等断言、OS High Contrast 截图与 Light/Dark 均不同的断言）。
- Light/Dark 视觉零回归：**这句话原本的范围仅限于本节新增的前景/描边键本身**——它们在 Light/Dark 字典里的取值与原有前景/描边键完全相同（同一个 `StaticResource` 引用），单看这几个新增键确实是零视觉变化。但 2026-08-30 复核发现，`EtherDropdown.xaml` 在同一批未提交改动里还包含一处**超出本节范围的重构**（触发器 `Background`/`CornerRadius`/`Foreground`/`FontWeight` 从硬编码 `{ThemeResource}`/`{StaticResource}` 改成 `{TemplateBinding ...}`，值改由 `DefaultEtherDropdownStyle` 的新 Setter 提供），这句"零回归"未覆盖到、也没有为它提供任何证据；详见 §12，结论是该重构本身不产生真实像素回归，但原表述容易被读成"整个 EtherDropdown.xaml 改动零回归"，特此更正范围。
- `Verify-WinUiConventions.ps1` 通过。
- `Verify-HighContrastPairing.ps1` 通过。
- `dotnet build src/Ether.DesignSystem.Controls` 0 警告 0 错误。

## 12. EtherDropdown 触发器属性来源重构与像素级回归核查（2026-08-30 复核）

### 背景

2026-08-30 复核 §11 的交付时发现，`src/Ether.DesignSystem.Controls/Controls/Inputs/EtherDropdown.xaml` 里除了 §11 描述的 Hover/Pressed 前景配对修复之外，还混入了一处**超出该任务范围的重构**：触发器（闭合态）Border `StateFill`/`OpenFill` 的 `Background`/`CornerRadius`、`TriggerText` 的 `Foreground`/`FontWeight`、`Arrow` 的 `Fill`，从模板里硬编码的 `{ThemeResource EtherDropdownFillDefaultBrush}`/`{StaticResource RadiusSm}`/`{ThemeResource EtherDropdownForegroundBrush}`/`{StaticResource WeightSemibold}`，改成了 `{TemplateBinding Background}`/`{TemplateBinding CornerRadius}`/`{TemplateBinding Foreground}`/`{TemplateBinding FontWeight}`，原来的值挪到 `DefaultEtherDropdownStyle` 的新增 `Setter Property="Background"/"CornerRadius"/"Foreground"/"FontWeight"` 里作为默认值。

**这个重构方向本身是对的，不应回退**：它与本仓库这一整轮审核一直在推进的方向一致——模板硬编码一个值会让消费方设置的同名公开属性（`Background`/`CornerRadius`/`Foreground`/`FontWeight`）在视觉上被直接忽略，改成 `TemplateBinding` 之后消费方设置的值才会真正生效。但它是在完成 §11 的 High Contrast 任务过程中顺手做的，未经独立验证就写下了"Light/Dark 视觉零回归"的结论（见上一节的更正）。

### 实测：58 像素差异的根因

按复核记录的方法——用 `PIL` 逐像素比较同一次 `Verify-ConsumerFixtures.ps1` 运行产出的 `dropdown-light.png`（`consumer-light` 屏幕矩阵里 `dropdown` 这一项，尺寸 426×80，来自 `RuntimeVerification.Infrastructure.cs` 的 `CaptureCurrentPngAsync`/`CaptureThemeScreenshotsAsync`，单帧截图、没有 §4.7 那种"采样到指纹收敛"的容差/收敛机制）——复核过程实测如下：

1. 仓库里已经保留了本次未提交改动过程中产生的 39 份 `artifacts/audit-runs/consumer-runtime-evidence-*` 证据目录，每份都含 `screenshots/controls/dropdown-light.png`，全部来自**同一份代码**（TemplateBinding 重构 + High Contrast 配对修复都已经在工作区里，这 39 次运行之间没有对 `EtherDropdown.xaml`做任何改动）。对全部 39 份文件取 SHA256，发现它们只落在两个不同的哈希值上（记作 A/B），并且 A/B 在 39 次运行之间不是单调的一次性跳变，而是反复交替出现（例如 …A, A, B, B, …, A, B, B, …, A, …），贯穿从 18:25 到次日 00:24 的整个时间窗口。这种"同一份代码、多次运行之间随机二值化交替"的模式是运行间渲染不确定性的特征，不是代码变更的特征（代码变更只会在改动发生的那一刻产生一次性跳变，不会在数十次后续运行里来回摆动）。
2. 逐像素比较 A、B 两个变体（`consumer-runtime-evidence-20260829-182500723` 与 `consumer-runtime-evidence-20260829-192237053` 的 `dropdown-light.png`），结果：总像素 34,080 个，58 个像素发生变化，最大单通道差值 46，改动像素全部落在第 35–48 行——与复核记录的"58 / 34080 像素不同，最大单通道差值 46，集中在第 35-46 行的文字带"逐数字吻合。也就是说，复核记录用来论证"TemplateBinding 重构导致回归"的那组像素差异，与**同一份代码在不同运行之间的截图噪声**在像素数、最大差值、发生位置上完全一致，二者无法区分。
3. 作为交叉验证，同样对全部 39 份 `dropdown-dark.png` 取 SHA256：全部 39 个哈希完全相同（只有 1 个哈希值），Dark 主题下没有观察到这种二值化噪声。这与"该噪声来自 Light 主题下某个特定合成/取整边界附近的次像素抗锯齿"的解释一致（Dark 主题下的具体颜色值可能没有落在同一个取整边界上），而不是所有截图普遍不稳定。
4. 额外把 `EtherDropdown.xaml` 临时替换回 `git show HEAD:...`（即今天所有未提交改动之前、TemplateBinding 重构与 §11 配对修复都不存在的版本），确认 `dotnet build` 通过（HEAD 版本本身编译无误，只是不满足现在的运行时/契约断言，未据此单独跑一次完整截图对比，因为已用第 1–3 步的证据确认了噪声的存在，且 HEAD 版本的模板与当前 `RuntimeVerification.Dropdown.cs` 等测试代码的假设不完全匹配，跑起来意义有限）；随后已恢复为工作区当前版本（`git diff --stat` 确认与恢复前一致）。

**结论**：58 像素差异是 `CaptureCurrentPngAsync`（用于 Light/Dark 控件截图矩阵）固有的运行间渲染噪声，与 §4.7 记录的另一条截图管线（`CaptureSettledVisualFingerprintAsync`）曾经出现过的问题同属一类——只是 §4.7 的收敛+容差修复只应用到了后者，没有覆盖到 `CaptureThemeScreenshotsAsync`/`CaptureCurrentPngAsync` 这条更简单的单帧截图路径。**没有证据表明 `EtherDropdown.xaml` 的 `TemplateBinding` 重构本身引入了真实的颜色/字重/位置回归**：`Background`/`CornerRadius`/`Foreground`/`FontWeight` 改走 `TemplateBinding` 后，其值来自 `DefaultEtherDropdownStyle` 新增的 Setter，取值（`EtherDropdownFillDefaultBrush`/`RadiusSm`/`EtherDropdownForegroundBrush`/`WeightSemibold`）与重构前模板里硬编码的值完全相同，且复核期间没有找到任何消费方（Gallery `DropdownPage.xaml`、`ConsumerFixtures` 的 `DropdownProof`/`DefaultDropdownProof`）在具体 `EtherDropdown` 实例上显式设置这四个属性、从而可能因为"重构前模板忽略本地值、重构后开始遵守本地值"而改变渲染结果的情形——所以这次复核没有发现需要修的东西。

### 结论与后续

- 不回退 `EtherDropdown.xaml` 的 `TemplateBinding` 重构。
- 已更正上一节"回归验证"里"Light/Dark 视觉零回归"一句的范围（见上），使其不再暗示涵盖了这处未记录的重构。
- **已知限制，未修复**：`CaptureThemeScreenshotsAsync`/`CaptureCurrentPngAsync`（`tests/Ether.DesignSystem.ConsumerFixtures/RuntimeVerification.Infrastructure.cs`）用于 Light/Dark 控件截图矩阵，没有 `CaptureSettledVisualFingerprintAsync` 那种收敛等待或容差比较，因此这条截图路径本身对逐字节比较不稳定（本节实测的二值化噪声就是例子）。当前 `Verify-ConsumerFixtures.ps1` 只用这些截图做"存在性 + 尺寸 + Light/Dark 哈希不相等"断言，没有做跨运行的逐字节比较，所以这个不稳定性目前不会导致门禁误报；但如果未来有人想用这些截图做"改动前后逐像素零回归"式的证据（就像本节的复核尝试做的那样），需要先按 §4.7 的方法把收敛/容差机制补到这条路径上，否则任何这种比较都会把噪声误判成真实差异，或者反过来把真实差异误判成噪声。这不是本次修复的任务范围，留在这里记录以免下次重蹈覆辙。

## 13. 属性总数与受审类型数变更（2026-08-30，本文档记录范围之外的后续提交）

本文档 §1、§4.1、§9 记录的 `1,501` 个公开可写属性、`482` 个视觉属性、`37` 个 Ether
自有 DP、"13 个受审类型"（含 `EtherSegmentedTrack`）都是 **2026-08-29 审核当时的准确
状态**，以下不是对那次审核的更正，而是记录此后代码本身发生的变化：

提交 `af909c93`（"refactor(controls): tighten the published surface before
release"，2026-08-30）将 `EtherSegmentedTrack` 从公开表面整体删除——库和 Gallery
都从未构造过它，只有本文档描述的审核验收代码构造它，且它的类型名与一个同名样式键
冲突。删除后 `RuntimeVerification.PublicPropertyInventory.cs` 的
`PublicPropertyInventoryTypes` 由 13 个类型降为 12 个（不再含
`EtherSegmentedTrack`），随之：

| 项目 | 2026-08-29（本文档记录） | 2026-08-30 起（当前） |
| --- | ---: | ---: |
| 受审类型数 | 13 | 12 |
| 公开可写属性总数 | 1,501 | 1,388 |
| 视觉属性（逐项可观察证据） | 482 | 445 |
| 语义属性 | 73 | 69 |
| 平台属性 | 946 | 874 |
| Ether 自有 DP | 37 | 35 |
| 标准交互适配器 | 12 | 12（不变） |
| 控件截图矩阵 | 13 控件 × Light/Dark | 13 控件 × Light/Dark（不变，控件类型集合本身没变，只是不再单列 `EtherSegmentedTrack` 这个无独立视觉契约的子类） |

§9 提到的 `CreateVisualFixture` 把 `EtherSegmentedTrack` 替换成
`EtherSegmentedControl` 渲染的说明，以及"公开属性代码调用仍以自身类型验证"的描述，
在 `EtherSegmentedTrack` 删除后不再适用（不存在需要替换渲染标本的子类了）。

当前权威数字见 `scripts/Verify-ConsumerFixtures.ps1` 的
`$expectedWritablePublicProperties` / `$expectedVisualPublicProperties` /
`$expectedSemanticPublicProperties` / `$expectedPlatformPublicProperties` /
`$expectedConsumedProperties`（35 项），以及
`tests/Ether.DesignSystem.ConsumerFixtures/RuntimeVerification.PublicPropertyInventory.cs`
的 `PublicPropertyInventoryTypes`（12 项）。同一轮提交还新增了 26 张控件黄金基线
PNG（`tests/Ether.DesignSystem.ConsumerFixtures/VisualBaselines/controls/`，13 控件 ×
Light/Dark）和一个真正在仓库外运行、不继承本仓库构建配置的外部消费者验证脚本
（`scripts/Verify-ExternalConsumer.ps1`），二者都晚于本文档记录的审核范围，不在
本文档 §1–§12 的验收结果内。
