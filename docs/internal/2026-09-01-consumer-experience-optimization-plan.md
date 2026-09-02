# 消费者体验优化计划 — 让第三方开发者更容易接入 Ether

> **日期：** 2026-09-01 **分支：** `codex/refine-components` **性质：** 规划文档（本文不含任何代码改动、构建或运行）。
>
> **前提（已完成，本文不再重做）：** 15 个组件已全部通过 consumability + completeness 两项审查，
> `ConsumerFixtures` 运行时验证为绿（`{"marker":"ETHER_CONSUMER_SMOKE","outcome":"success"}`），全部已提交。
> 已证明的能力：所有公开属性都是 `DependencyProperty`；12 个状态属性的 TwoWay 绑定
> （`tests/Ether.DesignSystem.ConsumerFixtures/RuntimeVerification.R2.cs:57-194`）；按钮族 `Command`
> （`R2.cs:374-431`）；Dropdown / SegmentedControl / TabNavigation 的 `ItemsSource` 数据路径
> （`R2.cs:196-236`）。完整收口状态见
> [`consumability/_SUMMARY.md`](consumability/_SUMMARY.md) 与
> [`2026-09-01-consumability-remediation-plan.md`](2026-09-01-consumability-remediation-plan.md)。
>
> **本文回答的问题：** 组件"能用"之后，怎样让一个不熟悉本仓库的第三方开发者**更快、更省事地**用起来。
>
> **工作量标尺：** S ≤ 半天；M = 半天到 2 天；L > 2 天。
> **执行方式：** 沿用 remediation plan 的约定——编码由 Codex 执行、另一 agent 分波审查；
> 每个"接受标准"都必须有仓库内的证据（fixture / 脚本 / 门禁），不接受"应该没问题"。

---

## 0. 一页总览（推荐顺序）

| 序 | 项目 | 一句话 | 对消费者的价值 | 工作量 | 需要用户决策 |
| --- | --- | --- | --- | --- | --- |
| **P0** | 推送分支 + 开 PR | 把 73 个从未经过托管 CI 的提交推上去，让 CI 先跑一遍 | 间接（保护成果、暴露 CI 问题） | **S** | 否 |
| **P1** | 消费者文档：每控件"可复制 + 绑数据 + 接动作"示例 + MVVM 章节 | 现有文档只教"长什么样"，不教"怎么绑、怎么接" | **最高** | **M** | 小（文档语言、是否推荐 Toolkit.Mvvm） |
| **P2** | Gallery 数据绑定示范 | 把已证明的 `ItemsSource`/TwoWay/`Command` 能力做成能看见、能复制的活文档 | 高 | **M** | 小（Gallery 是否引入 MVVM 包） |
| **P3** | SegmentedControl 增加可选 `Command`（其余控件评估后不加） | 数据驱动的分段控件目前没有任何"接动作"钩子，只能写事件胶水 | 中 | **M** | **是**（API 命名、是否只对用户操作触发） |
| **P4** | Card 可发现性：A 方案（文档补全，并入 P1）/ B 方案（做成真类型，暂缓） | `<Border Style=…>` 六个 key 很难被 IntelliSense 发现 | 中低 | A **S** / B **L** | **是**（设计决策） |
| **P5** | 收尾：packaged MSIX 运行时验证 | 目前**没有任何脚本**会安装/启动 MSIX；发布阻断规范已判定为非阻断 | 低（内部 x64 unpackaged 优先） | **M**（依赖机器） | **是**（是否列为发布前必做） |

**推荐顺序：P0 → P1 → P2 → P3（若批准）→ P4-A（随 P1 完成；P4-B 挂起）→ P5（若批准，需 Developer Mode 机器）。**

理由：P0 几乎零成本且先于一切（CI 反馈会影响后续所有工作）；P1 是价值/成本比最高的一项，且不改产品 API；
P2 让 P1 的示例有"活的"对照物；P3 是唯一值得做的 API 增补，做完后 P1/P2 再补一段即可；
P4-B 与 P5 都是需要拍板的设计/环境项，不应阻塞前四项。

---

## P0 — 推送分支并开 PR（S）

**一句话：** 现在的成果只存在本机，先备份并让 CI 跑一遍。

**技术细节：** `origin` 上只有 `main` 与 `codex/refactor`，两者都停在 `8029f13`；当前分支
`codex/refine-components` 领先 `main` **73 个提交**且**没有 upstream**（`git branch -vv` 无跟踪信息）。
这意味着 remediation 全部工作从未经过 `.github/workflows/build.yml` 的托管 CI
（它在 `push` / `pull_request` 时触发，`build.yml:9-11`），也就是 `scripts/Gates.psd1:48-74` 里 26 个
`ci` 门禁没有一个在托管环境验证过这 73 个提交。

