# WinUI 官方符合性评审 + 修复记录（2026-08-31）

## 背景
用户报告 ToggleSwitch 从 On 点到 Off 会 `off→on→off` 闪烁，怀疑是逻辑/写法没按官方来。
由此扩展为：对除 SegmentedControl 外的每个组件，**对照官方 WinUI（microsoft-ui-xaml 模板 + 契约、WinUI-Gallery）**
审查其 code-behind 与模板是否忠实于官方做法（这些都是基础控件，行为理应与 WinUI 一致，只换皮肤）。

参考基准：WindowsAppSDK 2.3.1 / net8.0-windows10.0.19041。

## 核心洞察（贯穿全篇）
我们的组件分两类：
- **A 类**：给原生控件套 keyed Style + ControlTemplate（ToggleSwitch/TextBox/ScrollBar…）。交互逻辑在**闭源原生代码**里，
  它按固定"模板契约"驱动模板（特定 part 名、VisualState 组/状态/过渡）。**只要模板偏离该契约，闭源逻辑就可能在运行时踩坑。**
- **B 类**：真正的自定义控件（Button/Checkbox/Radio/Dropdown 子类，或 Slider/SteeringBar/ProgressBar/Masthead 自定义），有自己的 code-behind。

**教训**：静态对照官方模板能发现"缺 part/改名/缺状态"，但**判断"缺了会不会真出 bug"必须运行时验证**——
ToggleSwitch 的闪烁就是静态推理判成"无害 no-op"、实际是 P0 的例子。

---

## ToggleSwitch 深挖（本次主线）
### 闪烁根因
原生 ToggleSwitch 在**按下**时进入 `Dragging` 状态，期望闭源代码通过官方 part（`KnobTranslateTransform`/`SwitchKnobBounds`/`SwitchKnob`）按住旋钮/外观。
我们旧模板是**从零手写**：改了 part 名（`KnobTransform`）、缺 `SwitchKnob`/`SwitchKnobBounds`、用 Visibility 交换代替官方结构。
于是进 `Dragging` 时旋钮/轨道**掉回基础值（=Off 外观）**：
- Off→On：掉回 Off 但本就 Off，无可见变化 → 不闪。
- On→Off：掉回 Off（"快速变 Off"）→ 松开时 IsOn 仍 true 先滑回 On → 再翻转滑到 Off → **三段闪**。
（Setter 还是 Storyboard 无差别，离开状态值都掉。）

### 修法：以官方模板为骨架重做，只刷 Ether 皮肤
把 `EtherSwitch.xaml` 的 ControlTemplate 按官方 ToggleSwitch 默认模板重建：保留官方 part 名
（`SwitchKnob`/`SwitchKnobBounds`/`SwitchKnobOn`/`SwitchKnobOff`/`OuterBorder`/`OnTrackBacking`/`TrackGlow`/`KnobTranslateTransform`/`SwitchThumb`/`OffContentPresenter`/`OnContentPresenter`/`HeaderContentPresenter`）、
`CommonStates`/`ToggleStates`/`ContentStates` 结构；Ether 皮肤（灰/蓝渐变轨、白外描边旋钮、发光、三层阴影、左侧标签、焦点环、禁用态）刷到这些 part 上。
`ToggleStates` 最终为**自驱动**版：Off/On 各自显式设 `KnobTranslateTransform.X`(0/12) + 全部不透明度（0.0833s）；
`Dragging` 空；关键是 **`OnToDraggingTransition` 显式按住整套 On 外观（含 X=12、OuterBorder=0）**，
使 On→按下→Dragging 不再掉回 Off。移除了脆弱的 `RepositionThemeAnimation` + `{Binding TemplateSettings.*}`。

### 真正导致"整页闪退"的 bug（重做过程中引入，已修）
`<ColumnDefinition Width="{StaticResource Spacing8}" />` —— `Spacing8` 是 `Double` 资源，而 `ColumnDefinition.Width` 需要 `GridLength`，
类型错配 → 实例化时 `XamlParseException`（崩点 offset 三次相同即此）。改为字面量 `Width="8"`。
**方法论教训：应先用全局 UnhandledException 抓真实异常，别盲猜。**

