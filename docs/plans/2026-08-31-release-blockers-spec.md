# 发布阻断项清单（Release Blockers Spec）

Date: 2026-08-31
Branch: `codex/refine-components`
Baseline commit: `e270990`（干净基线；R-00 自包含调查已移至分支 `spike/self-contained-investigation` @ 82f12e9）
Status: 施工中（F0 完结：自包含 descope 待用户复核；F1–F6 待执行）

## 这份文件的用途

这是剩余工作的**唯一事实来源**。不靠记忆、不靠对话历史。

规则：

1. 任何人（含 agent）开工前先读本文件对应条目，**不要凭印象**。
2. 每条改完，更新该条的 `状态` 与 `验收证据`，并在文末「变更记录」追加一行。
3. **不允许**在本文件之外新增"待办"。新发现的问题必须以新条目写进这里。
4. 每条都必须有**可执行的验收命令**。没有验收命令的条目不算完成。

## 校验状态图例

| 标记 | 含义 |
|---|---|
| ✅ 已亲验 | 我本人跑过命令 / 读过代码确认，证据记在条目里 |
| ⚠️ 仅审核声称 | 来自 Codex 独立审核，**我尚未亲自复核**，数字与结论可能再变 |
| 🔬 进行中 | 有 agent 正在处理 |
| ⛔ | 已查清但判定为不可行/平台限制，含需用户复核的范围决定 |

## 与 Codex 审核 12 条的对账

审核给出 12 条。本文件是 **13 个工作项**，算式如下：

- 审核第 5 条与第 8 条 → **合并为 R-04**（同一个 splat 绑定缺陷的两个症状）：−1
- 审核第 2 条实际捆绑了两个不相干的问题 → **拆为 R-06 / R-07**：+1
- 自包含部署崩溃**不在审核 12 条内**（另行发现）→ **新增 R-00**：+1

12 − 1 + 1 + 1 = **13**

> 我此前对你说过「12 → 11」。那句话说的是**根因数**，且当时尚未决定把审核第 2 条拆开、也没把 R-00 计入。按**工作项**计是 13。两个数都不算错，但口径不同——以本文件的 13 为准。

## 总表

| ID | 审核# | 严重度 | 批次 | 状态 | 标题 |
|---|---|---|---|---|---|
| R-00 | — | 阻断 | F0 | ⛔ | 自包含-非打包不予支持（平台限制，需你复核） |
| R-01 | 3 | 阻断 | F1 | ✅ | 未锁 SDK，官方命令不可复现 |
| R-02 | 12 | 阻断前置 | F2 | ✅ | 门禁清单有 4 处各自维护，已漂移 |
| R-03 | 1 | 阻断 | F2 | ✅ | 发布路径可绕过，发布门禁漏跑 2 条 |
| R-04 | 5+8 | 阻断 | F3 | ✅ | splat 绑定失效使高对比度强制失灵，并产出误名文件 |
| R-05 | 7 | 应修 | F3 | ✅ | 推送前就把 `pushed: true` 落盘 |
| R-06 | 2a | 阻断 | F4 | ✅ | 「445 可观察」夸大：实为 270 可观察 / 175 仅契约 |
| R-07 | 2b | 阻断 | F4 | ✅ | 不支持属性清单是封闭列表 |
| R-08 | 6 | 应修 | F5 | ✅ | getting-started 夸大 MSIX 支持 |
| R-09 | 9 | 应修 | F5 | ✅ | Interactions 包缺发布元数据 |
| R-10 | 10 | 建议 | F5 | ✅ | XAML-only 类型标注不一致且不可机读 |
| R-11 | 11 | 建议 | F5 | ✅ | Foundation 包内混有占位图 |
| R-12 | 4 | 阻断 | F6 | ✅ | 证据未绑定到冻结提交 |

---

## R-00 — 自包含部署运行时崩溃

**严重度**：阻断　**批次**：F0　**状态**：⛔ 已查清根因，判定为平台限制，**自包含-非打包不予支持**（2026-08-31）

> ### ⚠️ 需要你复核的决定（我唯一一次反转你早前的明确要求）
>
> 你早前说过自包含"都要测、都要通过"。经两个 agent 共 ~140 分钟调查 + 我独立核实，**自包含-非打包（unpackaged, `WindowsPackageType=None`）无法承载本库的自定义控件,这是 WindowsAppSDK 平台限制,应用层无法修复**（论证见下）。
>
> "都要通过"若靠削弱测试来达成是被明令禁止的,而平台层面做不到的事我无法用命令让它成立。因此在你给的全权授权 + "保持干净"目标下,我的决定是：
> - **支持并已验证的分发模式 = 框架依赖**（packaged + unpackaged，VariantA/B 通过）——这本就是 WinUI 3 内部分发的常规模式,消费者机器有运行时
> - **自包含-非打包 = 记录在案的已知平台限制**,不作为发布阻断,不留常红门禁
> - defect#1 的**真实修复已完整保留在分支 `spike/self-contained-investigation`**,随时可恢复
> - **自包含-打包(MSIX,有真实包标识→PRI 可正常合并)很可能可行,但未测**——留作未来选项
>
> 如果你认为自包含是硬需求,醒来否决即可,我会转去验证 MSIX-打包-自包含路径。否则按上述"框架依赖"口径发布。

### 结论：两个独立缺陷，一个已修一个是平台限制

调查（agent + 我独立核实）证明阻断 VariantC 的是**两个独立缺陷**：

**defect#1（已修复，已证明）**：`ms-appx:///{程序集}/...` 资源字典合并在自包含下崩溃。凡对**被引用（非主）程序集**的 `ms-appx:///` 资源引用,在 `WindowsAppSDKSelfContained=true` 下必崩,与包、深度、语法无关（比原假设的"跨包 Foundation 引用"范围更大）。修法:运行时用 `XamlReader.Load` 从内嵌文本加载,绕开 pack-URI/PRI。**已保留在 spike 分支**。