**步骤：**
1. `git push -u origin codex/refine-components`。
2. `gh pr create --base main --draft`，PR 描述链接三份文档：
   `consumability/_SUMMARY.md`、`2026-09-01-consumability-remediation-plan.md`、
   `2026-09-01-component-consumability-review-spec.md`；说明 runtime 证据文件路径
   （`artifacts/consumer-fixtures/runtime-result-9a2b552efe3b4c8bb6c9280635e54b77.json`）。
3. 等 `build` 与 `package-consumers` 两个 job 完成；重点看最后一步 `Verify-GateManifest.ps1`
   （`build.yml:134-140`）——它断言工作流步骤与 `Gates.psd1` 一致，是最容易因本地新增脚本而红的门禁。
4. CI 红则按门禁输出修；绿后把 PR 从 draft 转正式（合并与否由用户决定）。

**接受标准：** 分支在 `origin` 上；PR 存在；CI 两个 job 绿。
**不做：** 不 `nuget push`（`HANDOFF.md:3` — `Publish-Internal.ps1` 是唯一授权入口，且发布状态仍是 hold，
`docs/releases/0.1.0-preview.1.md:3`）。

---

## P1 — 消费者文档：每控件"可复制粘贴 + 绑数据 + 接动作"（M，价值最高）

**一句话：** 现在的接入文档告诉开发者控件"长什么样"，但没有告诉他们"怎么把数据绑上去、怎么让点击触发自己的代码"。

### 1.1 现状评估（`docs/consumers/getting-started.md`）

已经很好的部分：GitHub Packages 接入（§1）、最小工程与 `App.xaml` 合并（§2，`:75-227`）、
故障排查表（§3，`:231-239`）、不支持属性登记（§4.1，`:261-273`）、版本纪律（§6）。

**缺口（逐条核实）：**

| # | 缺口 | 证据 |
| --- | --- | --- |
| G1 | §5"各控件标记参考"（`:305-446`）**只有外观属性**，全篇没有一处 `x:Bind`、`Command=`、`ItemsSource` | 对全文 grep `TabNavigation\|ItemsSource\|x:Bind\|Command=\|ControlInteractionAdapter` 仅命中 `:15`、`:107`（都是包引用行） |
| G2 | **Tab Navigation 没有 §5 条目** — 15 个组件里唯一缺席的 | §5 只有 14 段：Button…ScrollBar；`EtherTabNavigation` 在文档中零出现 |
| G3 | SegmentedControl 的 D1 数据驱动契约（`ItemsSource`/`ItemTemplate`/`DisplayMemberPath`/`SelectedIndex`/`SelectedItem`，`src/Ether.DesignSystem.Controls/Controls/Inputs/EtherSegmentedControl.cs:37-126`）**未写入文档**；§5 仍只教内联 `EtherSegmentRadioButton` 写法（`:363-379`） | 同上 |
| G4 | `Ether.DesignSystem.Interactions`（三个包之一）**没有任何用法示例**；包内 README 只有散文没有代码（`src/Ether.DesignSystem.Interactions/README.md:1-12`） | `ControlInteractionAdapter` 已有 10 个 Observe\* 方法（`src/Ether.DesignSystem.Interactions/ControlInteractionAdapter.cs:21-182`）却无人知道怎么调 |
| G5 | Masthead 的 `ActionInvoked` 事件（`Controls/Navigation/EtherMasthead.xaml.cs:119-120`）、Slider/SteeringBar 的 `StepFrequency`/`Stops`/`SnapToStops`、Dropdown 的 `MaxVisibleItems`/`MenuGap`（`EtherDropdown.cs:126-154`）、Button 的 `Variant`/`Size`/`LeftIcon`/`RightIcon`（`EtherButton.cs:87-145`，仅在 §2 示例顺带出现）——这些 remediation 新增或已存在的"旋钮"在 §5 无示例 | 对照各控件 DP 清单 |
| G6 | **仓库根目录没有 README.md**；能把人引到 `docs/consumers/getting-started.md` 的只有包 README 的一行（`src/Ether.DesignSystem.Controls/README.md:5`） | `Glob README.md` 根目录无命中 |
| G7 | 文档自身的防腐规则（上一轮计划 B6.4："README 中每个代码块必须能在 fixture/Gallery 找到等价物"，`docs/plans/2026-08-30-third-party-consumability-plan.md:283`）只对现有 14 段成立；新增的绑定/命令示例目前在 fixture 里**没有 XAML 形态的对照物**（TwoWay/Command 证明是 C# 代码构造的，`R2.cs:49-55, 388-398`） | — |

### 1.2 具体改动

