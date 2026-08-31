# scripts/Gates.psd1
#
# SINGLE SOURCE OF TRUTH for the acceptance-gate list (R-02). Before this file existed, the gate
# list was hand-maintained in four places - .github/workflows/build.yml, the hardcoded 22-entry
# array + "Count -ne 22" assertion in scripts/Publish-Internal.ps1, the hardcoded 3-script list in
# scripts/Verify-RuntimeGates.ps1, and prose in docs - and they had already drifted from each
# other. Every consumer of the gate list (Publish-Internal.ps1, Verify-RuntimeGates.ps1) MUST
# derive its gate set from this file. .github/workflows/build.yml is YAML and cannot read a .psd1
# at author time, so it instead keeps its steps written out explicitly and is validated AGAINST
# this file by scripts/Verify-GateManifest.ps1, which fails the build if the two disagree.
#
# INVOCATION RULE - READ BEFORE WIRING A NEW CONSUMER: every gate's Args below is a HASHTABLE.
# Callers MUST invoke with a hashtable splat:
#
#     $argsHash = @{}
#     foreach ($key in $gate.Args.Keys) { $argsHash[$key] = $gate.Args[$key] }
#     & $scriptPath @argsHash
#
# NEVER build an array of dash-prefixed strings and splat that (e.g. `@('-RequireHighContrastParity')`).
# PowerShell array splat does NOT re-parse '-Xxx' strings as parameter names - it binds them
# POSITIONALLY, so a switch passed that way silently stays $false while the string lands in the
# first positional parameter instead. That exact bug (R-04) is why
# scripts/Publish-Internal.ps1 once ran "Verify-ResourceKeys.ps1 -RequireHighContrastParity" with
# the parity switch silently off and a stray file named "-RequireHighContrastParity" written to
# the repo root. Hashtable splat has no such ambiguity: keys always bind by name.
#
# ENVIRONMENTS
#   ci             - hosted-runner-safe, no GUI/desktop session. This is exactly what
#                    .github/workflows/build.yml's package-consumers job runs, in order.
#   local-runtime  - needs a local GUI/desktop session (WinUI launch). Owned by
#                    scripts/Verify-RuntimeGates.ps1; hosted CI must never run these.
#   local-external - needs the machine's NuGet cache and a true out-of-repo consumer project.
#
# A gate whose Script needs different arguments per environment gets ONE ENTRY PER ENVIRONMENT
# (e.g. Verify-ConsumerFixtures.ps1 and Verify-MsixPackage.ps1 below each have a 'ci' entry with
# -Skip* switches and a separate 'local-runtime' entry run to completion) rather than trying to
# cram environment-conditional logic into a single Args value. Consumers that need "every gate
# that would run anywhere, using the fullest variant available" (Publish-Internal.ps1) resolve
# duplicates by Script name and prefer the 'local-runtime'/'local-external' entry over the 'ci'
# one for that Script.
#
# Gate Name values are internal labels (used in log output); they are not required to match the
# Script filename. Script values are resolved as scripts/<Script> relative to the repo root.