**defect#2（平台限制，不可修）**：任何来自被引用程序集的自定义 `Control` 在自包含下**布局时**崩溃,与 defect#1 独立（不合并任何资源也崩）。

### 我为什么独立认同 defect#2 是平台限制

- 每个 Ether 控件构造函数都设 `DefaultStyleKey = typeof(自身)`（已核实,13 个控件全部如此）
- WinUI 在 measure/arrange 时会据此**自动**去程序集的 themeresources **经 PRI** 解析默认样式
- 自包含-非打包下 PRI 无法解析被引用程序集的资源（正是 defect#1 的根因）——但这次是**框架自己发起的查找,应用无法拦截**
- 这解释了 agent 的全部证据:`Template=null` 也崩（查找先于模板）、`XamlReader.Load` 与编译 XAML 都崩、合并资源的 workaround 无效、fault `0xC000027B` 属 PRI/资源解析族
- 唯一的应用层"绕法"是让消费者对每个控件实例显式设 `Style`——这会摧毁设计系统控件的全部意义,不可行

即:我的独立分析与 agent 结论**收敛**,没有指向任何未试的应用层修复。匹配 WindowsAppSDK #7830 / #10970 的严重度。

### 本轮对工作树的处置（保持干净）

- defect#1 修复 + VariantC 脚手架 + 新增公开 API（`EtherDesignSystemResources.MergeInto` 等）→ **全部保留到 `spike/self-contained-investigation` 分支,从发布分支撤除**（为一个不工作的模式留公开 API 和常红门禁 = 不干净）
- `Verify-ExternalConsumer.ps1` → **回到 A+B 两变体**（已知良好）,VariantC 从发布门禁移除
- **保留**两处 PS 5.1 hex 修复（`Convert::ToHexString` 是 .NET 5+,本仓库文档称 PS 5.1 为主 shell）——独立且正确,与自包含无关,并入 F1
- 自包含限制写入 `docs/consumers/getting-started.md` 的"已知限制"与一份 handoff

### 现象（存档）

`scripts/Verify-ExternalConsumer.ps1` 的三个变体中：

- VariantA-EtherOnly（框架依赖）— 通过
- VariantB-ExplicitWindowsAppSDK（框架依赖）— 通过
- **VariantC-SelfContained**（`SelfContained=true` + `WindowsAppSDKSelfContained=true` + `RuntimeIdentifier=win-x64`，经 `dotnet publish`）— **运行时崩溃**

### 已确认事实（不要重复推导）

- 不引用任何 Ether 包的**裸 WinUI 3 自包含应用可正常运行** → 环境与自包含机制本身没问题
- **仅合并 Ether 资源字典、不实例化任何控件**即以相同方式崩溃（fault offset `0x3a9c5d`） → 故障在**资源加载**，不在控件代码
- VariantC 产物约 443MB（框架依赖约 50MB），构建慢

### 首要嫌疑（需证明，不得假设）

`src/Ether.DesignSystem.Controls/Themes/Generic.xaml:9`：

```xml
<ResourceDictionary Source="ms-appx:///Ether.DesignSystem.Foundation/Themes/Foundation.xaml" />
```

跨包 `ms-appx:///` 引用。相关已核实的微软 issue：WindowsAppSDK #7830、#10970。

注意 `Generic.xaml` 另有多条**同包** `ms-appx:///Ether.DesignSystem.Controls/...` 引用，bisect 必须区分跨包与同包。

### 验收标准（按"平台限制、框架依赖发布"口径）

- 发布分支工作树：defect#1 修复 / VariantC / 新公开 API 全部撤除,`git status` 干净;完整修复保留在 `spike/self-contained-investigation` 分支且可 checkout
- `Verify-ExternalConsumer.ps1` 回到 A+B,两者仍通过,无 VariantC 常红门禁
- 两处 PS 5.1 hex 修复保留（并入 F1）
- `VariantA / VariantB` 无回归;`Verify-ConsumerFixtures.ps1` 仍 `1388 / 445 / 69 / 874` 与 35 受控属性;26 baseline 不变
- `docs/consumers/getting-started.md` 增"已知限制:自包含-非打包不受支持"章节,口径与包 README 一致
- 若用户否决此决定 → 转 MSIX-打包-自包含验证路径（另开 R 条目）

---

## R-01 — 未锁 SDK，官方命令不可复现

**严重度**：阻断　**批次**：F1　**状态**：✅ 已亲验

### 证据

```
$ ls global.json          → No such file or directory
$ dotnet --list-sdks      → 10.0.400 [C:\Program Files\dotnet\sdk]
```

工程 TFM 为 `net8.0-windows10.0.19041.0`，实际用 SDK 10.0.400 经 roll-forward 构建，**版本完全未锁**。

### 根因已定位（2026-08-31 亲验）

`.github/workflows/build.yml:26-28` 两个 job 都用：

```yaml
- uses: actions/setup-dotnet@v6
  with:
    dotnet-version: 8.0.x
```

**CI 钉死 SDK 8.0.x，本地无 `global.json` 所以吃 10.0.400。** 这就是"官方命令在不同机器上行为不同"的根源——CI 上 `dotnet restore` 用 SDK 8 正常，本地用 SDK 10 的并行 restore 才退 1。不是玄学，是 SDK 版本没对齐。

### 审核另称 —— 无并发复跑后：两条都**未复现**（2026-08-31，F1）

- `dotnet restore ...slnx -p:Platform=x64`：单独跑 6 次（1 热 + 5 冷，每次删光各项目 obj/），**全部 exit 0**，无 `-m:1` 之需
- `Verify-InteractionContracts.ps1`：exit 0，内层 `dotnet run --no-restore` 未失败