**A. §5 每控件改为固定三段式**（"外观 → 绑数据 → 接动作"），每段引用已存在的证明：

| 控件 | 绑数据（已证明的属性） | 接动作 | 证据 |
| --- | --- | --- | --- |
| EtherButton / EtherIntelligenceButton | `Content`, `IsEnabled` | `Command="{x:Bind ViewModel.SaveCommand}"` + `CommandParameter` | `R2.cs:388-398` |
| EtherCheckbox | `IsChecked` TwoWay（两态，null→false） | `Command`（Toggle 触发） | `R2.cs:68-76, 400-412` |
| EtherRadioButton | `IsChecked` TwoWay + `GroupName` | `Command`（SelectionItem 触发） | `R2.cs:78-86, 419-431` |
| EtherInput | `Text` TwoWay | `TextChanged="{x:Bind ViewModel.OnQueryChanged}"` | `R2.cs:88-96` |
| EtherDropdown | `ItemsSource` + `SelectedItem`/`SelectedValue`(+`SelectedValuePath`) TwoWay | `SelectionChanged` → x:Bind 方法 | `R2.cs:98-116`；`Interactions/README.md:9` 的 `SelectedValuePath` 建议 |
| EtherSegmentedControl | `ItemsSource` + `DisplayMemberPath`/`ItemTemplate` + `SelectedIndex`/`SelectedValue` TwoWay；内联写法保留 | `SelectionChanged`（`SegmentedSelectionChangedEventArgs` 带 Old/New）；若 P3 批准则加 `Command` | `R2.cs:118-136, 213-224` |
| EtherTabNavigation（**新增条目**） | `ItemsSource` + `SelectedIndex`/`SelectedItem` TwoWay | `SelectionChanged`（标准 `Selector` 事件） | `R2.cs:168-186, 205-211` |
| EtherSlider / EtherSteeringBar | `Value` TwoWay、`StepFrequency`、`Stops`+`SnapToStops` | `ValueChanged`；**注明每次拖动 tick 都会触发** | `R2.cs:138-146`；`EtherSteeringBar.xaml.cs:381-384, 476-487` |
| EtherSwitch（Style） | `IsOn` TwoWay | `Toggled` | `R2.cs:148-156` |
| EtherProgressBar | `Value`/`Title`/`ValueContent`/`ShowTitle`/`ShowValue` OneWay | 无（只读展示） | `EtherProgressBar.cs:60-93` |
| EtherMasthead | `ShowSettings`/`ShowSearch`/`ShowMenuIcon`/`ShowChevron`/`EnableWindowCommands` | `ActionInvoked`（`MastheadAction` 枚举）；图标槽位为装饰（D2 🚫） | `EtherMasthead.xaml.cs:120-214, 543/561/585` |
| EtherCard / EtherScrollBar（Style） | 无 Ether 属性 | 无 | 见 P4-A |

**B. 新增 §7"绑定到 ViewModel 与 MVVM 模式"**（放在 §5 之后、§6 之前，或独立文件
`docs/consumers/mvvm-patterns.md` 并从 getting-started 链接）：

1. **`x:Bind` TwoWay 到 `INotifyPropertyChanged` VM** — 直接给出与 fixture 同形的 VM
   （`R2.cs:17-45` 的 `TwoWayViewModel` 模式）+ 一页 XAML；强调 `x:Bind` 默认 `OneTime`，可编辑属性必须写
   `Mode=TwoWay`（这是 WinUI 最常见的踩坑，`deepwiki/WinUI3-XAML-AI-Coding-Guide.md:14`）。
2. **按钮族 `Command`/`CommandParameter`** — 已证明四个控件，直接引用。
3. **事件 → VM 方法：`{x:Bind ViewModel.OnSelectionChanged}`** — WinUI 内建的事件绑定，
   **不需要额外 NuGet 包**，签名可与事件一致或无参。这是"没有 Command 的控件"（Dropdown、TabNavigation、
   Slider、SteeringBar、Switch、Input）的**首选答案**，比引入 Behaviors 更轻。
4. **`Microsoft.Xaml.Interactivity`（`EventTriggerBehavior` + `InvokeCommandAction`）作为备选** —
   给一段示例，但明确标注：该包**不在**本仓库 `Directory.Packages.props:6-13` 里，属于消费者自选依赖；
   本仓库不会为它做 fixture 证明（除非用户决定把它加进 fixtures，见 1.4）。
5. **`ItemsSource` 三例**（Dropdown / SegmentedControl / TabNavigation）+ 两个诚实的限制说明：
   - TabNavigation 的 `Icon` 是 **容器（`EtherTabItem`）属性**（`EtherTabNavigation.cs:28-41`），
     数据驱动生成的 tab **拿不到 per-item 图标**（无 `PrepareContainerForItemOverride`，`EtherTabNavigation.cs:9-18`）；要图标请用内联 `EtherTabItem`。
   - Dropdown 触发器只显示文本（`DisplayMemberPath`/`ToString`），富 `ItemTemplate` 只在列表里生效
     （`EtherDropdown.cs:42-43`）。
