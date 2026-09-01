<#
.SYNOPSIS
Static (no-GUI, no-evidence-file-needed) doc-consistency gate for R-06: fails the build if
src/Ether.DesignSystem.Controls/README.md's property-evidence claim drifts back to overstating
the 555 per-property-evidence properties as uniformly "observable".

.DESCRIPTION
R-06 found that the README described all 445 of 1,388 public writable properties with
per-property runtime evidence as having "observable" evidence, when only 270 of those 445 were
actually proven to visibly take effect (pixel/layout/visibility differences on a rendered,
attached control); the other 175 only proved a DP round-trip (getter/setter invoked without
throwing on an attached control - bitmap pixels unchanged). The agreed fix (R-06, decision B) was
to keep the total number but split the wording rather than shrink the headline number to the
observable-only count.

The 2026-09-01 consumability remediation added three newly-audited types
(EtherTabNavigation/EtherTabItem/EtherSegmentRadioButton) plus 7 new DPs, growing the baseline
445/1,388/270/175/943 to the current 555/1,395/344/211/840 - the numbers below were updated in
lockstep with scripts/Verify-ConsumerFixtures.ps1's acceptance constants so this gate keeps
enforcing the *current* honest split rather than a frozen historical one.

This script is the CI-safe half of that gate. It does not run the runtime fixture or read any
evidence JSON (scripts/Verify-ConsumerFixtures.ps1's runtime-tagged entry owns that - it derives
344/211/555 from the live Evidence records and asserts they reconcile). This script only checks
that the shipped prose still states the honest split:

  1. The README must mention all four numbers from the agreed wording: 555, 344, 211, 840.
  2. The README must not contain a paragraph that uses the word "observable" without also naming
     344 in the same paragraph - i.e. no standalone "555 ... observable" claim that omits the
     344/211 split. ("Paragraph" = text between blank lines, matching this README's own
     one-paragraph-per-bullet structure.)

A future edit that quietly drops one of the numbers, or reintroduces "555 ... observable evidence"
prose without the split, fails this gate instead of silently re-overstating the evidence.
#>

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$readmePath = Join-Path $repoRoot 'src\Ether.DesignSystem.Controls\README.md'

if (-not (Test-Path -LiteralPath $readmePath -PathType Leaf)) {
    throw "Property-evidence wording audit could not find required path: $readmePath"
}

$readmeText = Get-Content -LiteralPath $readmePath -Raw

# --- Check 1: all four current-state numbers must be present ------------------------------------
# 555 total carrying per-property evidence; 344 proven observable; 211 proven contract-only only;
# 840 = 1,395 - 555 verified only as callable. Matched as bare digit runs so this does not
# accidentally match inside a larger number (e.g. "8400" would not satisfy an "840" check).
$requiredNumbers = @('555', '344', '211', '840')
$missingNumbers = New-Object System.Collections.Generic.List[string]
foreach ($number in $requiredNumbers) {
    if ($readmeText -notmatch "(?<!\d)$number(?!\d)") {
        $missingNumbers.Add($number)
    }
}
if ($missingNumbers.Count -gt 0) {
    throw "Property-evidence wording audit: $readmePath is missing required number(s) $($missingNumbers -join ', ') from the R-06 evidence claim (555 of 1,395 carry evidence: 344 observable + 211 contract-only; 840 callable-only)."
}

# --- Check 2: no standalone "555 ... observable" claim missing the split -----------------------
# Split into paragraphs the same way this README is authored (blank-line-separated bullets).
# Any paragraph using the word "observable" must also name 344 in that same paragraph - otherwise
# it is (or is on its way back to being) the R-06 defect: "observable" used to describe all 555
# without disclosing that 211 of them are contract-only.
$paragraphs = [System.Text.RegularExpressions.Regex]::Split($readmeText, '\r?\n\r?\n')
$offendingParagraphs = New-Object System.Collections.Generic.List[string]
foreach ($paragraph in $paragraphs) {
    if ($paragraph -match '(?i)\bobservable\b' -and $paragraph -notmatch '(?<!\d)344(?!\d)') {
        $offendingParagraphs.Add($paragraph.Trim())
    }
}
if ($offendingParagraphs.Count -gt 0) {
    throw "Property-evidence wording audit: $readmePath uses 'observable' without the 344/211 split in $($offendingParagraphs.Count) paragraph(s) - this is the R-06 overstatement (555 described as uniformly observable). Offending text: $($offendingParagraphs -join ' | ')"
}

Write-Host "Verify-PropertyEvidenceWording passed: $readmePath states the R-06 property-evidence split honestly (555/344/211/840 all present, no standalone 'observable' claim)."
