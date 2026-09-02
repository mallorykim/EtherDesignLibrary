# WinUI 3 Design Components: Backend-Listenable and Consumable Audit Framework

**Status:** Research record / audit baseline
**Date:** 2026-08-29
**Scope:** WinUI 3 components in `Ether.DesignSystem.*` that trigger or present business interactions, and the client applications and corresponding backends that use these components.

## Conclusion and boundaries

A backend cannot directly listen to a WinUI 3 control's local `Click`, dependency-property changes, or UI Automation events. These are only valid within the client process. For a backend to reliably consume user actions, the application must convert control interactions into versioned business commands or events and send them through a controlled transport channel.

```text
WinUI 3 control
  -> ViewModel ICommand / semantic event
  -> Client interaction adapter (validation, tracing, offline queue)
  -> API Gateway / Event Ingress
  -> Backend command handler
  -> Domain events, downstream consumers, audit and monitoring
```

Design-system components are responsible only for presentation, accessibility, and expressing user intent; business rules, authentication, networking, retries, and event delivery belong to the application layer or infrastructure layer.

## Audit goals

Every component interaction that changes business state must be traceable to:

1. A unique semantic command or event;
2. A versioned, verifiable transport contract;
3. A stable idempotency key and correlation ID;
4. A backend processing record and a queryable final result;
5. A user-facing success, failure, or pending-sync state.

## Component-layer audit checklist

| Audit domain | Admission requirement | Blocking issue |
| --- | --- | --- |
| Public API | Bindable state is exposed as a `DependencyProperty`; the data source supports `INotifyPropertyChanged` or an equivalent notification | Control state can only be read or modified in code-behind |
| User intent | Actions are exposed as an `ICommand` or a semantic event, e.g. `SubmitRequested`, `FilterApplied` | Only technical events such as `Click`, `PointerPressed` are exposed to the business layer |
| Component boundary | The control must not directly reference business APIs, auth tokens, or `HttpClient` | The design library internally makes HTTP requests, writes business audit records, or decides business retries |
| State machine | Clearly defines `Idle`, `Pending`, `Succeeded`, `Failed`, `OfflineQueued`, and prevents duplicate submission | Shows success before the request is confirmed; multiple submissions can be triggered in a row |
| UI thread | Background callbacks update the UI via `DispatcherQueue` | A background thread directly accesses a `DependencyObject` |
| Automation and accessibility | Has a stable `AutomationId`, keyboard reachability, and correct semantics; new controls verify the AutomationPeer | Key actions cannot be recognized by assistive technology or automated tests |

Dependency properties and change notifications are suited to data binding between a component and a ViewModel; commands are suited to unifying multiple input paths into one business action. Neither is a cross-process backend event protocol.

## Interaction contract audit checklist

Every consumable interaction must register the following in the contract repository:

| Field | Requirement |
| --- | --- |
| `type` | A stable, business-oriented name, e.g. `order.submit.requested`, containing no control or layout implementation details |
| `schemaVersion` | A monotonically increasing contract version; a breaking change adds a new version or a new event type |
| `eventId` | A client-generated unique message ID, used for delivery de-duplication |
| `idempotencyKey` | Stable for the same business operation, used by the backend to avoid duplicate writes |
| `correlationId` | Runs through client logs, requests, server-side processing, downstream events, and alerts |
| `occurredAtUtc` | UTC ISO 8601 timestamp |
| Identity context | Authenticated user, tenant, and authorization context; the server must still verify it and cannot trust the client's claim |
| `source` | A minimal app version, page, and component identifier, used for diagnostics; cannot substitute for business data |
| `data` | The minimal necessary business payload; tokens, passwords, and unnecessary personal data are prohibited |

Example:

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

## Backend consumption and reliability audit checklist

- The backend de-duplicates by `eventId` and the business idempotency key, and persists the processing status, result, and error reason.
- The client is designed for "at-least-once delivery": network errors, restarts, and timeouts can cause duplicate delivery; consumers must be idempotent.
- Pending interactions use a persistent outbox/offline queue; they must not be deleted before a successful server acknowledgment is received.
- Retries are used only for transient failures, and should employ backoff, jitter, timeouts, and a circuit breaker. Non-idempotent `POST`/write operations must not be retried unconditionally.
- If the UI still needs to be notified after backend processing, use a separate server-side notification contract, and define reconnection, ordering, snapshot, and replay rules; the backend must not directly manipulate controls.
- The server must re-perform authentication, authorization, tenant isolation, input validation, and business state validation.
- Audit logs are redacted, with defined retention periods and access control; sensitive data does not enter general-purpose telemetry.

## Acceptance evidence

The following evidence should be provided when an audit item is complete:

