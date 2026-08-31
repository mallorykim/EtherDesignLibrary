# WinUI 3 设计组件：后端可监听与消费审核框架

**状态：** 研究记录 / 审核基线  
**日期：** 2026-08-29  
**适用范围：** `Ether.DesignSystem.*` 中会触发或呈现业务交互的 WinUI 3 组件，以及使用这些组件的客户端应用和对应后端。

## 结论与边界

后端不能直接监听 WinUI 3 控件的本地 `Click`、依赖属性变更或 UI Automation 事件。它们只在客户端进程内有效。要让后端可靠消费用户操作，应用必须把控件交互转换为版本化的业务命令或事件，并通过受控的传输通道发送。

```text
WinUI 3 控件
  -> ViewModel ICommand / 语义事件
  -> 客户端交互适配器（校验、追踪、离线队列）
  -> API Gateway / Event Ingress
  -> 后端命令处理器
  -> 领域事件、下游消费者、审计与监控
```

设计系统组件只负责表现、可访问性与表达用户意图；业务规则、鉴权、网络、重试及事件投递属于应用层或基础设施层。

## 审核目标

每一个会改变业务状态的组件交互都必须能追溯到：

1. 唯一的语义命令或事件；
2. 版本化、可验证的传输契约；
3. 稳定的幂等键和关联 ID；
4. 后端处理记录及可查询的最终结果；
5. 面向用户的成功、失败或待同步状态。

## 组件层审核清单

| 审核域 | 准入要求 | 阻断问题 |
| --- | --- | --- |
| 公共 API | 可绑定状态以 `DependencyProperty` 暴露；数据源支持 `INotifyPropertyChanged` 或等价通知 | 控件状态只能在 code-behind 中读取或修改 |
| 用户意图 | 以 `ICommand` 或语义事件暴露动作，例如 `SubmitRequested`、`FilterApplied` | 仅暴露 `Click`、`PointerPressed` 等技术事件给业务层 |
| 组件边界 | 控件不得直接引用业务 API、认证令牌或 `HttpClient` | 设计库内部发 HTTP 请求、写业务审计或决定业务重试 |
| 状态机 | 明确定义 `Idle`、`Pending`、`Succeeded`、`Failed`、`OfflineQueued`，并防止重复提交 | 请求尚未确认即显示成功；可连续触发多次提交 |
| UI 线程 | 后台回调通过 `DispatcherQueue` 更新 UI | 后台线程直接访问 `DependencyObject` |
| 自动化与无障碍 | 有稳定的 `AutomationId`、键盘可达性和正确语义；新控件验证 AutomationPeer | 关键操作无法被辅助技术或自动化测试识别 |

依赖属性与变更通知适合组件和 ViewModel 之间的数据绑定；命令适合把多个输入路径归一为一个业务动作。它们不是跨进程后端事件协议。

## 交互契约审核清单

每个可消费交互都须在契约仓库登记以下内容：

| 字段 | 要求 |
| --- | --- |
| `type` | 稳定、业务化的名称，例如 `order.submit.requested`，不包含控件或布局实现 |
| `schemaVersion` | 单调递增的契约版本；破坏性变化新增版本或新事件类型 |
| `eventId` | 客户端生成的唯一消息 ID，用于投递去重 |
| `idempotencyKey` | 对同一业务操作稳定，用于后端避免重复写入 |
| `correlationId` | 贯穿客户端日志、请求、服务端处理、下游事件与告警 |
| `occurredAtUtc` | UTC ISO 8601 时间戳 |
| 身份上下文 | 经认证的用户、租户与授权上下文；服务端仍须验证，不能信任客户端声明 |
| `source` | 最小化的应用版本、页面和组件标识，用于诊断，不能代替业务数据 |
| `data` | 最小必要业务载荷；禁止令牌、密码和不必要的个人数据 |

示例：

```json
{
  "eventId": "3d0ee025-c5bd-46fe-8694-ec6cab053f50",
  "type": "order.submit.requested",
  "schemaVersion": 1,
  "idempotencyKey": "f172502c-1df8-4cd6-848d-280de8d70a77",
  "occurredAtUtc": "2026-08-29T12:34:56Z",
  "correlationId": "trace-id",
  "tenantId": "tenant-123",
  "source": {
    "appVersion": "1.4.0",
    "screen": "OrderEdit",
    "component": "SubmitOrderButton"
  },
  "data": {
    "orderId": "order-456"
  }
}
```

## 后端消费与可靠性审核清单

- 后端按 `eventId` 和业务幂等键去重，并持久化处理状态、结果和错误原因。
- 客户端按“至少一次投递”设计：网络异常、重启和超时可能造成重复送达；消费者必须幂等。
- 待发送交互使用持久化 outbox/离线队列；收到服务端成功确认前不得删除。
- 重试仅用于瞬时故障，并应采用退避、抖动、超时与断路器。非幂等的 `POST`/写操作不能无条件重试。
- 如果后端处理后还需通知 UI，使用独立的服务端通知契约，并定义重连、顺序、快照与回放规则；不得直接操纵控件。
- 服务端必须重新执行认证、授权、租户隔离、输入校验和业务状态校验。
- 审计日志脱敏，定义保留期限和访问控制；敏感数据不进入通用遥测。