### 验证（自验证，非用户点击）
用 UI Automation 从脚本驱动 Gallery：导航 Toggle 页无崩溃、`toggle-capable`=10（开关全部实例化）、
全部 9 个页面导航无崩溃、UnhandledException 日志为空；CopyFromScreen 截图确认 On/Off 静态外观正确、无伪影。
**用户已确认（2026-08-31）**：On→Off 闪烁消除、拖拽跟手、外观正确。ToggleSwitch 收尾完成。

---

## 本次已修（Batch 1：清晰无争议缺陷，均已构建通过 + 运行时 smoke 验证）
| 组件 | 问题 | 修法 | 文件 |
|---|---|---|---|
| **EtherSwitch** | 闪烁 + 拖拽不跟手 + 崩溃 | 官方骨架重做 + 修 ColumnDefinition | EtherSwitch.xaml |
| **EtherDropdown** | P0 `PlaceholderText` 不显示 | 无选中时 TriggerText 回退 PlaceholderText + 监听其变化 | EtherDropdown.cs |
| **EtherCheckbox** | P0 `IsChecked=null` 卡死 / P1 `IsThreeState` 抛异常 | null coerce 成 false；IsThreeState 静默 coerce（不再 throw） | EtherCheckbox.cs |
| **EtherRadioButton** | 同上 | 同上 | EtherRadioButton.cs |
| **EtherIntelligenceButton** | P1 `ContentTemplate`/`ContentTemplateSelector` 未绑定 | Cp 补 TemplateBinding | EtherIntelligenceButton.xaml |
| **EtherButton** | P1 FocusRing 被 FocusStates 与 Disabled 两组同控 | 从 3 个 Disabled 去掉 FocusRing.Visibility=Collapsed，FocusStates 独占 | EtherButton.xaml |
| **EtherSlider** | P1 自动化名不回退 Title | Peer 加 GetNameCore → Title | EtherSlider.Automation.cs |

注：Checkbox/Radio 的 null→false coerce 若绑定为 TwoWay `bool?`，会把 null 写回为 false（与其"二态"契约一致）。

---

## 硬化批次（2026-08-31，已完成 + 已验证）
用户要求"别留技术债，能硬化的都硬化"。已做（构建 0/0；UIA 全 11 页无崩溃；SteeringBar 越界 SetValue 实测夹取到 max、无死循环）：
- **Slider + SteeringBar：补鼠标滚轮步进**（`PointerWheelChanged` → 按 `SmallChange` 步进，夹取/吸附复用现有逻辑，仅可交互时响应）。
- **Slider：SnapToStops 的 `ValueChanged` oldValue 修正**（吸附时记录原始 oldValue，嵌套发事件时用它 → 一次吸附一次事件、oldValue=真实上一提交值）。
- **SteeringBar：数值夹取合并为单一来源**（`Value` CLR setter 不再 normalize，统一由 DP 回调 `OnRangePropertyChanged` 夹取，`_normalizingValue` 防递归）——消除双路径脆弱点，**未破坏公共 `ValueChanged`/args API**。
- **EtherButton：`Variant`/`Size` 解析失败不再静默**（未 Loaded 则延后到 Loaded 重试；仍失败输出 Debug 诊断）。
- **Dropdown：补齐 FocusStates 子态**（FocusedPressed/PointerFocused/FocusedDropDown + 元数据）。
- **Slider/SteeringBar 自动化 `SetValue` 只读时改抛 `ElementNotEnabledException`**。
- **IntelligenceButton：删死 `FontSize` setter + 修文档图标名（.png→.svg）**。

**仍搁置（本轮明确不做）**：SteeringBar 改从 `RangeBase` 派生（破坏性 API + 高回归风险，做了低风险等效硬化代替）；Radio 的 `AccessibilityView="Raw"`（需讲述人实测）。