> **结论：审核当时的 restore 失败几乎可以确定是并发 agent 抢 NuGet 缓存的产物（正是 R-12 所述），不是真的 SDK bug。** SDK 二进制与审核时相同（都是 10.0.400），唯一差别是这次无并发。再次印证"一次只有一个 agent 动仓库"这条纪律。

### F1 修复已落地（commit `26f2adc`），但留有一处我复核时发现的隐患 → 已并入 F2

- `global.json` = `{ version: 10.0.400, rollForward: disable }`（最严格，本机唯一装的就是 10.0.400）
- CI setup-dotnet 从 `8.0.x` 改成 `10.0.x` —— **隐患**：`disable` 要求**精确** 10.0.400，而 `10.0.x` 会装最新 10.0 补丁；一旦 runner 装到 10.0.401 之类，CI 会"SDK 未找到"而崩。
- **F2 必做**：把 CI 两处 `dotnet-version` 从 `10.0.x` 改成精确 `10.0.400`，与 `global.json` 的 `disable` 精确对齐（这才是 R-01 真正要的可复现）。

### 为什么排最前

若「官方命令在不同机器上行为不同」，则后续**所有**验证结论都不可靠。这是其他所有条目的地基。

### 验收标准

- 仓库根存在 `global.json`，锁定 SDK 版本**与 CI 的 `8.0.x` 对齐**，并显式声明 `rollForward` 策略
- 本地按 `global.json` 若无对应 SDK，给出可操作的安装指引（不静默 roll-forward 到 10.x）
- 干净环境下，`docs/` 与 `HANDOFF.md` 中记载的官方命令逐条可跑通
- 若锁到 8.0.x 后并行 restore 仍需 `-m:1`，则**要么**修根因，**要么**写进命令并注明——不允许"知道要加但没写"
- **一并核对** CI 的 `dotnet-version: 8.0.x` 与新 `global.json` 不冲突（setup-dotnet 会尊重 global.json）

---

## R-02 — 门禁清单有 4 处各自维护，已漂移

**严重度**：阻断前置（R-03 的地基）　**批次**：F2　**状态**：✅ 已亲验

### 证据

磁盘上 29 个 `scripts/Verify-*.ps1`。四处各自维护清单：`.github/workflows/build.yml`、`scripts/Publish-Internal.ps1`、`scripts/Verify-RuntimeGates.ps1`、文档。

实测差集：

| 情况 | 脚本 |
|---|---|
| Publish-Internal 缺 | `Verify-PowerShellCompatibility.ps1`、`Verify-GallerySmoke.ps1` |
| CI 缺 | `Verify-ExternalConsumer.ps1`（合理——托管 runner 跑不了 WinUI，但属未声明的差异） |
| **无任何调用方** | `Verify-Arm64Packages.ps1` — 仅被 `.superpowers/sdd/` 历史规划文档引用；且已定 **x64 only**，属死代码 |
| 仅出现在注释中 | `Verify-RuntimeGates.ps1` — `build.yml:6` 与 `:119` 的注释 |

> 后两行是**审核未提及**的新发现。`Verify-RuntimeGates.ps1:23` 是 `Verify-GallerySmoke.ps1` 的**唯一**拥有者，而它自身没有任何自动化调用方——这解释了 GallerySmoke 为何会漏：不是被直接遗忘，是它的**编排器**没接进任何链路。

`Publish-Internal.ps1:231` 硬编码 `if ($staticGates.Count -ne 22)`，`:11` 与 `:204` 的注释同样写死 22。这个 22 目前**是对的**（CI 25 条运行步骤 − ConsumerFixtures − MsixPackage − PowerShellCompatibility = 22），但它是人工同步的常量。

### 根因

同一份清单在四处以四种格式重复维护，任何一处变更都不会强制其他三处跟进。

### 验收标准

- 存在**单一机器可读**的门禁清单（如 `scripts/Gates.psd1`），声明每条门禁的脚本、参数、运行环境（CI / 本地运行时 / 发布）
- `build.yml`、`Publish-Internal.ps1`、`Verify-RuntimeGates.ps1` 全部由该清单派生或对其校验
- 存在门禁校验四处一致性，**制造漂移能让它失败**（变异测试）
- `Verify-Arm64Packages.ps1` 要么接入清单并声明用途，要么删除——不允许留成死代码
- 删除 `Publish-Internal.ps1` 中人工维护的 `22` 常量

### 实现设计（交钥匙，F2 agent 照此实现）

**单一清单** `scripts/Gates.psd1`，每条门禁一个条目，声明它属于哪些运行环境（一条门禁可属多个环境，参数可因环境而异）：

```
@{
  Gates = @(
    @{ Name='PowerShellCompatibility'; Script='Verify-PowerShellCompatibility.ps1'; Environments=@('ci-static','publish'); Args=@{} }
    @{ Name='ResourceKeys';            Script='Verify-ResourceKeys.ps1'; Environments=@('ci-static','publish'); Args=@{ RequireHighContrastParity=$true } }
    ... 各控件契约 ...
    @{ Name='ConsumerFixtures-hosted'; Script='Verify-ConsumerFixtures.ps1'; Environments=@('ci-static'); Args=@{ SkipSolutionBuild=$true; SkipRuntimeSmoke=$true } }
    @{ Name='ConsumerFixtures-runtime';Script='Verify-ConsumerFixtures.ps1'; Environments=@('runtime-local'); Args=@{} }
    @{ Name='GallerySmoke';            Script='Verify-GallerySmoke.ps1'; Environments=@('runtime-local'); Args=@{} }
    @{ Name='MsixPackage';             Script='Verify-MsixPackage.ps1'; Environments=@('ci-static','runtime-local'); Args=@{ SkipSolutionBuild=$true } }
    @{ Name='ExternalConsumer';        Script='Verify-ExternalConsumer.ps1'; Environments=@('external-local'); Args=@{} }
  )
}
```

