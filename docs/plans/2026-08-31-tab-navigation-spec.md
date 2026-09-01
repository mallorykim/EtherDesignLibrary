# SPEC — Tab Navigation（EtherTabNavigation / EtherTabItem）

日期 2026-08-31 · 目标：新增一个"标签导航"组件。**以官方 WinUI 控件骨架换肤，能复制就别从 0 手写。**
参考：https://github.com/microsoft/WinUI-Gallery ，microsoft-ui-xaml（ListView / ListViewItem / ListViewItemPresenter 官方模板）。

Figma（file `ursRC201v8IiVeafliI45F`）：
- 单个 tab 组件 `Tab_buttons`：`Status=Default` node 62517:7077；变体集（Default/Selected/Hover/Pressed）node 62517:7076（亮）/ 62517:8474（暗）
- 组装条 `Tab nav`：node 62597:5204

---

## 0. 决策（已定，勿再改）

- **基座 = 官方单选 ItemsControl（ListView）换肤**。用户已拍板"ListView/Selector 选择器"。
  - 仅做**选择**（暴露 `SelectedIndex`/`SelectedItem`/`SelectionChanged`，均由 ListView 免费提供），**不承载/切换内容**——内容由 App 自行处理，和现有 `EtherSegmentedControl` 定位一致。
- **补齐 hover/pressed**：Figma 变体集已画了 Default/Selected/Hover/Pressed 四态（见 §2 token 表，全部有精确变量值，非臆造）。
- **disabled**：Figma **未**定义 → 按库内约定用"整体降透明 ~0.4"补（与 `EtherSegmentedControl` 的 `Disabled` 一致）。
- **图标 optional**：`EtherTabItem.Icon`（`IconElement`，默认 `null` → 图标区整体隐藏、无占位间隙）。

## 1. 命名与落位

- 控件类（typed subclass，和库内其它 Ether* 控件一致）：
  - `EtherTabNavigation : ListView` — `Controls/Navigation/EtherTabNavigation.cs`
  - `EtherTabItem : ListViewItem` — 同文件或 `Controls/Navigation/EtherTabItem.cs`
  - 命名对齐 WinUI 习惯（TabView/TabViewItem、NavigationView/NavigationViewItem）。
- 模板字典：`Controls/Navigation/EtherTabNavigation.xaml`（纯 XAML `ResourceDictionary`，风格照抄 `EtherSegmentedControl.xaml` 的组织方式）。
- 注册：在 `src/Ether.DesignSystem.Controls/Themes/Generic.xaml` 的 `MergedDictionaries` 里加一行
  `<ResourceDictionary Source="ms-appx:///Ether.DesignSystem.Controls/Controls/Navigation/EtherTabNavigation.xaml" />`
  （放在 Masthead 那一行附近即可）。
- Gallery：见 §6。

## 2. 精确规格（全部来自 Figma，勿改数值）

**单个 Tab（pill）**
| 项 | 值 | 库内 token |
|---|---|---|
| 内边距 | 8（水平）× 6（垂直） | `Spacing8` / `Spacing6`，或字面量 `8,6` |
| 圆角 | 8 | `RadiusMd`（= `radius/control`） |
| 图标↔文字间距 | 4 | `Spacing4` |
| 图标尺寸 | 12 × 12 | `icon-size-2xs`（字面量 12 亦可） |
| 字体 | Instrument Sans **SemiBold 12**，行高 1，居中 | `InstrumentSans` / `Size12` / `WeightSemibold` |

**Tab 条容器**
| 项 | 值 | token |
|---|---|---|
| 布局 | 水平、**左对齐**、内容宽度（**非等宽**） | — |
| tab 间距 | 10 | `Spacing10`（StackPanel `Spacing="10"`） |
| 容器内边距 | 8 × 6 | `8,6` |
| 数量 | 动态，**最少 2** | — |

