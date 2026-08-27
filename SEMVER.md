# Ether Design System versioning policy

`Ether.DesignSystem.Foundation` and `Ether.DesignSystem.Controls` are preview packages until a stable release is explicitly announced. During preview, APIs can change, but every change remains visible through the PublicAPI analyzer baselines and release notes. Stable releases follow Semantic Versioning: patch releases are backward-compatible fixes, minor releases add backward-compatible capability, and major releases contain breaking changes.

The two packages are published in lockstep on a coordinated release line. Controls declares a compatible Foundation NuGet dependency range rather than an exact-version lock; source and publishing remain coordinated until compatibility evidence supports independent versioning and an explicit policy change.

Public API is governed by `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt`. Those files are the current public surface, not a removal ledger: the analyzer does not record deleted members. Preview removals (for example EtherButton `RightIconVisibility`) stay visible through git history of the baselines, release notes, architecture notes, and contract-script guards. Removing, renaming, or changing the compatible behavior of a shipped public member is breaking and requires a major stable version (or clear preview release notes before stability). Additive public API is normally a minor stable change.

Public resource keys are compatibility surface. Deleting or renaming a published key is breaking, including a resource key consumed from XAML. Changing a resource value is classified by impact: an implementation-only correction with no intended consumer-visible effect is a patch; a new backward-compatible visual capability is minor; and a material visual, accessibility, layout, or behavior change that can break consumer assumptions is breaking. Resource-key audits must keep Light and Dark key sets aligned. High Contrast deficits are reported explicitly in preview; a stable release must run `Verify-ResourceKeys.ps1 -RequireHighContrastParity` and cannot ship while the parity gate fails.

Primitive and semantic token files under `src/Resources/Tokens/` are frozen for this refactor. Their keys, values, theme mappings, and file hashes do not change in this release line.

The Gallery and all test projects are non-packable and are not package compatibility surface.