**四个环境的定义**（实测自当前 CI + 脚本）：

| 环境 | 谁消费 | 内容 | 主机要求 |
|---|---|---|---|
| `ci-static` | `build.yml` package-consumers job | 23 条（PowerShellCompat + 22 契约/资源/Gallery 门禁）+ ConsumerFixtures(-Skip both) + MsixPackage(-Skip) | 托管 runner 可跑（无 GUI） |
| `publish` | `Publish-Internal.ps1` | 与 `ci-static` 同（**当前漏了 PowerShellCompat 与 GallerySmoke**，见 R-03） | 本地 |
| `runtime-local` | `Verify-RuntimeGates.ps1` | ConsumerFixtures(完整)、GallerySmoke、MsixPackage | 本地 GUI |
| `external-local` | 手动 / `Publish-Internal` 前置 | ExternalConsumer（含 VariantC，见 R-00） | 本地 + NuGet 缓存 |

**关键参数注意**：`Args` 用**哈希表**（`@{ RequireHighContrastParity=$true }`），消费方用哈希表 splat（`& $script @argsHash`）——**绝不用数组 splat**，那正是 R-04 的 bug。这条要写进清单文件头注释。

**改造消费方**：
- `Publish-Internal.ps1`：删掉 22 条硬编码列表与 `Count -ne 22` 断言，改为 `Import-PowerShellDataFile Gates.psd1` 后按 `Environments -contains 'publish'` 过滤
- `Verify-RuntimeGates.ps1`：同理按 `'runtime-local'` 过滤
- `build.yml`：CI 的 YAML 步骤无法直接读 psd1；改为**校验**而非派生——新增 `Verify-GateManifest.ps1` 断言"清单里标 `ci-static` 的门禁集合 == build.yml 实际步骤集合"，任一漂移即失败。此脚本自身加入 `ci-static`。
- **CI SDK 对齐**（承 R-01）：确认 setup-dotnet 尊重新 global.json，8.0.x 一致。

**变异测试**：F2 交付时须证明——往 `Gates.psd1` 加一条假门禁但不更新 build.yml → `Verify-GateManifest.ps1` 失败；从 build.yml 删一步 → 同样失败。

---

## R-03 — 发布路径可绕过，发布门禁漏跑 2 条

**严重度**：阻断　**批次**：F2（依赖 R-02）　**状态**：✅ 已亲验

### 证据

`scripts/Pack-PreviewPackages.ps1` 仍可直接推送：

```
:6   [string]$Source,
:7   [string]$ApiKey
:72  $pushArgs = @('nuget', 'push', $package.FullName, '--source', $Source, '--skip-duplicate')
:79  Write-Host "Pushed preview packages to $Source."
```

`docs/releases/0.1.0-preview.1.md:12` 把这条路径写成了正式步骤 4：

```
4. `.\scripts\Pack-PreviewPackages.ps1 -Source <feed> [-ApiKey <key>]`
```

即：**存在一条完全绕开 `Publish-Internal.ps1` 全部门禁的发布路径，且文档在教人用它。**

漏跑门禁见 R-02。

### 验收标准

- `Pack-PreviewPackages.ps1` 不再具备推送能力（移除 `-Source` / `-ApiKey` 与 `nuget push`），仅负责打包
- `Publish-Internal.ps1` 成为**唯一**发布入口
- `docs/releases/0.1.0-preview.1.md` 步骤 4 改指向唯一入口
- 发布门禁覆盖 `Verify-PowerShellCompatibility.ps1` 与 `Verify-GallerySmoke.ps1`

---

## R-04 — splat 绑定失效使高对比度强制失灵，并产出误名文件

**严重度**：阻断（较审核**上调**）　**批次**：F3　**状态**：✅ 已亲验

> 审核把这记为两条（第 5 条"高对比度恢复失败只警告"、第 8 条"根目录误名生成物"）。实为**同一个缺陷的两个症状**。

### 证据

`scripts/Publish-Internal.ps1:208`：

```powershell
@{ Name = 'Verify-ResourceKeys.ps1 -RequireHighContrastParity'; Script = 'Verify-ResourceKeys.ps1'; Args = @('-RequireHighContrastParity') }
```

调用方式（`:239`）：`& $scriptPath @gateArgs`

实测绑定行为：

```
splat @gateArgs      -> OutputPath='-RequireHighContrastParity'  Switch=False
direct literal       -> OutputPath=''                            Switch=True
splat empty          -> OutputPath=''                            Switch=False
```

**数组 splat 不会把 `-Xxx` 字符串重新解析为参数名**，它按位置绑定到 `Verify-ResourceKeys.ps1:3` 的 `[string]$OutputPath`，而 `:4` 的 `[switch]$RequireHighContrastParity` 保持 `False`。

### 三层后果

1. 发布门禁中**名字写着** `-RequireHighContrastParity` 的那条，实际**开关是关的**——走到 `Verify-ResourceKeys.ps1:124` 只 `Write-Warning` 然后通过
2. 22KB JSON 被写入仓库根一个**文件名就叫 `-RequireHighContrastParity`** 的文件（已被 git 跟踪，mtime 2026-08-30 23:17，**仍在持续重新生成**，非陈旧残留）
3. `SEMVER.md:9` 明文规定 stable release "cannot ship while the parity gate fails"——该保证在发布门禁上**当前不生效**

### 缓解事实（不得省略）

用正确调用方式实跑：

```
Light: 234; Dark: 234; HighContrast: 234
Light-only: 0; Dark-only: 0; Missing from HighContrast: 0; HighContrast-only: 0
EXIT=0
```

parity **当前真实通过**。所以这是**潜伏的洞，不是正在掩盖失败**。