**四态配色（亮 / 暗 —— 均为 Figma 变量精确值，已核对图元）**
| 态 | 背景 亮 | 背景 暗 | 文字/图标 亮 | 文字/图标 暗 |
|---|---|---|---|---|
| Default | Transparent | Transparent | `AlphaBlack70` | `AlphaWhite70` |
| Selected | `Gray1000`(#000) | `Gray0`(#fff) | `Gray0` | `Gray1000` |
| Hover | `AlphaBlack4`(#0A000000) | `AlphaWhite4`(#0AFFFFFF) | 同 Default | 同 Default |
| Pressed | `AlphaBlack6`(#0F000000) | `AlphaWhite6`(#0FFFFFFF) | 同 Default | 同 Default |

要点：
- **Selected 背景 = Figma `background/Tabs/Selected`，其值恰等于现有语义 token `background/surface-inverse`**（亮 Gray1000 / 暗 Gray0）。选中文字/图标 = 现有 `text/inverse` / `icon/inverse`（亮 Gray0 / 暗 Gray1000）。默认文字/图标 = 现有 `text/secondary` / `icon/secondary`（亮 AlphaBlack70 / 暗 AlphaWhite70）。这些语义 token **已存在**，无需新造。
- **Selected 态在 hover/pressed 时保持不变**（Figma 无 Selected+Hover 变体）——即 `SelectedPointerOver`/`SelectedPressed` 复用 Selected 外观，不叠加 hover/pressed 底色。
- 上表数值已逐一从 Figma 变量核对：Default 文字 `#000000b2`=AlphaBlack70 / `#ffffffb2`=AlphaWhite70；Hover `#0000000a`=AlphaBlack4 / `#ffffff0a`=AlphaWhite4；Pressed `#0000000f`=AlphaBlack6 / `#ffffff0f`=AlphaWhite6；Selected `#000000`=Gray1000 / `#ffffff`=Gray0（暗）。图元键均已在 `EtherPrimitives.xaml` 存在。

## 3. Token（照 `EtherSegmentedControl` 的双层做法）

`EtherSegmentedControl` 的既有做法是：语义 token 进 `EtherColors.xaml`（对齐 Figma 命名），控件私有 brush 进控件字典自己的 `ThemeDictionaries`（含 HighContrast）。**照此办理**。

### 3a. 语义 token（加到 `src/Ether.DesignSystem.Foundation/Resources/Tokens/EtherColors.xaml`）
在 Light 和 Dark 两个 `ThemeDictionary` 各加（命名照 Figma `background/Tabs/*` 的仓库小写斜杠约定，参照已有 `background/menuitem/*`）：
```
<!-- background / Tabs -->
背景 default : Transparent（两模式）
背景 selected: Light Gray1000 / Dark Gray0
背景 hover   : Light AlphaBlack4 / Dark AlphaWhite4
背景 pressed : Light AlphaBlack6 / Dark AlphaWhite6
```
key：`background/tabs/default` `background/tabs/selected` `background/tabs/hover` `background/tabs/pressed`。
（这是"token 名对齐 source"的记忆要求；即使 selected 等价于 `surface-inverse` 也照 Figma 名单独建，便于溯源。）

### 3b. 控件私有 brush（在 `EtherTabNavigation.xaml` 的 `ThemeDictionaries`：Light / Dark / HighContrast）
| brush key | Light | Dark | HighContrast |
|---|---|---|---|
| `EtherTabItemBackgroundSelectedBrush` | `background/tabs/selected`(Gray1000) | (Gray0) | `SystemColorHighlightColor` |
| `EtherTabItemBackgroundHoverBrush` | `background/tabs/hover`(AlphaBlack4) | (AlphaWhite4) | `SystemColorHighlightColor` |
| `EtherTabItemBackgroundPressedBrush` | `background/tabs/pressed`(AlphaBlack6) | (AlphaWhite6) | `SystemColorHighlightColor` |
| `EtherTabItemForegroundBrush`（默认字/图标） | `AlphaBlack70` | `AlphaWhite70` | `SystemColorWindowTextColor` |
| `EtherTabItemForegroundSelectedBrush` | `Gray0` | `Gray1000` | `SystemColorHighlightTextColor` |
| `EtherTabNavigationFocusStrokeBrush` | `Blue600` | `Blue500` | `SystemColorHighlightColor` |

（HC 下 hover/pressed/selected 底都用 `SystemColorHighlightColor`，文字用 `SystemColorHighlightTextColor`——和 `EtherSegmentedControl` 的 HC 处理一致；`HighContrastAdjustment="None"`。）

## 4. 骨架实现（关键：抄官方，别从 0 手写）

### 4a. `EtherTabNavigation : ListView`
- `DefaultStyleKey = typeof(EtherTabNavigation)`。
- 容器生成（让子项自动成为 EtherTabItem，支持显式子项 + `ItemsSource` 纯文本 tab）：
  - `protected override bool IsItemItsOwnContainerOverride(object item) => item is EtherTabItem;`
  - `protected override DependencyObject GetContainerForItemOverride() => new EtherTabItem();`
- 默认属性（在默认 Style 里 Setter）：
  - `SelectionMode = Single`（单选；单选 ListView 不会因再次点击而取消选中——符合 tab 语义）。
  - `IsItemClickEnabled = False`（走选择，不走 click）。
  - `Padding = "8,6"`、`HorizontalAlignment=Left`、`Background=Transparent`。
  - `IsTabStop=False`、`UseSystemFocusVisuals=False`、`HighContrastAdjustment=None`。
  - `ItemsPanel` = 水平 `StackPanel`（`Orientation=Horizontal` `Spacing=10` `HorizontalAlignment=Left`）。
- 模板（**从官方 ListView 默认模板起改**，保留官方 part 名）：`Border(Padding) > ScrollViewer > ItemsPresenter`。
  - `ScrollViewer`：`HorizontalScrollMode=Disabled` `HorizontalScrollBarVisibility=Hidden` `VerticalScrollMode=Disabled` `VerticalScrollBarVisibility=Disabled`（tab 条不出滚动条）。
  - 归零 ListView 自带留白（Padding 交给上面的 Border）。

### 4b. `EtherTabItem : ListViewItem`
- `DefaultStyleKey = typeof(EtherTabItem)`。
- **新增依赖属性 `Icon`（`IconElement`，默认 `null`）**：前置图标，12×12，`null` 时图标区 `Collapsed` 且不留 4px 间隙。
  - 图标着色跟随文字：把图标承载处（`ContentPresenter`/容器）的 `Foreground` 绑到与文字同一 brush，使 Selected 时图标一起变 `inverse`。
  - `null→Collapsed`：可用 `OnApplyTemplate` 取 `IconPresenter` + `Icon` 属性变更回调切 `Visibility` 并赋 `Content`（模板里不能对 IconElement 用 x:Bind；避免依赖字符串转换器）。
- Style Setter：`MinHeight=0` `MinWidth=0` `Margin=0` `HorizontalAlignment=Left`（内容宽度，不拉伸）`HorizontalContentAlignment=Center` `VerticalContentAlignment=Center` `Padding="8,6"` `FontFamily=InstrumentSans` `FontSize=Size12` `FontWeight=WeightSemibold` `UseSystemFocusVisuals=False` `HighContrastAdjustment=None`。

- **换肤方式（二选一，按优先级）**：
  1. **优先：retarget 官方 `ListViewItemPresenter` 的画刷属性**（最"抄官方"、改动最小、状态逻辑走原生）。
     即保留官方 ListViewItem 模板里的 `<ListViewItemPresenter .../>`，只把它的
     `SelectedBackground`/`SelectedPointerOverBackground`/`SelectedPressedBackground` → `EtherTabItemBackgroundSelectedBrush`，
     `PointerOverBackground` → Hover brush，`PressedBackground` → Pressed brush，
     `SelectedForeground` → `EtherTabItemForegroundSelectedBrush`，默认 `Foreground` → `EtherTabItemForegroundBrush`，
     `CornerRadius=8`、`ContentMargin="8,6"`，并**去掉选中指示条**（新 SDK：`SelectionIndicatorMode="None"`；`CheckMode` 关闭勾选标记）。
     **前提**：先核对 WindowsAppSDK **2.3.1** 的 `Microsoft.UI.Xaml.Controls.Primitives.ListViewItemPresenter` 是否具备上述属性（尤其 `SelectionIndicatorMode`、`SelectedForeground`）。具备就用这个。
  2. **回退：整套自绘 `ControlTemplate` + 分层不透明度底**（当 2.3.1 的 presenter 缺关键属性时用）。
     照抄 `EtherSegmentedControl.xaml` 里 `EtherSegmentTemplate` 的成熟做法（**该做法已在本仓解决过"ThemeResource setter 离开状态不回退"的坑**，用 Opacity 切换而非直接改 Background）：
     - 根 `Grid`，内含叠放的 `Border`：`HoverLayer`(Hover brush)、`PressedLayer`(Pressed brush)、`SelectedLayer`(Selected brush)，各 `CornerRadius=8`、`Opacity=0`。
     - 内容：`StackPanel Orientation=Horizontal Spacing=4` = `IconPresenter`(12×12，`Icon`) + 文本 `ContentPresenter`/`TextBlock`；`Foreground` 默认 `EtherTabItemForegroundBrush`。
     - `FocusRing` `Border`（`IsHitTestVisible=False` `Opacity=0`，`BorderBrush=EtherTabNavigationFocusStrokeBrush` `BorderThickness=2` `CornerRadius` 略大于 8、`Margin=-3`）。
     - VSM 组 **CommonStates**（用官方 ListViewItem 的状态名，由 ListView 驱动）：
       `Normal`(全 0) · `PointerOver`(HoverLayer=1) · `Pressed`(PressedLayer=1) ·
       `Selected` / `SelectedUnfocused`(SelectedLayer=1 + 文本&图标 Foreground=Selected brush) ·
       `SelectedPointerOver` / `SelectedPressed`(同 Selected，**不叠 hover/pressed 底**) ·
       `Disabled` / `SelectedDisabled`(根 `Opacity=0.4`，其余同各自基态)。
       用 `Setter`（对 Opacity/Foreground 都能干净回退）而非改 Background。
     - VSM 组 **FocusStates**：`Focused`(FocusRing.Opacity=1) · `Unfocused` · `PointerFocused`（空）。
       注：ListViewItem 的 FocusStates 由框架驱动（不同于 CheckBox/RadioButton 那个死组），但仍需**运行时**确认焦点环只在键盘焦点出现。
- 保留官方模板里其它 VSM 组（`DisabledStates`/`MultiSelectStates`/`ReorderHintStates`/`DragStates`/`DataVirtualizationStates` 等）原样不动，只重塑 `CommonStates` + `FocusStates`。

### 4c. 键盘 / 无障碍
- ListView 自带方向键移动 + 选择（`SingleSelectionFollowsFocus` 默认 true → 方向键即切换选中，符合 tab）。保持默认。
- 无障碍：ListView 暴露 List + SelectionItem 模式，v1 可接受。**可选增强**（非必须）：为 EtherTabItem 提供报告 `Tab`/`TabItem` 控件类型的 AutomationPeer；如实现有风险则跳过并记 TODO。

## 5. 用法（目标 API，供 Gallery/文档）
```xml
<controls:EtherTabNavigation SelectedIndex="0" SelectionChanged="...">
    <controls:EtherTabItem Content="Overview">
        <controls:EtherTabItem.Icon><FontIcon Glyph="&#xE80F;"/></controls:EtherTabItem.Icon>
    </controls:EtherTabItem>
    <controls:EtherTabItem Content="Details"/>
    <controls:EtherTabItem Content="History"/>
</controls:EtherTabNavigation>
```
- 显式 `EtherTabItem` 子项为主用法（图标用 `Icon`）。`ItemsSource` 绑纯文本 tab 亦可开箱工作（无图标）。

## 6. Gallery（照 `SegmentedControlPage` 样板）
- `samples/.../ComponentCatalog.cs`：在 **Navigation** 分类（Masthead 旁）加一行
  `new ComponentEntry("Tab Navigation", typeof(TabNavigationPage), IsUpdated: true),`
- 新页 `samples/.../Views/Navigation/TabNavigationPage.xaml`(+`.cs`)，命名空间 `EtherSandbox.Views.Navigation`，用 `views:ComponentPage` + `views:ControlExample`：
  - **InteractiveContent**：2 / 3 / 5 个 tab 的实例；至少一组含前置图标；`SelectionChanged` 把 `SelectedItem` 文本写到 `OutputText`；`SourceXaml` 绑 `SpecimenXaml`。
  - **StatesContent**：Default / Selected / Hover / Pressed 四个静态样例。参照该页 `HandRadioButton.PreviewState` 的既有机制——用一个 Gallery-only 的 `EtherTabItem` 子类（如 `HandTabItem`）加 `PreviewState` 强制视觉态、`IsHitTestVisible=False`；若成本高，可用 code-behind 在 Loaded 时 `VisualStateManager.GoToState` 强制（非交互）。
  - `HasDisabledToggle="True"`（演示 disabled 降透明）。
- x:Uid 本地化键照抄同类页的写法即可。

## 7. 验收
- x64 / Debug 构建 0 error（先 `dotnet workload restore Ether.DesignSystem.slnx`，再
  `dotnet build src/Ether.DesignSystem.Controls/Ether.DesignSystem.Controls.csproj -p:Platform=x64 -p:Configuration=Debug -restore`；Gallery 同法）。
- 运行时（自验证，别让用户点）：Gallery 导航到 Tab Navigation 页无崩溃；四态、亮/暗、有无图标外观符合上表；键盘方向键切换选中、焦点环仅键盘出现；disabled 降透明。
- 不改动 SegmentedControl 及其它组件。

## 8. 明确不做（除非另行拍板）
- 不承载/切换内容（无内容区）。
- 无 overflow/更多菜单、无关闭/新增/拖拽（那是 TabView/NavigationView 的范畴）。
- `ItemsSource` 数据项**逐项图标**（需 ItemTemplate）超范围——图标场景用显式 `EtherTabItem`。