6. **Interactions 适配器一例到底**：`ObserveButton` / `ObserveSelection` / `ObserveMasthead` →
   订阅 `InteractionProduced` → 写入应用 outbox（`ControlInteractionAdapter.cs:17-18, 57-69, 115-127`）。
   定位为"后端事件信封"，**不是** MVVM 胶水，避免读者混淆两层。
7. **常见错误对照**：`{Binding}` vs `{x:Bind}` 的 `DataContext` 差异；`DataTemplate` 里 `x:Bind` 必须写
   `x:DataType`；`IsChecked` 是 `bool?`，VM 用 `bool?` 或转换。

**C. 根目录 `README.md`（S，可与 P0 同批）**：三段——这是什么、怎么接入（链接 getting-started）、
仓库地图（`src/`、`samples/`、`tests/`、`docs/internal/consumability/`、`scripts/Gates.psd1`）。

**D. 防腐（可选，M；不做则用人工规则）**：把 §5/§7 的 XAML 示例落到 fixture 里一个专门的
`DocsSnippets` 区域（`tests/Ether.DesignSystem.ConsumerFixtures/Unpackaged/MainWindow.xaml` 已有 13 段
`*Proof` 标记的模式，`:44-293`），并新增 `scripts/Verify-ConsumerDocSnippets.ps1` 断言"文档代码块 ⊆ fixture 标记"。
注意：任何新 `Verify-*.ps1` 都必须同时登记到 `scripts/Gates.psd1` 与 `build.yml`，否则
`Verify-GateManifest.ps1` 会红（`Gates.psd1:101-114`）。

### 1.3 工作量与顺序
- A + B + C：**M**（约 1–1.5 天，纯文档 + 一次本地 `Verify-UnsupportedProperties.ps1`，因为 §4.1 表格是脚本生成/校验的，`:249-254`）。
- D：额外 **M**；建议先按人工规则执行 A/B，D 留到 P2 之后（Gallery 示例本身就是第二份活对照）。

### 1.4 需要用户决策
1. **文档语言**：getting-started 是中文，`docs/internal/2026-09-01-*` 是英文。建议消费者文档保持中文（读者是内部团队），术语用英文原样。
2. **是否"钦定" `CommunityToolkit.Mvvm`** 作为示例中的 VM 写法（`[ObservableProperty]`/`[RelayCommand]`）。
   它只影响消费者侧、不影响库本身；仓库内的 deepwiki 指南已推荐它（`deepwiki/WinUI3-XAML-AI-Coding-Guide.md:708-714, 742`）。
   建议：示例主线用手写 `INotifyPropertyChanged`（与 fixture 一致、零依赖），旁注给出 Toolkit 等价写法。
3. **是否把 `Microsoft.Xaml.Interactivity` 加入 fixtures** 以便对 Behaviors 示例做运行时证明。建议：不加，文档明示"消费者自选、未经本仓库验证"。

---

## P2 — Gallery 数据绑定示范（M）

**一句话：** Gallery 现在所有示例都是手写死的项和代码后置事件，看不出"绑一个集合、绑一个 ViewModel"长什么样。

**技术细节（现状）：**
- `samples/Ether.DesignSystem.Gallery/Views/Controls/SegmentedControlPage.xaml:19-94`：三组内联
  `EtherSegmentRadioButton`，输出靠代码后置 `SelectionChanged` 处理器（`.xaml.cs:31-35`）；
  `SpecimenXaml` 也只有内联形态（`.xaml.cs:9-20`）。
- `Views/Navigation/TabNavigationPage.xaml:12-33`：三组内联 `EtherTabItem`；`.xaml.cs:21-25` 同样是代码后置。
- `Views/Controls/DropdownPage.xaml:44-59`：九个内联 `ComboBoxItem`。
- `Views/Controls/ButtonPage.xaml:49-138`：七处 `Click=`，**没有一处 `Command=`**。
- Gallery 工程没有任何 MVVM 包（`Ether.DesignSystem.Gallery.csproj:18-24` 只引用 WASDK），也没有 `ViewModels/` 目录。

**具体改动（每页加一个"DATA-DRIVEN"块，放在现有块之后，不动现有块）：**

