# EtherTabNavigation 重导航图标消失：根因与控件级修复

日期：2026-09-01  
目标版本：WindowsAppSDK 2.3.1 / Microsoft.WindowsAppSDK.WinUI 2.3.0 / .NET 8

## 根因判断

页面重建和 ListView 容器生命周期是稳定触发器；真正的资源所有权错误是：旧实现虽然每次创建新的 `PathIcon`，却仍从同一个资源 `Style` 取得并复用已经强制转换的 `Geometry` 对象。

WinUI 的 `Geometry` 继承 `DependencyObject`，但没有 WPF `Freezable` 的 `Clone/Freeze` 共享机制。[Microsoft 的 Geometry API](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.media.geometry)列出了其对象模型；microsoft-ui-xaml 的[共享 Geometry issue #827](https://github.com/microsoft/microsoft-ui-xaml/issues/827)则直接记录了同一 Geometry 被多个 PathIcon/Path 使用会失败，以及 WinUI 没有平台 clone API。ListViewBase 同时有明确的容器回收阶段，[`InRecycleQueue` 文档](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.containercontentchangingeventargs.inrecyclequeue)说明控件可在容器进入回收队列时释放相关引用。

据此，本 bug 的完整链路是：

1. `IconHome` 等 Style 的 Data Setter 在加载时把 `"F1 M..."` 强制转换成一个 Geometry 依赖对象，并随应用资源长期存在。
2. 第一批 ListViewItem/PathIcon 使用该 Geometry 后建立 WinUI 内部渲染关联。
3. 页面离开时 ListView 项和模板树被清理；应用级 Style/Geometry 仍然存在。
4. 页面重建时，新 PathIcon 再次得到同一个 Geometry。WinUI 不能安全共享这一对象，在当前版本中表现为槽位存在但 glyph 为空。

Button 没有触发相同失败，只说明它没有命中 ListView 的这组清理/重建时序，并不能使共享 Geometry 变成受支持用法。该结论中，“共享不受支持”有官方直接证据；“2.3.1 在这条时序中具体表现为空白”是结合稳定复现得出的版本相关判断，仍需运行时截图验证。

## 修复方案

`EtherTabItem.BuildIcon` 现在把 Setter.Value 按正确的运行时类型 `Geometry` 读取，并对整个对象图进行深拷贝：

- Geometry：PathGeometry、GeometryGroup、EllipseGeometry、LineGeometry、RectangleGeometry；
- Path：PathFigure、全部 WinUI PathSegment 派生类型、PointCollection；
- Transform：全部 WinUI 2.3 Transform 派生类型及 TransformGroup。

随后创建 `PathIcon { Data = clonedGeometry, Width = 14, Height = 14 }`。没有任何回退路径会再把资源 Style 或其 Geometry 本体交给 EtherTabItem 的视觉树。

这建立了一个与生命周期无关的不变量：每个已实现的 EtherTabItem 图标都独占 PathIcon 和完整 Geometry 对象图。页面重建会创建新图；容器复用时，Icon DP 变化会创建新图；模板重应用也会创建新图。容器钩子不再承担“修复共享对象”的职责，因此无需改动 ListView 的选择、键盘、焦点和自动化语义。

## 兼容性与验证

- `Icon="Home"` 字符串 API、14×14、8 EPX gap、前景色随选中状态反白全部保留。
- `EtherIconGeometries.xaml` 未改动；结构检查确认 430 个 Style 对应 430 个 Data Setter，且没有会因不应用 Style 而丢失的其它 Setter。
- 官方 ListView/ListViewItem 控制骨架、VisualState 名称和选择语义未改动。
- Gallery 的 `NavigationCacheMode.Required` workaround 已移除，恢复真实页面重建条件。
- Controls 指定构建：0 warning，0 error。
- Gallery 在隔离输出目录按相同 x64/Debug/restore/`-m:1` 参数构建：0 warning，0 error。默认输出目录当时被运行中的 Gallery 进程锁定，失败仅发生在复制 DLL 阶段。

## 仍需运行时验证

使用 UI Automation 执行：打开 Tab Navigation → 截图三枚图标 → 导航离开 → 返回 → 再截图，并至少重复两轮。还应切换三个 tab，确认 Home/Document/Clock 均存在且选中 pill 上前景色反白；这部分不能由静态构建替代。