1. **Component inventory**: component properties, commands, semantic events, state machine, and AutomationId.
2. **Interaction mapping table**: `user action -> ViewModel command -> API/event -> backend handler -> UI acknowledgment state`.
3. **Contract repository contents**: JSON Schema, OpenAPI, or Protobuf; versioning policy, examples, error model, owners, and consumers.
4. **Contract compatibility tests**: producer and consumer Schema/consumer-driven contract tests.
5. **End-to-end tests**: normal submission, network-recovery, timeout, repeated clicks, duplicate delivery, server rejection, and compatibility between an old client and a new backend.
6. **Observability evidence**: using `correlationId`, the complete chain of one interaction — from the client to the final backend processing — can be queried.

> **2026-08-30 update**: this section records the state at the time of the 2026-08-29 audit.
> `EtherSegmentedTrack` was removed in commit `af909c93` ("refactor(controls): tighten the
> published surface before release") — it was never actually constructed by the library or the
> Gallery, only by this audit's acceptance code, and its name collided with a style key of the
> same name — bringing the number of audited types down from 13 to 12, and the property totals
> and categories down accordingly from `1,501 / 482 / 37 / 946` to the current
> `1,388 / visual 445 / semantic 69 / platform 874` (Ether's own DPs down from 37 to 35). The
> numbers below are a historical record and do not represent the current state; for the current
> authoritative numbers see `$expectedWritablePublicProperties` /
> `$expectedVisualPublicProperties` / `$expectedSemanticPublicProperties` /
> `$expectedPlatformPublicProperties` in `scripts/Verify-ConsumerFixtures.ps1` and §13 of
> `docs/handoff/2026-08-29-winui3-full-property-audit-handoff.md`.

## Full-property consumable acceptance boundary (2026-08-29, historical record, see the update note above)

"Full-property" in WinUI 3 cannot be understood as sending all layout and rendering properties of ancestor types such as `FrameworkElement` and `UIElement` to the backend; that would have no stable business meaning and would also carry UI implementation details and potentially sensitive content outside the client. This library adopts the following actionable boundary:

1. Every public dependency property **declared by the Ether control itself** must have a standard DP identifier and CLR wrapper, and complete `SetValue -> CLR getter / GetValue -> RegisterPropertyChangedCallback -> JSON envelope` acceptance in a pure NuGet consumer.
2. Every **business state** inherited from a WinUI control is accepted using its corresponding standard adapter: button action, `Text`, selection, `IsChecked`, `IsOn`, and `RangeBase.Value`. This preserves WinUI's native event and binding conventions.
3. Visual/layout configuration (e.g. icon, spacing, label collections) can be explicitly consumed via `ObserveProperty`, but is not sent as business telemetry by default. The adapter reduces native UI objects to JSON-safe values, avoiding cross-process serialization of UI object graphs.
4. Adding a new Ether dependency property without registering a runtime sample fails consumer acceptance; adding a new business state without choosing a standard adapter or an explicit `ObserveProperty` is also not admitted.

The current runtime baseline is restored from the packaged `Ether.DesignSystem.Controls` and `Ether.DesignSystem.Interactions` (not using project references), and enforces verification of:

- **1,501** Ether control public writable properties: CLR getter/read and setter calls one by one on the real types;
- **482** visual properties: changed, re-laid-out, and rendered one by one via `RenderTargetBitmap` on standalone specimens attached to a real WinUI visual tree; each one outputs a precise pixel, layout, visibility, component-DP, or platform-CLR visual-contract observation, and must not misreport "a single sample bitmap unchanged" as a pixel difference;
- **37** component-owned dependency properties: `SetValue`, CLR/`GetValue`, change callback, and JSON backend envelope;
- **12** standard WinUI interaction adapter chains, plus Light/Dark/OS High Contrast, LTR/RTL, UIA, text scaling, localization, and screenshot regression.

Inherited properties are still not sent as backend business events by default: visual properties' "renderable" acceptance and business properties' "consumable event" acceptance are two clear, independent gates. The compatibility subclass `EtherSegmentedTrack` has no independent template contract; its inherited properties render according to `EtherSegmentedControl`'s canonical template, while still completing public-property code calls against its own actual type.

Every record of a visual property must also fall into one precise method: `pixel-difference` (bitmap change), `layout-difference` (actual size/expected size/origin change), `visibility-transition`, `ether-component-dp-contract` (the component DP's `GetValue` and backend envelope contract), or `platform-clr-visual-contract` (a WinUI public CLR visual property, such as `BackgroundSizing`, whose API does not expose a `<Name>Property` field). This way, non-DP platform properties are not incorrectly rejected, and DP properties are not incorrectly downgraded to plain CLR verification.

Every passing consumer Smoke run retains that run's JSON result, the full-page Light/Dark/OS High Contrast PNGs, and the Light/Dark screenshot matrix for each of the 13 audited controls under `artifacts/audit-runs/consumer-runtime-evidence-<timestamp>/`; the next run cleans up the temporary package directory but does not clean up these audit artifacts.

## WinUI 3 official convention baseline

- Custom bindable properties use `public static readonly DependencyProperty <Name>Property` together with a same-named public CLR `GetValue/SetValue` wrapper.
- A control with a default template sets `DefaultStyleKey = typeof(ControlType)` in its constructor; `OnApplyTemplate` is overridden only when template parts need to be read, and `base.OnApplyTemplate()` is called first.
- Template parts and visual states are declared with `TemplatePart` / `TemplateVisualState`; theme colors use resources, and High Contrast uses system resources; non-instant transitions explicitly declare an easing function.
- Supporting classes used only for template implementation stay `internal` by default; if WinUI XAML metadata resolution requires `public`, the XML documentation must clearly state their template-support purpose and non-design-system-API status.

These conventions are automatically enforced by `scripts/Verify-WinUiConventions.ps1`, and are aligned with Microsoft's [Custom dependency properties](https://learn.microsoft.com/en-us/windows/apps/develop/platform/xaml/custom-dependency-properties), [WinUI 3 templated controls](https://learn.microsoft.com/en-us/windows/apps/winui/winui3/xaml-templated-controls-csharp-winui-3), and [Control templates](https://learn.microsoft.com/en-us/windows/apps/develop/platform/xaml/xaml-control-templates) documentation.

## WinUI 3 Gallery benchmarking gate (2026-08-29)

Microsoft's WinUI 3 Gallery is **the behavior, sample architecture, and accessibility benchmark for platform controls**, not a substitute for Ether's visual tokens. Therefore every Ether control must pass all three of the following layers simultaneously; failing any one layer means it cannot be marked as passing visual acceptance:

| Layer | Actionable requirement | Current automated evidence |
| --- | --- | --- |
| Platform behavior | Uses official DP, template, VisualState, UIA, keyboard, and RTL conventions | `Verify-WinUiConventions.ps1`, consumer runtime UIA/RTL verification |
| Gallery sample architecture | Every control has an independent sample, clear interaction states, AutomationProperties, and a readable XAML/code-behind example | Gallery control example and localization gate |
| Ether visual spec | LTR content order, logical order after RTL mirroring, spacing, theme, Disabled/Pressed/Hover, and high contrast are all visible and regressable | Light/Dark/High Contrast screenshots and state assertions from the NuGet consumer Smoke |

This round found and fixed a visual-order gap in the segmented control: `EtherSegmentPanel` now arranges its children according to `FlowDirection`; the consumer sample was changed to actually use this panel; the runtime asserts that the first logical item is on the left in LTR and on the right in RTL. The Smoke root node is fixed to LTR to avoid the demo window being implicitly mirrored by the ambient RTL; the runtime tests still explicitly switch to RTL and restore afterward.

When a control is added or modified going forward, three-state (LTR, RTL, High Contrast) screenshot evidence and the corresponding layout/order assertions must be added or updated. A full-page Smoke screenshot alone, without control-level assertions, must not be judged as completing visual benchmarking.

## Audit verdict

Any business-changing interaction that lacks a semantic command, a versioned contract, idempotent handling, correlation tracking, or a clear user acknowledgment should be judged **FAIL (blocks release)**. Complete visual styling or UI Automation does not make up for this gap; UI Automation primarily serves accessibility and UI automated testing.

## References

- [Microsoft Learn: Dependency properties overview](https://learn.microsoft.com/en-us/windows/apps/develop/platform/xaml/dependency-properties-overview)
- [Microsoft Learn: Events and routed events overview](https://learn.microsoft.com/en-us/windows/apps/develop/platform/xaml/events-and-routed-events-overview)
- [Microsoft Learn: Windows data binding in depth](https://learn.microsoft.com/en-us/windows/apps/develop/data-binding/data-binding-in-depth)
- [Microsoft Learn: Custom automation peers](https://learn.microsoft.com/en-us/windows/apps/design/accessibility/custom-automation-peers)
- [Microsoft WinUI 3 Gallery source](https://github.com/microsoft/WinUI-Gallery)
- [Microsoft WinUI Gallery keyboard accessibility samples](https://github.com/microsoft/WinUI-Gallery/blob/main/WinUIGallery/Samples/AccessibilityKeyboard/AccessibilityKeyboardPage.xaml)
- [Microsoft Learn: Event-driven architecture style](https://learn.microsoft.com/en-us/azure/architecture/guide/architecture-styles/event-driven)
- [Microsoft Learn: Build resilient HTTP apps](https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience)
- [Microsoft Learn: Transactional Outbox sample](https://learn.microsoft.com/en-us/samples/azure-samples/cosmos-db-design-patterns/transactional-outbox/)