1. **SegmentedControlPage**：新增第四块
   `ItemsSource="{x:Bind Periods}" DisplayMemberPath="Label" SelectedItem="{x:Bind SelectedPeriod, Mode=TwoWay}"`；
   页面类加 `ObservableCollection<PeriodItem> Periods` + `SelectedPeriod`（实现 `INotifyPropertyChanged`）；
   OutputText 由 `SelectedPeriod` 的 setter 更新（证明 TwoWay 真的回写），不再只靠事件。
   同步更新 `SpecimenXaml` 为"内联 + 数据驱动"两段。
2. **TabNavigationPage**：新增 `ItemsSource="{x:Bind Sections}"` + `DisplayMemberPath`（或 `ItemTemplate`）+
   `SelectedIndex="{x:Bind SelectedSectionIndex, Mode=TwoWay}"`；在块标题旁用一行说明"数据驱动 tab 不支持 `Icon`"（与 P1-B-5 一致）。
3. **DropdownPage**：新增 `ItemsSource` + `SelectedValuePath="Id"` + `SelectedValue` TwoWay 的块（对应 Interactions README:9 的建议写法）。
4. **ButtonPage**（可选，S）：把其中一个按钮改为 `Command="{x:Bind SaveCommand}"`，页面类提供一个最小 `ICommand`
   （可直接复用 fixture 的 `RecordingCommand` 形态，`R2.cs:364-372`），让"按钮族原生支持 Command"可见。
5. 视图模型放 `samples/Ether.DesignSystem.Gallery/ViewModels/`（或页面内 `partial`），**手写 INPC**，不引入新包。

**门禁影响（已核对，均不阻断）：**
- `scripts/Verify-GalleryControlExample.ps1:109-127` 只断言页面仍在 `ComponentPage`/`ControlExample` 上、
  `SourceXaml` 绑定 `SpecimenXaml`、代码含控件名——新增块不触发。
- `scripts/Verify-GalleryLocalization.ps1:89-102` 只要求 `ComponentPage` 的 `x:Uid` Title/Description 与固定 key 集合（`:60-84`）；
  页面内新 `TextBlock` 的 `x:Uid` 是**惯例**而非门禁（架构文档承认"page body copy 仍多为英文"，
  `docs/architecture/2026-08-26-l4-gallery-control-example.md:90`）。建议仍按惯例加 `x:Uid` 并在
  `Strings/en-US/Resources.resw` 登记（现有 `GalleryOutput.Selected` 在 `:1558`）。
- 目录 `ComponentCatalog.cs:61-83` 不需改（不新增页面）。

**验证：** CI 两个静态门禁 + 本地 `scripts/Verify-GallerySmoke.ps1`（`Gates.psd1:78`，local-runtime）。
**工作量：** M（约 1 天，含一次本地 Gallery smoke）。
**决策：** Gallery 是否引入 `CommunityToolkit.Mvvm`。建议不引入（Gallery 是 `ProjectReference` 宿主，
不应暗示库有该依赖；手写 INPC 与 fixture 一致）。

---

## P3 — 把更多交互暴露为 `ICommand`：逐控件评估（M，需决策）

**一句话：** 让开发者少写"事件处理器里调 ViewModel"这种胶水代码；但只在真的省事、又不偏离 WinUI 原生契约的地方加。

**约束：** `AGENTS.md:8-13` 要求组件"行为与官方 WinUI 对应控件一致、只差外观"；偏离必须写明理由
（`AGENTS.md:37-40`）。原生 WinUI 里只有 `ButtonBase` 族有 `Command`；`Selector`（ComboBox/ListView）、
`RangeBase`（Slider）、`ToggleSwitch` 都没有。所以对**薄子类**加 `Command` 是偏离，对 **Ether 自有类型**
（`ContentControl`/`Control` 手写模板）加是合理的增补——这与 D1（SegmentedControl 加 `ItemsSource`）、
D2（Masthead 加 `ActionInvoked`）的先例一致。

**评估表：**

