<#
.SYNOPSIS
Static (no-GUI, no-evidence-file-needed) doc-consistency gate for R-06: fails the build if
src/Ether.DesignSystem.Controls/README.md's property-evidence claim drifts back to overstating
the 445 per-property-evidence properties as uniformly "observable".

.DESCRIPTION
R-06 found that the README described all 445 of 1,388 public writable properties with
per-property runtime evidence as having "observable" evidence, when only 270 of those 445 were
actually proven to visibly take effect (pixel/layout/visibility differences on a rendered,
attached control); the other 175 only proved a DP round-trip (getter/setter invoked without
throwing on an attached control - bitmap pixels unchanged). The agreed fix (R-06, decision B) was
to keep the number 445 but split the wording rather than shrink the headline number to 270.

This script is the CI-safe half of that gate. It does not run the runtime fixture or read any
evidence JSON (scripts/Verify-ConsumerFixtures.ps1's runtime-tagged entry owns that - it derives
270/175/445 from the live Evidence records and asserts they reconcile). This script only checks
that the shipped prose still states the honest split:

  1. The README must mention all four numbers from the agreed wording: 445, 270, 175, 943.
  2. The README must not contain a paragraph that uses the word "observable" without also naming
     270 in the same paragraph - i.e. no standalone "445 ... observable" claim that omits the
     270/175 split. ("Paragraph" = text between blank lines, matching this README's own
     one-paragraph-per-bullet structure.)

A future edit that quietly drops one of the numbers, or reintroduces "445 ... observable evidence"
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

# --- Check 1: all four R-06 numbers must be present -------------------------------------------
# 445 total carrying per-property evidence; 270 proven observable; 175 proven contract-only only;
# 943 = 1,388 - 445 verified only as callable. Matched as bare digit runs so this does not
# accidentally match inside a larger number (e.g. "9430" would not satisfy a "943" check).
$requiredNumbers = @('445', '270', '175', '943')
$missingNumbers = New-Object System.Collections.Generic.List[string]
foreach ($number in $requiredNumbers) {
    if ($readmeText -notmatch "(?<!\d)$number(?!\d)") {
        $missingNumbers.Add($number)
    }
}
if ($missingNumbers.Count -gt 0) {
    throw "Property-evidence wording audit: $readmePath is missing required number(s) $($missingNumbers -join ', ') from the R-06 evidence claim (445 of 1,388 carry evidence: 270 observable + 175 contract-only; 943 callable-only)."
}

# --- Check 2: no standalone "445 ... observable" claim missing the split -----------------------
# Split into paragraphs the same way this README is authored (blank-line-separated bullets).
# Any paragraph using the word "observable" must also name 270 in that same paragraph - otherwise
# it is (or is on its way back to being) the R-06 defect: "observable" used to describe all 445
# without disclosing that 175 of them are contract-only.
$paragraphs = [System.Text.RegularExpressions.Regex]::Split($readmeText, '\r?\n\r?\n')
$offendingParagraphs = New-Object System.Collections.Generic.List[string]
foreach ($paragraph in $paragraphs) {
    if ($paragraph -match '(?i)\bobservable\b' -and $paragraph -notmatch '(?<!\d)270(?!\d)') {
        $offendingParagraphs.Add($paragraph.Trim())
    }
}
if ($offendingParagraphs.Count -gt 0) {
    throw "Property-evidence wording audit: $readmePath uses 'observable' without the 270/175 split in $($offendingParagraphs.Count) paragraph(s) - this is the R-06 overstatement (445 described as uniformly observable). Offending text: $($offendingParagraphs -join ' | ')"
}

Write-Host "Verify-PropertyEvidenceWording passed: $readmePath states the R-06 property-evidence split honestly (445/270/175/943 all present, no standalone 'observable' claim)."