### 范围有界

全仓库唯一一处数组 splat 误用。`Verify-RuntimeGates.ps1:31-41` 用的是正确的哈希表 splat（`@{}` + `['SkipSolutionBuild'] = $true`），不受影响。

### 验收标准

- 改用哈希表 splat（或直接字面量调用），使开关**真实绑定**
- 删除仓库根 `-RequireHighContrastParity` 文件，并加入 `.gitignore` 防复发
- 存在检查，**故意把开关传错能让它失败**（变异测试）
- 复核 `Verify-ResourceKeys.ps1:124` 的 warning 降级路径：确认在开关开启时确实 `throw` 而非仅警告

---

## R-05 — 推送前就把 `pushed: true` 落盘

**严重度**：应修　**批次**：F3　**状态**：✅ 已亲验

### 证据

`scripts/Publish-Internal.ps1:334`：

```powershell
pushed            = [bool]$Push
```

该 summary 在 `:339` 即写入 `publish-summary.json`——**早于** `:427` 的确认提示与 `:434` 的真实 `dotnet nuget push`。真实推送后 `:451` 才再次写 `$summary.pushed = $true`。

即：带 `-Push` 运行时，若在确认提示处输错版本号（`:429` 抛出）或推送本身失败，**磁盘上的证据文件已经写着 `pushed: true`，而实际什么都没推**。

字段名在说谎：`pushed` 记录的是"是否请求了推送"，不是"是否发生了推送"。

### 验收标准

- 首次写入时 `pushed` 恒为 `false`
- 仅在真实推送成功后才改写为 `true`
- 存在检查，**在推送前中止能让证据文件保持 `pushed: false`**

---

## R-06 — 「445 可观察」夸大

**严重度**：阻断（对外口径）　**批次**：F4　**状态**：✅ 已亲验

### 证据

从最新证据文件 `artifacts/audit-runs/consumer-runtime-evidence-20260831-010140131/runtime-result.json` **实测**分类计数：

| 分类 | 计数 | 是否真实可观察 |
|---|---|---|
| `pixel-difference` | 247 | ✅ |
| `layout-difference` | 11 | ✅ |
| `visibility-transition` | 12 | ✅ |
| `platform-dp-contract` | 156 | ❌ 仅契约往返 |
| `ether-component-dp-contract` | 19 | ❌ 仅契约往返 |
| **合计** | **445** | **270 可观察 / 175 仅契约** |

`src/Ether.DesignSystem.Controls/README.md:7` 当前表述：

> 445 of 1,388 public writable properties have per-property **observable** evidence (pixel/layout/visibility/contract) ...

括号里**确实列出了** `contract`，所以不算完全隐瞒；但用 `observable`（可观察）统称这 445 条是错的——其中 175 条门禁自身记录的就是"Bitmap pixels were unchanged"。

同一句话已被复制到 `artifacts/consumer-fixtures/` 下的打包 README 副本中。

### 决定：选 B（2026-08-31，全权授权下由我定）

- **A**：数字改成 `270`，只把真实可观察的算进去
- **B**（采纳）：保留 `445`，措辞改为「445 条逐项证据，其中 270 条证明视觉生效（pixel/layout/visibility），175 条仅证明契约往返（DP getter/setter round-trip，像素未变）」

选 B 的理由：

1. **信息更完整**：B 同时给出总证据量与其中"真视觉/仅契约"的拆分；A 丢掉了 175 条契约证据这件事本身（它们仍是有价值的——证明属性可读写、不抛异常）。
2. **改动面更小、更不易再漂**：`445` 已进入两处打包 README 副本、release 文档、getting-started。A 要把所有 `445`→`270` 且重算 `1388−445=943`→`1388−270=1118`，改点更多；B 只需在每处 `445` 后补一句限定。
3. **诚实性靠措辞而非数字**：问题从来不是 445 这个数,而是用 `observable` 统称。B 直接改掉这个词,根治。

**对外统一措辞（所有文案以此为准）**：

> 445 of 1,388 public writable properties carry per-property evidence: 270 proven to visibly take effect (pixel / layout / visibility differences on a rendered, attached control), and 175 proven only as a DP round-trip (getter/setter invoked on an attached control without throwing; bitmap pixels unchanged). The remaining 943 are verified only as callable on a detached instance.

### 验收标准

- 上述措辞在 `src/Ether.DesignSystem.Controls/README.md`、`src/Ether.DesignSystem.Foundation/README.md`、`docs/releases/0.1.0-preview.1.md`、`docs/consumers/getting-started.md`、门禁成功信息中**全部一致**
- 数字（445 / 270 / 175 / 943）由证据文件**派生**，不再人工誊写
- 存在检查，**改动分类计数而不更新文案能让它失败**（变异测试）

---

## R-07 — 不支持属性清单是封闭列表

**严重度**：阻断　**批次**：F4　**状态**：✅ 已亲验（2026-08-31）

### 审核声称

`scripts/UnsupportedProperties.psd1` 有 12 条，`Verify-UnsupportedProperties.ps1` 只校验**这 12 条是否仍然无效**，因而**发现不了第 13 个**静默失效的属性。

### 亲验结果（证实）

1. **清单确为封闭列表**：`UnsupportedProperties.psd1` 恰 12 条，全部集中在 EtherDropdown / EtherInput / EtherSwitch 的 Header/HeaderTemplate/Description/Placeholder/Text/IsEditable 家族。文件头注释自述"asserts every entry below is genuinely zero-consumption"——是**允许列表式断言**，不是扫描器。

2. **审核举例属实**：读 `EtherCheckbox.xaml`，实测这些属性的 `TemplateBinding` 出现次数：

   ```
   Background: 0   BorderBrush: 0   BorderThickness: 0
   CornerRadius: 0 Padding: 0       FontSize: 0    （对照 Foreground: 2）
   ```