| 控件 | 基类 | 现有通知面 | 原生对应有 Command？ | 结论 |
| --- | --- | --- | --- | --- |
| **EtherSegmentedControl** | `ContentControl`（Ether 自有，`EtherSegmentedControl.cs:28`） | `SelectionChanged` 事件（`:129`）+ 5 个选择 DP | 无直接对应（`RadioButtons` 也无） | **建议加**（见下） |
| EtherSteeringBar | `Control`（Ether 自有，`EtherSteeringBar.xaml.cs:78`） | `ValueChanged`（`:131`），**每个拖动 tick 都触发**（`:381-384 → :476-487`），松手（`:861-869`）无单独"提交"通知 | `Slider` 无 | **不加**：Command 会每 tick 执行一次，语义错误；若有需求先加 `ValueCommitted` **事件**（新语义，另立项） |
| EtherSlider | `RangeBase`（`EtherSlider.xaml.cs:45`） | 原生 `ValueChanged`（`:313`），同样每 tick；`:604-614` 松手无提交事件 | 无 | **不加**，同上 |
| EtherTabNavigation | `ListView`（`EtherTabNavigation.cs:9`） | 原生 `SelectionChanged` | 无（`NavigationView` 也只有 `ItemInvoked` 事件） | **不加**：偏离 ListView 契约；用 `{x:Bind}` 事件绑定或 `ObserveSelection`（`ControlInteractionAdapter.cs:57-69`） |
| EtherDropdown | `ComboBox`（`EtherDropdown.cs:67`） | 原生 `SelectionChanged` | 无 | **不加** |
| EtherMasthead | `Control` | `ActionInvoked`（`:120`），在宿主窗口命令之前触发（`:543/561/585`），不可取消 | 无对应 | **不加**：三个动作已是窗口命令，VM 很少需要接管；若需"拦截关闭"是另一个需求（可取消参数） |
| EtherSwitch | 仅 keyed Style（`EtherSwitch.xaml.cs:6-13`，刻意无类型） | `Toggled` | `ToggleSwitch` 无 | **不能加**（无类型可挂 DP；`IsOn` TwoWay 已足够） |
| EtherInput | `TextBox`（`EtherInput.cs:28`） | `TextChanged` | 无 | **不加** |
| Button / IntelligenceButton / Checkbox / RadioButton | `ButtonBase` 族 | 原生 `Command` | 有 | **已完成**（`R2.cs:374-431`） |

**为什么 SegmentedControl 值得加：** 内联分段是 `EtherSegmentRadioButton : RadioButton`
（`EtherSegmentRadioButton.cs:26`），每个分段**已经**有原生 `Command`；但 **`ItemsSource` 生成的分段**
由 `CreateGeneratedSegment` 内部创建（`:251-263`），消费者没有任何钩子给它们设 `Command`——数据驱动路径
是 D1 刚加的主推路径，却是唯一"只能写事件胶水"的地方。

**设计草案（供决策）：**
- 新增 `SelectionCommand : ICommand?` + `SelectionCommandParameter : object?` 两个 DP（注册模式同 `:37-77`）。
  `CommandParameter` 为 null 时以 `SelectedValue` 作为参数。
- **仅对用户操作触发**：在 `OnSegmentChecked`（`:373-377`）→ `ApplySelection`（`:382-401`）路径上带
  `userInitiated` 标志，程序设置 `SelectedIndex`/`ItemsSource` 重建（`:214-249`）**不**触发——与
  `ButtonBase.Command` 只在用户点击时执行的语义一致。执行时机放在 `SelectionChanged` 事件之后（`:195-197`）。
- 证明：扩展 `VerifyCommands`（`R2.cs:374-386`）加一例，用 `RecordingCommand`（`:364-372`）+ 分段的
  `SelectionItem.Select()`（`:419-431` 的先例）断言"恰好执行一次、参数正确"，再断言程序设 `SelectedIndex` **不**执行。
- 登记：`src/Ether.DesignSystem.Controls/PublicAPI.Unshipped.txt`（模式见 `:203-204`）；
  `scripts/Verify-EtherSegmentedControlContract.ps1` 若断言公开面需同步；`consumability/EtherSegmentedControl.md` 的 C3 行；
  getting-started §5 与 P2 的 Gallery 块各补一行。
- 文档中写明这是相对官方骨架的**增补**及理由（`AGENTS.md:39-40`）。

**工作量：** M（约 1 天：DP + 触发逻辑 + fixture + PublicAPI + 文档 + 一次 `Verify-ConsumerFixtures.ps1`）。
**需要决策：**
1. 做不做。
2. 命名：`Command`/`CommandParameter`（与 `ButtonBase` 同名，IntelliSense 最熟悉）还是 `SelectionCommand`（语义更清楚、不与未来其他动作冲突）。建议后者。
3. 是否只对用户操作触发（建议是）。

---

## P4 — Card 可发现性（A：S，随 P1；B：L，需设计决策）

**一句话：** 开发者在 IntelliSense 里打 `<ether:` 找不到 Card，因为它不是一个控件类型，而是套在 `Border` 上的六个样式 key。

**现状：** `src/Ether.DesignSystem.Controls/Resources/Foundations/EtherCard.xaml:95-149` 定义六个
`TargetType="Border"` 的 keyed Style（Normal/Intelligence/Callout 各一对 shell+body）；Callout 是**复合结构**，
只能靠粘贴示例（`:151-185`），Gallery 也是手工拼装，连头部箭头都是 Gallery 自带的 SVG
（`Views/Surfaces/CardPage.xaml:56-87`，SVG 在 `:68-77`）。审查结论把"不可实例化"记为 🚫 并给出替代
（`consumability/EtherCard.md:33, 44-50`）。getting-started 只演示了 Normal 一种（`:420-429`）。