@{
    Gates = @(
        # --- ci: static/no-GUI gates, in the exact order build.yml's package-consumers job runs them ---
        @{ Name = 'PowerShellCompatibility';      Script = 'Verify-PowerShellCompatibility.ps1';      Environments = @('ci'); Args = @{} }
        @{ Name = 'ResourceKeys';                 Script = 'Verify-ResourceKeys.ps1';                 Environments = @('ci'); Args = @{ RequireHighContrastParity = $true } }
        @{ Name = 'ResourceGraph';                Script = 'Verify-ResourceGraph.ps1';                Environments = @('ci'); Args = @{} }
        @{ Name = 'MarkerContract';               Script = 'Verify-MarkerContract.ps1';               Environments = @('ci'); Args = @{} }
        @{ Name = 'EtherProgressBarContract';     Script = 'Verify-EtherProgressBarContract.ps1';     Environments = @('ci'); Args = @{} }
        @{ Name = 'EtherButtonContract';          Script = 'Verify-EtherButtonContract.ps1';          Environments = @('ci'); Args = @{} }
        @{ Name = 'EtherCheckboxContract';        Script = 'Verify-EtherCheckboxContract.ps1';        Environments = @('ci'); Args = @{} }
        @{ Name = 'EtherRadioButtonContract';     Script = 'Verify-EtherRadioButtonContract.ps1';     Environments = @('ci'); Args = @{} }
        @{ Name = 'EtherInputContract';           Script = 'Verify-EtherInputContract.ps1';           Environments = @('ci'); Args = @{} }
        @{ Name = 'EtherDropdownContract';        Script = 'Verify-EtherDropdownContract.ps1';        Environments = @('ci'); Args = @{} }
        @{ Name = 'EtherSegmentedControlContract';Script = 'Verify-EtherSegmentedControlContract.ps1';Environments = @('ci'); Args = @{} }
        @{ Name = 'EtherIntelligenceButtonContract'; Script = 'Verify-EtherIntelligenceButtonContract.ps1'; Environments = @('ci'); Args = @{} }
        @{ Name = 'EtherSteeringBarContract';     Script = 'Verify-EtherSteeringBarContract.ps1';     Environments = @('ci'); Args = @{} }
        @{ Name = 'EtherSliderContract';          Script = 'Verify-EtherSliderContract.ps1';          Environments = @('ci'); Args = @{} }
        @{ Name = 'EtherMastheadContract';        Script = 'Verify-EtherMastheadContract.ps1';        Environments = @('ci'); Args = @{} }
        @{ Name = 'EtherSwitchContract';          Script = 'Verify-EtherSwitchContract.ps1';          Environments = @('ci'); Args = @{} }
        @{ Name = 'EtherScrollBarContract';       Script = 'Verify-EtherScrollBarContract.ps1';       Environments = @('ci'); Args = @{} }
        @{ Name = 'UnsupportedProperties';        Script = 'Verify-UnsupportedProperties.ps1';        Environments = @('ci'); Args = @{} }
        @{ Name = 'InteractionContracts';         Script = 'Verify-InteractionContracts.ps1';         Environments = @('ci'); Args = @{} }
        @{ Name = 'WinUiConventions';             Script = 'Verify-WinUiConventions.ps1';             Environments = @('ci'); Args = @{} }
        @{ Name = 'HighContrastPairing';          Script = 'Verify-HighContrastPairing.ps1';          Environments = @('ci'); Args = @{} }
        @{ Name = 'GalleryControlExample';        Script = 'Verify-GalleryControlExample.ps1';        Environments = @('ci'); Args = @{} }
        @{ Name = 'GalleryLocalization';          Script = 'Verify-GalleryLocalization.ps1';          Environments = @('ci'); Args = @{} }
        @{ Name = 'PropertyEvidenceWording';      Script = 'Verify-PropertyEvidenceWording.ps1';      Environments = @('ci'); Args = @{} }
        @{ Name = 'ConsumerFixtures-ci';          Script = 'Verify-ConsumerFixtures.ps1';             Environments = @('ci'); Args = @{ SkipSolutionBuild = $true; SkipRuntimeSmoke = $true } }
        @{ Name = 'MsixPackage-ci';               Script = 'Verify-MsixPackage.ps1';                  Environments = @('ci'); Args = @{ SkipSolutionBuild = $true } }
        @{ Name = 'GateManifest';                 Script = 'Verify-GateManifest.ps1';                 Environments = @('ci'); Args = @{} }

        # --- local-runtime: needs a local GUI/desktop session. Owned by Verify-RuntimeGates.ps1 ---
        @{ Name = 'ConsumerFixtures-runtime';     Script = 'Verify-ConsumerFixtures.ps1';             Environments = @('local-runtime'); Args = @{} }
        @{ Name = 'GallerySmoke';                 Script = 'Verify-GallerySmoke.ps1';                 Environments = @('local-runtime'); Args = @{} }
        @{ Name = 'MsixPackage-runtime';          Script = 'Verify-MsixPackage.ps1';                  Environments = @('local-runtime'); Args = @{} }
        # R-07: reads a runtime evidence file (artifacts/audit-runs/consumer-runtime-evidence-*/
        # runtime-result.json) that ConsumerFixtures-runtime (above) produces. Placed last so
        # Verify-RuntimeGates.ps1 (which runs local-runtime gates in this declared order) sees fresh
        # evidence by manifest order alone.
        #
        # R-12 fix: Publish-Internal.ps1 pulls Verify-ConsumerFixtures.ps1/Verify-MsixPackage.ps1 out of
        # manifest order to run them last (dependency ordering - see its own header comment), which used
        # to mean this gate ran BEFORE that run's own ConsumerFixtures pass and graded whatever evidence
        # directory was newest ON DISK from some earlier run instead. Publish-Internal.ps1 now pulls this
        # gate out too and drives it explicitly after ConsumerFixtures, passing the exact evidence
        # directory that pass just produced via Verify-SilentPropertyCoverage.ps1's -EvidenceDir
        # parameter (env var equivalent: $env:ETHER_CONSUMER_EVIDENCE_DIR) - so it is no longer a
        # newest-on-disk scan in either Publish-Internal.ps1 or Verify-RuntimeGates.ps1. See
        # Verify-SilentPropertyCoverage.ps1's own header comment and
        # docs/plans/2026-08-31-release-blockers-spec.md R-12 for the full mechanism.
        @{ Name = 'SilentPropertyCoverage';       Script = 'Verify-SilentPropertyCoverage.ps1';       Environments = @('local-runtime'); Args = @{} }

        # --- local-external: needs the machine's NuGet cache and a true out-of-repo consumer ---
        @{ Name = 'ExternalConsumer';             Script = 'Verify-ExternalConsumer.ps1';             Environments = @('local-external'); Args = @{} }
    )

    # Every scripts/Verify-*.ps1 on disk that is deliberately NOT a Gates entry above, with why.
    # scripts/Verify-GateManifest.ps1 asserts this list plus the Script values above account for
    # every Verify-*.ps1 file on disk, so a newly added gate script can never be silently ignored
    # by both this manifest and every consumer that derives from it.
    UnmanifestedScripts = @(
        @{
            Script = 'Verify-Arm64Packages.ps1'
            Reason = 'x64-only is the decided distribution scope and this script has no automated caller (not chained from Verify-RuntimeGates.ps1, no CI job, no release gate) - see the disabled-since-2026-08-30 note at its own top. It is intentionally kept on disk rather than deleted: it is still referenced from multiple tracked files as a documented re-enable point if arm64 consumer demand returns (Directory.Build.props, HANDOFF.md, docs/architecture/2026-08-26-foundation-refactor-full-review-handoff.md, docs/architecture/2026-08-26-l4c-in-repo-quality-gates.md, docs/architecture/2026-08-26-l4c-quality-gates-plan.md, docs/plans/2026-08-30-third-party-consumability-plan.md, docs/releases/0.1.0-preview.1.md, scripts/Verify-RuntimeGates.ps1). Deleting the script without also scrubbing those references would leave dangling pointers to a file that no longer exists.'
        }
        @{
            Script = 'Verify-RuntimeGates.ps1'
            Reason = 'Orchestrator, not a gate: it is the local/self-hosted-desktop consumer of this manifest''s local-runtime-tagged entries (see Publish-Internal.ps1 and Verify-RuntimeGates.ps1 itself), not something invoked BY the manifest. It has no automated caller of its own - a local desktop operator runs it by hand before a runtime rehearsal.'
        }
    )
}