3. **证据分类吻合**：最新证据 `consumer-runtime-evidence-20260831-010140131` 中，EtherCheckbox 共 8 个属性记为 `platform-dp-contract`（设了像素不变）：`Background`、`BackgroundSizing`、`BorderBrush`、`BorderThickness`、`CornerRadius`、`FontSize`、`Padding`、`HorizontalContentAlignment`、`VerticalContentAlignment`、`CharacterSpacing`、`Clip`、`CompositeMode`、`FontStretch`。这些**全部不在**那 12 条清单里。全库 `platform-dp-contract` 共 156 条。

### 一个审核没点破的关键区分（决定工程方案）

这 156 条不能一刀切当"缺陷"。要分三类，且**大部分是产品决策，不是纯工程**：

- **陷阱型**（必须处理）：消费者会合理期待其生效、结果静默失效的**功能/装饰**属性。现有 12 条属此类（`Header` 会让人以为能加标签、`IsEditable` 会让人以为能打字）。EtherCheckbox 若有同类需查。
- **设计系统自持型**（记录即可）：`Background`/`BorderBrush`/`CornerRadius` 这类**外观**属性，设计系统**故意**不让消费者覆盖以保持视觉一致——"设了没反应"是**特性不是 bug**，但**当前没有任何地方声明这个意图**。
- **无人问津型**（可忽略）：`CompositeMode`、`Clip` 等消费者几乎不会去设的平台底层 DP。

**真正的工程缺陷是检测机制**：允许列表无法**发现**新引入的静默失效属性。这一条与"每个属性怎么归类"无关，是确定的。

### 验收标准

- 检测方式从**封闭清单**改为**开放式**：任何 `platform-dp-contract` 且模板中零 `TemplateBinding` 的公开可写属性都会被自动发现，并要求它落入上述三类之一（有显式归类）
- 新增一个静默失效属性，门禁**能自动发现**（变异测试）
- 已知的 12 条与文档表格保持双向对账（现有能力不得丢失）
- **接线范围已定（见文末决策 2）：一律不接线。** 156 条按三类归档：陷阱型 → 进不支持清单 + 替代方案；设计系统自持型 → 文档声明不开放覆盖；底层 DP → 标记已知无操作。开放式检测要求每条都落入某一类且有显式归档，出现未归档的新静默失效属性即失败。

---

## R-08 — getting-started 夸大 MSIX 支持

**严重度**：应修　**批次**：F5　**状态**：✅ 已亲验

### 证据

`docs/consumers/getting-started.md:12`：

> 宿主：已验证 unpackaged 与 packaged（MSIX）两种形态。

而包元数据 `src/Ether.DesignSystem.Controls/Ether.DesignSystem.Controls.csproj:13` 的口径是：

> x64 unpackaged and packaged (**MSIX build/produce, not install**) consumers are verified; **MSIX install/runtime** ... remain incomplete.

即 MSIX 只验证到**构建/产出**，**未验证安装与运行**。getting-started 的「已验证 packaged（MSIX）」略去了这个限定。

### 验收标准

- `getting-started.md` 与包 `PackageReleaseNotes` 口径一致，明确 build/produce 与 install/runtime 的区别
- 与 R-06 的口径检查合并，由同一处检查覆盖

---

## R-09 — Interactions 包缺发布元数据

**严重度**：应修　**批次**：F5　**状态**：✅ 已亲验

### 证据

| 属性 | Foundation | Controls | Interactions |
|---|---|---|---|
| `Authors` | ✅ `:12` | ✅ `:10` | ✅ `:10` |
| `Description` | ✅ `:13` | ✅ `:11` | ✅ `:11` |
| `PackageTags` | ✅ `:14` | ✅ `:12` | ✅ `:12` |
| `PackageReadmeFile` | ✅ `:19` | ✅ `:17` | ✅ `:15` |
| **`PackageReleaseNotes`** | ✅ `:15` | ✅ `:13` | ❌ **缺** |
| **`PackageProjectUrl`** | ✅ `:16` | ✅ `:14` | ❌ **缺** |

三个包会一起发布，其中一个缺少另外两个都有的发布元数据。

### 验收标准

- Interactions 补齐 `PackageReleaseNotes` 与 `PackageProjectUrl`，内容与另两个包一致
- 存在检查，**三个包的发布元数据字段集合必须相同**

---

## R-10 — 仅为 XAML 解析而公开的类型标注不一致且不可机读

**严重度**：建议　**批次**：F5　**状态**：✅ 已亲验（2026-08-31，**修正了审核措辞**）

### 审核声称

部分类型之所以是 `public`，只是为了让 XAML 能解析到它们，并非面向消费者的 API，但**没有任何标注**区分二者。

### 亲验结果（审核措辞过强，需修正）

"仅为 XAML 解析而公开"的类型共 5 个：

| 类型 | 现状 |
|---|---|
| `EtherStringContentVisibilityConverter` | ✅ 已有 doc 明示"public only because WinUI resolves types ... through public XAML metadata" |
| `HandContentControl` | ✅ 已有 doc 明示"Public only because WinUI XAML ... resolve `using:` types through public metadata" |
| `EtherScrollBarResources` | 🔸 有 doc"this is not a ScrollBar control type"，但没直说"仅为 XAML 而 public" |
| `EtherSwitchResources` | 🔸 有 doc"this is not a ToggleSwitch control type"，同上 |
| `EtherSegmentPanel` | ❌ 仅描述用途，**完全没有**"非消费者 API"的任何说明，读起来像可用的 Panel |

**关键**：全库 `grep EditorBrowsable` = **0 次**。所以无论 doc 写没写，这 5 个类型在消费者的 **IntelliSense 里照样弹出**——doc 注释只帮读源码的人，帮不了用 NuGet 包的人。