## 验收证据

审核项完成时应提供下列证据：

1. **组件清单**：组件属性、命令、语义事件、状态机和 AutomationId。
2. **交互映射表**：`用户动作 -> ViewModel 命令 -> API/事件 -> 后端处理器 -> UI 回执状态`。
3. **契约仓库内容**：JSON Schema、OpenAPI 或 Protobuf；版本策略、示例、错误模型、所有者和消费者。
4. **契约兼容性测试**：生产者与消费者的 Schema/消费者驱动契约测试。
5. **端到端测试**：正常提交、断网恢复、超时、重复点击、重复送达、服务端拒绝、旧客户端与新后端兼容。
6. **可观测性证据**：使用 `correlationId` 能查询一次交互从客户端到最终后端处理的完整链路。

> **2026-08-30 更新**：本节记录的是 2026-08-29 审核当时的状态。`EtherSegmentedTrack`
> 已于提交 `af909c93`（"refactor(controls): tighten the published surface before
> release"）删除（未被库或 Gallery 实际构造，只有本审核的验收代码构造它，且其名字与
> 同名样式键冲突），受审类型数由 13 降为 12，属性总数与分类随之从
> `1,501 / 482 / 37 / 946` 变为当前的 `1,388 / 视觉 445 / 语义 69 / 平台 874`（Ether 自
> 有 DP 由 37 降为 35）。以下数字为历史记录，不代表当前状态；当前权威数字见
> `scripts/Verify-ConsumerFixtures.ps1` 的 `$expectedWritablePublicProperties` /
> `$expectedVisualPublicProperties` / `$expectedSemanticPublicProperties` /
> `$expectedPlatformPublicProperties` 与 `docs/handoff/2026-08-29-winui3-full-property-audit-handoff.md` §13。

## 全属性可消费验收边界（2026-08-29，历史记录，见上方更新说明）

“全属性”在 WinUI 3 中不能理解为把 `FrameworkElement`、`UIElement` 等祖先类型的全部布局与渲染属性都发送到后端；那既没有稳定的业务含义，也会把 UI 实现细节和潜在敏感内容带出客户端。本库采用以下可执行的边界：

1. 每个 Ether 控件**自己声明**的公开依赖属性，都必须有标准的 DP 标识符和 CLR 包装器，并在纯 NuGet 消费者中完成 `SetValue -> CLR getter / GetValue -> RegisterPropertyChangedCallback -> JSON envelope` 验收。
2. 每个继承 WinUI 控件的**业务状态**都使用对应的标准适配器验收：按钮动作、`Text`、选择、`IsChecked`、`IsOn` 及 `RangeBase.Value`。这保留 WinUI 的原生事件与绑定约定。
3. 视觉/布局配置（例如图标、间距、标签集合）可通过 `ObserveProperty` 显式消费，但默认不作为业务遥测发送。适配器会将原生 UI 对象降为 JSON 安全值，避免跨进程序列化 UI 对象图。
4. 新增 Ether 依赖属性而未登记运行时样例会使消费者验收失败；新增业务状态而未选择标准适配器或显式 `ObserveProperty` 也不准入。

当前运行时基线从打包后的 `Ether.DesignSystem.Controls` 与 `Ether.DesignSystem.Interactions` 恢复（不使用项目引用），并强制验证：

- **1,501** 个 Ether 控件公开可写属性：在真实类型上逐一 CLR getter/read 与 setter 调用；
- **482** 个视觉属性：在附着到真实 WinUI 视觉树的独立标本上逐一变更、重新布局并通过 `RenderTargetBitmap` 渲染；每条都输出精确的像素、布局、可见性、组件 DP 或平台 CLR 视觉契约观测，不能把“单一样本位图未变”误报为像素差异；
- **37** 个组件自有依赖属性：`SetValue`、CLR/`GetValue`、变更回调和 JSON 后端 envelope；
- **12** 条标准 WinUI 交互适配器链，以及 Light/Dark/OS High Contrast、LTR/RTL、UIA、文本缩放、本地化和截图回归。

继承属性仍不默认发送为后端业务事件：视觉属性的“可渲染”验收与业务属性的“可消费事件”验收是两个明确、独立的门禁。兼容子类 `EtherSegmentedTrack` 没有独立模板契约，其继承属性按 `EtherSegmentedControl` 的规范模板渲染，同时仍会以自身实际类型完成公开属性代码调用。

视觉属性的每条记录还必须落入一种精确方法：`pixel-difference`（位图变化）、`layout-difference`（实际尺寸/期望尺寸/原点变化）、`visibility-transition`、`ether-component-dp-contract`（组件 DP 的 `GetValue` 与后端 envelope 契约），或 `platform-clr-visual-contract`（WinUI 公开 CLR 视觉属性，如 `BackgroundSizing`，其 API 并不公开 `<Name>Property` 字段）。因此非 DP 平台属性不会被错误拒绝，DP 属性也不会被错误降级为普通 CLR 验证。