## 待你拍板（Batch 2：属设计取舍 / 大改架构 / 涉及视觉设计，未擅自改）
- **有意省略的官方特性** — ✅ **已决定（2026-08-31，用户 No preference → 采纳默认）：全部确认为有意简化 / 不支持，不实现，文档化。** 逐项待补的"不支持"文案：
  - Input：无清除(X)按钮；不支持 `Header`/`Description`（标题/说明由外部自行摆放）。
  - ProgressBar：determinate-only；不支持 `IsIndeterminate`/`ShowError`/`ShowPaused`。（类注释已部分说明。）
  - Dropdown：紧凑不可编辑；不支持 `IsEditable`；触发区仅显文本（富 `ItemTemplate` 不在触发区渲染）。（类注释已部分说明。）
  - ScrollBar：有意常驻 6px 条；不做自动隐藏/hover 展开/Disabled 淡化。
  - Switch：不支持 `Header`；OffContent/OnContent 建议等宽（不等宽会跳宽）。
  - ✅ **已完成（2026-08-31）**：上述"不支持"说明已补进各组件注释——EtherInput.cs（无清除按钮/Header/Description）、EtherProgressBar.cs（determinate-only，无 indeterminate/error/paused）、EtherDropdown.cs（不支持 IsEditable；PlaceholderText 已支持；触发区仅文本）、EtherScrollBar.xaml（常驻条，无自动隐藏/展开/禁用淡化）、EtherSwitch.xaml（无 Header；OffContent/OnContent 建议等宽）。纯注释，构建 0/0。
- **无障碍**：
  - ✅ **已修（2026-08-31）**：Checkbox/Radio 键盘焦点不可见。加了**内描边焦点环**（紧贴 18×18 指示器、无白边光晕），中性高对比色（Light `AlphaBlack70` / Dark `AlphaWhite70`，HC `SystemColorHighlightColor`），仅键盘焦点时显示。**关键发现**：焦点环最初我用 `FocusStates` VSM 组驱动**无效**——现代 WinUI 控件不驱动 `FocusStates` 组，改由 code-behind `OnGotFocus/OnLostFocus`（`FocusState==Keyboard`）切换 `FocusRing` 才生效。已用真实键盘 Tab 截图确认。
  - ✅ **已实测排除误报（2026-08-31）**：我一度怀疑 EtherSwitch/Button/Dropdown/IntelligenceButton 的 FocusStates 也是死的——**实测证伪**：这 4 个键盘 Tab 时焦点环**都正常显示**（Switch 蓝环、Button 内圈、Dropdown 蓝环、IntelligenceButton 紫环）。原因：**只有 CheckBox/RadioButton 这两个基类在 WinUI3 里不驱动 FocusStates**（走系统焦点视觉），Button/ToggleSwitch/ComboBox 照常驱动。所以焦点环问题仅限 Checkbox/Radio，已修；其余无需改。
  - Radio：`AccessibilityView="Raw"` 加在内容 presenter 上（官方与 Checkbox 都没加）→ 非字符串内容可能对辅助技术隐藏，需讲述人验证。**未动**。
  - Dropdown：FocusStates 缺 FocusedPressed/PointerFocused/FocusedDropDown（其 FocusStates 组是活的，故此项仍成立但为次要）。**未动**。
- **大改架构**：SteeringBar 手工重实现 RangeBase（Value 双路 coercion + 自定义 ValueChanged 而非路由事件）→ 理想是改从 RangeBase 派生。
- **P2 风格/命名**：Button 丢 83ms BrushTransition；Slider `ValueText` part 实渲染 Title（应叫 TitleText）+ SnapToStops oldValue；SteeringBar 死主题笔刷（玻璃质感未渲染）；IntelligenceButton 硬编码 Sparkles 图标与文档示例冲突；等等（详见评审底稿）。

完全符合的：**EtherMasthead**（自定义控件写法规范，无 P0/P1）。

---

## 复核范围
本次复核 11 个组件 + 2 个基元（HandContentControl / EtherStringContentVisibilityConverter，均无符合性问题）。
SegmentedControl 一族按既定排除。