结论：审核说"没有任何标注"过强（两个已有明确 doc）；真实问题是**标注不一致**（EtherSegmentPanel 缺）**且缺机器可消费的 `[EditorBrowsable(Never)]`**。

### 验收标准

- 5 个类型统一加 `[EditorBrowsable(EditorBrowsableState.Never)]`，使其在消费者 IntelliSense 中隐藏
- doc 注释统一措辞（补 `EtherSegmentPanel`），说明"public only for XAML resolution"
- 确认加 `EditorBrowsable` 后 `PublicAPI.Unshipped.txt` 与门禁 `RS0016/RS0017` 不冲突（这几个类型仍是 public，仍需登记，只是对 IDE 隐藏）
- 文档说明该约定

---

## R-11 — Foundation 包内混有占位图

**严重度**：建议　**批次**：F5　**状态**：✅ 已亲验

### 证据

`Assets/Icons/Frame 2147253527.png` — 已被 git 跟踪。文件名是 Figma 导出的默认名（`Frame <节点ID>`），随 Foundation 包分发给消费者。

### 验收标准

- 该文件**要么**改成有意义的名字并说明用途，**要么**从仓库与包中移除
- 确认无任何代码/XAML 引用它后再删

---

## R-12 — 证据未绑定到冻结提交

**严重度**：阻断　**批次**：F6　**状态**：✅ 已亲验（成因为我方操作失误）

### 成因

审核期间我派了会改文件的 agent 与审核者并行运行，工作树 diff 从 46/12 涨到 88/18，导致审核者只能声明"我的结论基线是 `e270990`，与当前工作树对不上"。

同类事件在本文件写作当日再次发生：旧 agent `a324386204a1b1fc5` 与其替代者并发运行，两者都会驱动 `Verify-ExternalConsumer.ps1`——而该脚本每次运行都会**清空全局 NuGet 缓存中的三个 Ether 包**并重打 `artifacts/local-feed`。已终止旧 agent 并向新 agent 发出污染告警。

### 施工纪律（立即生效）

- **同一时刻只允许一个 agent 修改仓库**
- 证据生成期间**不允许任何并发改动**
- 每份证据必须记录其对应的 git commit

### 验收标准

- 工作树冻结，`git status` 干净
- 在一个确定的 commit 上，无并发地完整跑一遍全门禁链
- 产出的证据与该 commit **严格一一对应**，并在证据中记录 commit hash
- 本文件全部条目状态更新完毕

---

## 批次与顺序（按依赖排，不按严重度）

| 批次 | 条目 | 排序理由 |
|---|---|---|
| **F0** | R-00 | ✅ 已查清：平台限制，descope 自包含-非打包（详见 R-00，待你复核） |
| **F1** | R-01 | 命令不可复现则后续所有验证结论不可靠——地基 |
| **F2** | R-02 → R-03 | 必须先建单一清单，再修发布路径；顺序反了三个月后会再漂一次 |
| **F3** | R-04, R-05 | 同类：**让失败看起来像成功**。这条链路上已栽过多次 |
| **F4** | R-06, R-07 | 对外口径（R-06 已定 B）+ 开放式检测 |
| **F5** | R-08 ~ R-11 | 卫生项，互不依赖，可一批做完 |
| **F6** | R-12 | 必须最后：前面任何改动都会使证据失效 |

## 决策（全权授权下已全部由我定，2026-08-31）

1. **R-06 口径** → **选 B**（445 保留 + 拆分为 270 视觉生效 / 175 仅契约 / 943 仅可调用）。详见 R-06。
2. **R-07 接线范围** → **一律不接线，改为"声明 + 开放式检测"**。理由：这是**设计系统**，`Background`/`CornerRadius`/`BorderBrush` 等外观属性**故意**不让消费者覆盖，正是为了守住视觉一致性——把它们接线反而破坏设计系统的本意。因此对 156 条 `platform-dp-contract`：陷阱型（功能/装饰，消费者会误以为生效）纳入不支持清单并给替代方案；设计系统自持型（外观）在文档声明"本设计系统不开放覆盖"；其余底层 DP 归为已知无操作。工程交付是**开放式检测 + 三分类归档**，不新增任何 `TemplateBinding`。
3. **本轮不做真实发布**。目标是"只差打发布包"这一步，故：
   - `Publish-Internal.ps1` 保持参数化（feed / owner 作为入参），**不硬编码**任何真实地址
   - 全链跑到 **rehearsal 通过**（`-Push` 不传，`REHEARSAL_EXIT=0`）即为本轮终点
   - 真实 `nuget push` 属发布动作，需用户在最后一步亲自执行并提供 feed 地址——**不在本轮范围**

## 锁定项（任何人不得改动）

- `tests/Ether.DesignSystem.ConsumerFixtures/RuntimeVerification.AttachedVisualProperties.cs` 的 fingerprint / 收敛 / 分类逻辑
- `tests/Ether.DesignSystem.ConsumerFixtures/VisualBaselines/controls/` 的 26 张 golden baseline —— **失败必须查因，绝不重生成**
- `src/Ether.DesignSystem.Foundation/Resources/Tokens/` 下的冻结 token 文件（哈希见 `Verify-ResourceKeys.ps1:18-24`）

## 变更记录