### A 方案（推荐先做，S，并入 P1）
- getting-started §5 Card 条目补齐 **三种**组合的完整可粘贴 XAML（Intelligence、Callout 含头部行），
  头部箭头改用图标库的 `IconArrowRight`（`src/Ether.DesignSystem.Foundation/Resources/Tokens/EtherIconGeometries.xaml:91`），
  消费者不必自带 SVG。
- 在条目开头一句话说明"为什么是 Style 不是类型"，并列出六个 key 的用途表。
- Gallery `CardPage` 的 `SpecimenXaml` 同步为三段。

### B 方案（暂缓；L，设计决策）
把 Card 做成 `EtherCard : ContentControl`，`Variant`（`Normal|Intelligence|Callout`）+ `Header`/`HeaderTemplate`
（Callout 用）两三个 DP，默认样式放 `Controls/Surfaces/EtherCard.xaml` 并在 `Themes/Generic.xaml`（现 `:24`）合并；
六个 keyed Style **保留**（已是 shipped API，删除即破坏性变更）。

| 收益 | 代价 / 风险 |
| --- | --- |
| `<ether:EtherCard Variant="Callout" Header="Intelligence Insight">…</ether:EtherCard>` 一行可用，IntelliSense 可发现 | **没有官方 WinUI 骨架可抄**：WinUI Gallery 的卡片是 `Border`/`Grid`；Toolkit `SettingsCard` 派生自 `ButtonBase`（可点击），与"装饰性表面"语义不符 → 必须手写模板并按 `AGENTS.md:37-40` 写明理由 |
| Callout 复合结构收进库里，箭头/头部样式一处维护 | 新增第 16 个类型：`PublicPropertyInventoryTypes`、`DeclaredPropertyOwnerTypes`、PublicAPI、新 `Verify-EtherCardContract.ps1`（+`Gates.psd1`+`build.yml`）、`StyleResources.cs:13-30` 的六 key fixture要保留并新增类型 fixture、`EtherCard.md` 重写、`Verify-GalleryControlExample.ps1:29` 断言的 `EtherCardNormal` 片段要兼容 |
| 未来若设计出"可点击卡片"等变体，类型是唯一承载处 | 现无第二个消费者提出需求；`MinWidth=392` 等硬编码尺寸是留在样式还是变 DP 需要设计定夺 |

**建议：** 现在做 A；B 等到出现真实消费者诉求或设计新增 Style 表达不了的变体再启动。这是设计决策，由用户拍板。

---

## P5 — 收尾：packaged MSIX 运行时验证（M，依赖机器，需决策）

**一句话：** 唯一剩下的"残留项"其实不是"换台机器跑一下"就能关掉的——目前**没有任何脚本会安装并启动 MSIX**，需要先补这条路径。

**核实到的事实（纠正 `_RUN-ON-DESKTOP.md:59-64` 的表述）：**
- `scripts/Verify-ConsumerFixtures.ps1:862-874` 会**构建** Packaged 与 Unpackaged 两个 fixture；`:884-889` 对 Packaged 只断言
  输出文件存在；`:891-930` 的运行时 smoke **只启动 Unpackaged 的 exe**（`:895`）。
- `scripts/Verify-MsixPackage.ps1:28-48` 只产出**未签名** MSIX，并明确打印"not installed"。
- fixtures README 写明"installation and launch are deliberately out of ... scope"，且 Packaged fixture
  **退出了字体的 transitive 拷贝**（`tests/Ether.DesignSystem.ConsumerFixtures/README.md:10-14`）——即使跑起来，字体路径行为也与 Unpackaged 不同。
- 好消息：Packaged 的 `App.xaml.cs:21` 与 Unpackaged 一样响应 `ETHER_CONSUMER_SMOKE=1`，`RuntimeVerification` 代码是共享的；
  但结果标记路径靠环境变量传递（`RuntimeVerification.cs:571`、`Infrastructure.cs:279-282`），而 MSIX 应用经 shell 激活时**不继承**启动 shell 的环境变量。
- 发布阻断规范已把"干净机器装 MSIX→跑起来"判定为**可选后续、非本轮阻断**
  （`docs/plans/2026-08-31-release-blockers-spec.md:96, 643`；R-08 修正记录在 `:472-486`），包 README 与 release notes 也已诚实标注
  （`src/Ether.DesignSystem.Controls/README.md:11`；`docs/releases/0.1.0-preview.1.md:79`）。