每次通过的消费者 Smoke 会在 `artifacts/audit-runs/consumer-runtime-evidence-<timestamp>/` 保留当次 JSON 结果、整页 Light/Dark/OS High Contrast PNG，以及 13 个受审控件各自的 Light/Dark 截图矩阵；下次运行虽会清理临时包目录，但不会清理这些审核工件。

## WinUI 3 官方约定基线

- 自定义可绑定属性使用 `public static readonly DependencyProperty <Name>Property` 与同名 public CLR `GetValue/SetValue` 包装器。
- 有默认模板的控件在构造函数设置 `DefaultStyleKey = typeof(ControlType)`；只有需要读取模板部件时才重写 `OnApplyTemplate`，并先调用 `base.OnApplyTemplate()`。
- 模板部件与视觉状态以 `TemplatePart` / `TemplateVisualState` 声明；主题色使用资源，High Contrast 使用系统资源；非瞬时过渡显式声明 easing。
- 仅供模板实现的支持类默认保持 `internal`；如 WinUI XAML 元数据解析要求 `public`，必须在 XML 文档中明确其模板支持用途和非设计系统 API 地位。

这些约定由 `scripts/Verify-WinUiConventions.ps1` 自动执行，并与微软的 [自定义依赖属性](https://learn.microsoft.com/en-us/windows/apps/develop/platform/xaml/custom-dependency-properties)、[WinUI 3 模板控件](https://learn.microsoft.com/en-us/windows/apps/winui/winui3/xaml-templated-controls-csharp-winui-3) 和 [控件模板](https://learn.microsoft.com/en-us/windows/apps/develop/platform/xaml/xaml-control-templates) 文档对齐。

## WinUI 3 Gallery 对标门禁（2026-08-29）

Microsoft 的 WinUI 3 Gallery 是**平台控件的行为、样本架构与可访问性基准**，不是 Ether 视觉 token 的替代品。因此每个 Ether 控件须同时通过以下三层，任一层失败均不能标记为视觉验收通过：

| 层级 | 可执行要求 | 当前自动化证据 |
| --- | --- | --- |
| 平台行为 | 使用官方 DP、模板、VisualState、UIA、键盘和 RTL 约定 | `Verify-WinUiConventions.ps1`、消费者运行时 UIA/RTL 验证 |
| Gallery 样本架构 | 每个控件都有独立样本、明确的交互状态、AutomationProperties 和可读的 XAML/code-behind 示例 | Gallery 控件示例与本地化门禁 |
| Ether 视觉规范 | LTR 内容顺序、RTL 镜像后的逻辑顺序、间距、主题、Disabled/Pressed/Hover 和高对比度均可见且可回归 | NuGet 消费者 Smoke 的 Light/Dark/High Contrast 截图和状态断言 |

本轮发现并修复了分段控件的视觉顺序缺口：`EtherSegmentPanel` 现在按 `FlowDirection` 安排子项；消费者样本改为实际使用该面板；运行时断言 LTR 中第一个逻辑项在左、RTL 中第一个逻辑项在右。Smoke 根节点固定为 LTR，避免演示窗口被环境 RTL 隐式镜像；运行时测试仍会显式切换到 RTL 并恢复。

后续新增或修改控件时，必须新增或更新三态（LTR、RTL、High Contrast）截图证据以及对应的布局/顺序断言。仅有全页 Smoke 截图而无控件级断言，不得判定为视觉对标完成。

## 审核判定

任何一项业务变更交互若缺少语义命令、版本化契约、幂等处理、关联追踪或明确用户回执，应判定为 **不通过（阻断发布）**。视觉样式或 UI Automation 完整并不能弥补该缺口；UI Automation 主要服务于无障碍和 UI 自动化测试。

## 参考资料

- [Microsoft Learn：Dependency properties overview](https://learn.microsoft.com/en-us/windows/apps/develop/platform/xaml/dependency-properties-overview)
- [Microsoft Learn：Events and routed events overview](https://learn.microsoft.com/en-us/windows/apps/develop/platform/xaml/events-and-routed-events-overview)
- [Microsoft Learn：Windows data binding in depth](https://learn.microsoft.com/en-us/windows/apps/develop/data-binding/data-binding-in-depth)
- [Microsoft Learn：Custom automation peers](https://learn.microsoft.com/en-us/windows/apps/design/accessibility/custom-automation-peers)
- [Microsoft WinUI 3 Gallery source](https://github.com/microsoft/WinUI-Gallery)
- [Microsoft WinUI Gallery keyboard accessibility samples](https://github.com/microsoft/WinUI-Gallery/blob/main/WinUIGallery/Samples/AccessibilityKeyboard/AccessibilityKeyboardPage.xaml)
- [Microsoft Learn：Event-driven architecture style](https://learn.microsoft.com/en-us/azure/architecture/guide/architecture-styles/event-driven)
- [Microsoft Learn：Build resilient HTTP apps](https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience)
- [Microsoft Learn：Transactional Outbox sample](https://learn.microsoft.com/en-us/samples/azure-samples/cosmos-db-design-patterns/transactional-outbox/)