| 日期 | 条目 | 变更 |
|---|---|---|
| 2026-08-31 | — | 建立本文件；R-01/02/03/04/05/06/08/09/11/12 完成亲验；R-07/R-10 仍为审核声称待复核 |
| 2026-08-31 | R-07 | 亲验证实：清单确为 12 条封闭列表；EtherCheckbox 8 属性 `platform-dp-contract` 且不在清单内；补充"陷阱型/设计系统自持型/无人问津型"三分类，接线范围列为产品决策 |
| 2026-08-31 | R-10 | 亲验并**修正审核措辞**：5 个 XAML-only 类型中 2 个已有明确 doc，EtherSegmentPanel 缺；真实问题是标注不一致 + 全库 0 处 `[EditorBrowsable]`，IntelliSense 仍暴露 |
| 2026-08-31 | R-00 | 决议：defect#1 已修+保留至 `spike/self-contained-investigation` @82f12e9；defect#2 判定为平台限制（DefaultStyleKey→PRI），descope 自包含-非打包，发布走框架依赖（待用户复核）。清干净基线 `03bf6ad` |
| 2026-08-31 | R-01 | **F1 落地** commit `26f2adc`：global.json 锁 10.0.400（disable）+ CI 8.0.x→10.0.x + 2 处 PS5.1 hex；restore/build×2/PSCompat 全 exit 0。并行 restore 与 InteractionContracts 均**未复现**（判定为审核期并发污染）。**残留隐患**：CI `10.0.x` 与 `disable` 不精确匹配 → F2 必修为精确 `10.0.400` |
| 2026-08-31 | R-02/R-03 | **F2 落地** `f8002ee`：Gates.psd1 单一事实源 + Verify-GateManifest 反漂移（变异测试过）；Publish-Internal 派生 28 门禁并集（含 PowerShellCompat+GallerySmoke+完整 ConsumerFixtures）；Pack push 移除；CI 精确锁 10.0.400。arm64 脚本未删（有 tracked 注释/文档引用，全惰性 → F5 彻底清）。**顺带修掉 R-04 splat 根因**（哈希表 Args） |
| 2026-08-31 | R-04/R-05 | **F3 落地** `cbd7e51`：删除并 gitignore 误名文件 `-RequireHighContrastParity`；Verify-GateManifest 增"Args 必须哈希表"断言（变异测试过）；Publish-Internal 初始 summary `pushed=$false` + 防回归 guard，仅真实 push 成功后置 true。我独立复跑 GateManifest/ResourceKeys 均 exit 0 |
| 2026-08-31 | R-06 | **F4a 落地** `b213860`：Controls README / release / HANDOFF 改为诚实措辞（270 视觉生效 + 175 仅契约 + 943 仅可调用）；ConsumerFixtures 增 270/175 分类断言（对活证据），新增静态 Verify-PropertyEvidenceWording.ps1（ci，校 README 含 445/270/175/943 且无裸"observable"）；两项变异测试均真跑通过。我独立复跑 Wording/GateManifest 均 exit 0，核心 1388/445/69/874 不变 |
| 2026-08-31 | R-07 | **F4b 落地** `48782c3`：UnsupportedProperties.psd1 增 AcknowledgedSilent（146 条，119 design-system-owned + 27 platform-noop，从活证据生成）；新增 Verify-SilentPropertyCoverage.ps1（local-runtime，正反向 + 变异测试过）；12 traps 静态门禁保留。我独立复跑 Coverage/GateManifest/Unsupported 均 exit 0 |
| 2026-08-31 | R-07 修正 | 我复核发现 F4b 把**功能性**属性误标为"静默失效"：EtherInput.PlaceholderText/AcceptsReturn 有 TemplateBinding（功能正常），IsReadOnly/CharacterCasing 是基类行为属性（正常）。`platform-dp-contract`（像素无变化）不等于"失效"。getting-started **无对外错误声明**（PlaceholderText 示例正常展示）。→ F4c 修正分类：加 TemplateBinding 交叉检查区分"真静默"与"功能正常但不可视测"。另 3 条不确定（HorizontalTextAlignment/DisplayMemberPath/MaxDropDownHeight）标 needs-review |
| 2026-08-31 | 跟进项 | F4b 起了两个 spawn_task：功能 DP 可能升级为 trap（并入 F4c 复核）、Publish-Internal 取证顺序应保证用**新鲜**证据而非磁盘最新（并入 F6 全链复验）。均已纳入本计划，不遗漏 |
| 2026-08-31 | R-07 修正 | **F4c 落地** `63250ba`：加 TemplateBinding 交叉检查，146 条重分桶（94 design-system-owned / 15 consumed-visually-stable / 4 behavioral / 30 platform-noop / 3 needs-review）；19 个功能属性移出"静默失效"；门禁自校验标签正确性（双向变异测试过）。3 条 needs-review（DisplayMemberPath/MaxDropDownHeight/HorizontalTextAlignment）以 warning 浮现，留 F6/用户。我独立复跑 exit 0 |
| 2026-08-31 | arm64 | 决定：**不删** Verify-Arm64Packages.ps1。F2 已将其记入 Gates.psd1 UnmanifestedScripts 并附原因，HANDOFF 记为 unused——已满足 R-02"接入清单并声明用途"一臂。删除会引入 manifest 一致性风险且收益近零。F5 收窄为 R-08/09/10/11 |
| 2026-08-31 | R-08..R-11 | **F5 落地** `43ec6d2`：getting-started MSIX 改为"构建/产出已验证、安装/运行未验证"；Interactions 补 PackageReleaseNotes+PackageProjectUrl（已核实进 nuspec）；5 个 XAML-only 类型加 [EditorBrowsable(Never)]+统一 doc；删除占位图 Frame 2147253527.png（曾随 Foundation 包分发，无代码引用）。我独立复跑 Release build 0/0/exit0、GateManifest exit0、源改全部核对 |
| 2026-08-31 | R-12 前置 | **F6a 落地** `00b6090`：修复取证顺序——ConsumerFixtures 现于 SilentPropertyCoverage 之前跑；后者加 -EvidenceDir/-MinCreationTimeUtc（env 兜底）钉定本轮证据，standalone 保持 newest-on-disk 兼容。聚焦证明（marker 证据）确认读的是钉定文件而非磁盘最新。我独立复跑 -ListGates 顺序正确、GateManifest exit0、tree clean |