**若决定做，步骤：**
1. **先修文案（S，可立即做）**：`_RUN-ON-DESKTOP.md:59-64` 改为"无脚本路径，需按下列步骤补"，避免下一个人被误导。
2. **安装方式（决策）**：(a) `Add-AppxPackage -Register <bin>\AppxManifest.xml` 松散部署（需 Developer Mode，免签名）；
   (b) 测试证书签名 + `Add-AppxPackage <msix>`；(c) `Add-AppxPackage -AllowUnsigned`（Win11 22H2+ 且 Developer Mode）。建议 (a)。
3. **在包上下文里带环境变量启动**：`Invoke-CommandInDesktopPackage -PackageFamilyName … -AppId … -Command …`，
   或把结果路径改为通过启动参数 / `ApplicationData.LocalFolder` 传递（需改 `RuntimeVerification.cs:571` 附近的读取逻辑，两种宿主共用）。
4. 新增 `scripts/Verify-PackagedRuntime.ps1`（或给 `Verify-ConsumerFixtures.ps1` 加 `-Packaged` 开关），登记为
   `Gates.psd1` 的 `local-runtime` 条目（`:77-95`），并通过 `Verify-GateManifest.ps1`。
5. 在 Developer Mode 机器上跑，标记写入 `artifacts/audit-runs/…`，更新 `_SUMMARY.md:195-202` 残留段、包 README:11、release notes:79。

**工作量：** M（脚本 + 标记传递改造约 1 天；运行本身取决于机器）。
**决策：** 是否把它列为本次 preview 发布前必做。按现有规范与"内部专用 / x64 / unpackaged 优先"的分发决策，建议**不列为阻断**，排在 P1–P3 之后。

---

## 6. 已核实"已完成或不值得做"的项（不再规划）

- **TwoWay 绑定、按钮族 Command、三种 ItemsSource 路径** —— 均已由 fixture 证明（`_SUMMARY.md:78-84, 98-103, 109-114`），只欠文档与示范（P1/P2），不需要产品改动。
- **Interactions 适配器** —— 已覆盖全部 15 个组件的可观察面（`_SUMMARY.md:115-122`），缺的只是用法示例（P1-B-6）。
- **Slider / SteeringBar / TabNavigation / Dropdown / Masthead / Switch / Input 加 `Command`** —— 评估后不加（P3 表），理由分别是"每 tick 触发语义错误"、"偏离原生 Selector/ToggleSwitch 契约"、"无类型可挂"。
- **Card 做成类型（P4-B）** —— 现阶段收益不足以抵消 L 级成本与"无官方骨架"的架构偏离，挂起。
- **在托管 CI 上跑 GUI/MSIX 安装** —— `HANDOFF.md:4` 与 `build.yml:3-7` 明确禁止；P5 只能在本地 local-runtime 门禁里做。

## 7. 决策清单（用户需拍板）

| # | 决策 | 关联项 | 建议 |
| --- | --- | --- | --- |
| D-1 | 消费者文档语言保持中文？ | P1 | 是 |
| D-2 | 示例是否推荐 `CommunityToolkit.Mvvm`？ | P1 | 主线手写 INPC，旁注 Toolkit 写法 |
| D-3 | 是否把 `Microsoft.Xaml.Interactivity` 加进 fixtures 做证明？ | P1 | 否，文档标注"消费者自选" |
| D-4 | Gallery 是否引入 MVVM 包？ | P2 | 否 |
| D-5 | SegmentedControl 加 `SelectionCommand`？命名？只对用户操作触发？ | P3 | 加；`SelectionCommand`；是 |
| D-6 | Card：A（文档补全）还是 B（真类型）？ | P4 | 先 A，B 挂起 |
| D-7 | packaged MSIX 运行时是否列为发布阻断？ | P5 | 否（与 R-08 决定一致），排在 P1–P3 后 |

## 8. 建议的执行波次

- **Wave 0（半天）：** P0 推送 + PR；根 README（P1-C）；`_RUN-ON-DESKTOP.md:59-64` 文案修正（P5-1）。
- **Wave 1（1–1.5 天）：** P1-A/B 文档重写 + P4-A Card 三段示例；跑 `Verify-UnsupportedProperties.ps1` 确认 §4.1 表未被破坏。
- **Wave 2（1 天）：** P2 Gallery 数据驱动块 ×3（+ ButtonPage Command 可选）；本地 `Verify-GallerySmoke.ps1`。
- **Wave 3（1 天，若 D-5 批准）：** P3 SegmentedControl `SelectionCommand` + fixture + PublicAPI；本地 `Verify-ConsumerFixtures.ps1`；回填 P1/P2 各一段。
- **Wave 4（按 D-7，1 天 + 机器）：** P5 packaged 运行时脚本与实跑。

每一波结束条件与 remediation plan 相同：改动 + 证据 + 门禁绿；未跑的部分明说"未跑"，不报绿。
