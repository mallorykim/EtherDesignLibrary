<#
.SYNOPSIS
Guards scripts/*.ps1 against Windows PowerShell 5.1 incompatibilities so the repo's scripts keep
working for anyone who runs them with the machine-default `powershell` instead of `pwsh`.

.DESCRIPTION
CI invokes every scripts/*.ps1 with `shell: pwsh` (PowerShell 7+), so hosted CI never observes
5.1-only failures. A contributor who runs a script locally with the Windows-default `powershell`
(5.1) can hit syntax or cmdlet-parameter differences that pwsh 7 accepts silently, and land on a
confusing runtime error instead of a clear "this needs PowerShell 7+" message.

This is a static AST check (Parser.ParseFile) against every scripts/*.ps1 file. It does not
execute any script, so it is safe to run against the heavy/GUI ones too. Two classes of drift are
caught:

  1. Syntax that Windows PowerShell 5.1 cannot parse at all (added in PowerShell 7):
     - ternary `cond ? a : b`
     - null-coalescing `??`
     - null-conditional member/index access `?.` / `?[`
     - pipeline chain operators `&&` / `||`
     A file that fails to parse under the engine running this check is also reported, since a
     script this repo cannot even parse is broken regardless of PowerShell version.

  2. Cmdlet parameters that parse fine everywhere but behave differently (or don't exist) on
     Windows PowerShell 5.1:
     - `Select-Object -ExpandProperty` (throws "ExpandPropertyNotFound" on Hashtable-typed
       pipeline input under 5.1; works under 7). Use `| ForEach-Object { $_.<Key> }` or plain
       `.<Key>` member access instead - both work identically on 5.1 and 7.
     - `ForEach-Object -Parallel` (parameter does not exist on 5.1).
     - `ConvertFrom-Json -AsHashtable` (parameter does not exist on 5.1; added in PowerShell 6).

Detection is AST-based (CommandAst / node-type inspection), not text/regex, so occurrences of
these same character sequences inside string literals - e.g. embedded C# source strings in
Verify-ConsumerFixtures.ps1 or Verify-ExternalConsumer.ps1 that legitimately contain C#'s own
`??`, `?.`, `&&` - are not mistaken for PowerShell syntax.

This is a convention gate, not a functional check: it does not evaluate what any script asserts,
only what dialect of PowerShell it is written in.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$scriptsDir = Join-Path $repoRoot 'scripts'

# Command + parameter pairs that parse under both engines but must not be used because their
# runtime behavior (or existence) differs on Windows PowerShell 5.1.
$bannedParameters = @(
    [pscustomobject]@{
        Names     = @('Select-Object', 'select')
        Parameter = 'ExpandProperty'
        Reason    = "'-ExpandProperty' throws ExpandPropertyNotFound on Hashtable-typed pipeline input under Windows PowerShell 5.1 (works under PowerShell 7+). Use '| ForEach-Object { `$_.<Key> }' or plain '.<Key>' member access instead."
    },
    [pscustomobject]@{
        Names     = @('ForEach-Object', '%')
        Parameter = 'Parallel'
        Reason    = "'-Parallel' does not exist on Windows PowerShell 5.1 (added in PowerShell 7)."
    },
    [pscustomobject]@{
        Names     = @('ConvertFrom-Json')
        Parameter = 'AsHashtable'
        Reason    = "'-AsHashtable' does not exist on Windows PowerShell 5.1 (added in PowerShell 6)."
    }
)

function Get-ParameterBinding {
    param([Parameter(Mandatory)][System.Management.Automation.Language.CommandParameterAst]$Element, [Parameter(Mandatory)][string]$FullName)

    # Mirrors PowerShell's own prefix-matching parameter binding (e.g. -Expand, -ExpandProp all
    # bind to -ExpandProperty) so an abbreviated switch cannot silently dodge the ban.
    if ($Element.ParameterName.Length -lt 3) { return $false }
    return $FullName.StartsWith($Element.ParameterName, [System.StringComparison]::OrdinalIgnoreCase)
}

$violations = New-Object System.Collections.Generic.List[string]

Get-ChildItem -LiteralPath $scriptsDir -Filter '*.ps1' | Sort-Object Name | ForEach-Object {
    $file = $_
    $tokens = $null
    $parseErrors = $null
    $ast = [System.Management.Automation.Language.Parser]::ParseFile($file.FullName, [ref]$tokens, [ref]$parseErrors)

    if ($parseErrors.Count -gt 0) {
        foreach ($parseError in $parseErrors) {
            $violations.Add("$($file.Name):$($parseError.Extent.StartLineNumber): failed to parse ($($parseError.ErrorId)): $($parseError.Message)")
        }
        return
    }

    $allNodes = $ast.FindAll({ $true }, $true)

    foreach ($node in $allNodes) {
        $typeName = $node.GetType().Name

        if ($typeName -eq 'CommandAst') {
            $commandName = $node.GetCommandName()
            if (-not $commandName) { continue }

            foreach ($ban in $bannedParameters) {
                if ($ban.Names -notcontains $commandName) { continue }

                foreach ($element in $node.CommandElements) {
                    if ($element -is [System.Management.Automation.Language.CommandParameterAst] -and
                        (Get-ParameterBinding -Element $element -FullName $ban.Parameter)) {
                        $violations.Add("$($file.Name):$($element.Extent.StartLineNumber): '$commandName -$($element.ParameterName)' -- $($ban.Reason)")
                    }
                }
            }
            continue
        }

        if ($typeName -eq 'TernaryExpressionAst') {
            $violations.Add("$($file.Name):$($node.Extent.StartLineNumber): ternary operator '?:' is not supported on Windows PowerShell 5.1 (added in PowerShell 7). Use if/else instead.")
            continue
        }

        if ($typeName -eq 'PipelineChainAst') {
            $violations.Add("$($file.Name):$($node.Extent.StartLineNumber): pipeline chain operator '&&'/'||' is not supported on Windows PowerShell 5.1 (added in PowerShell 7).")
            continue
        }

        # Compared by name, not by static enum member: Windows PowerShell 5.1's own
        # TokenKind enum has no QuestionQuestion value, so referencing it directly (e.g.
        # `-eq [TokenKind]::QuestionQuestion`) would make this gate itself fail to even run
        # under 5.1 - the exact class of bug this gate exists to catch.
        if ($typeName -eq 'BinaryExpressionAst' -and $node.Operator.ToString() -eq 'QuestionQuestion') {
            $violations.Add("$($file.Name):$($node.Extent.StartLineNumber): null-coalescing operator '??' is not supported on Windows PowerShell 5.1 (added in PowerShell 7).")
            continue
        }

        # `??=` (null-coalescing assignment) is a separate AssignmentStatementAst.Operator value,
        # not a BinaryExpressionAst - the `??` check above does not cover it.
        if ($typeName -eq 'AssignmentStatementAst' -and $node.Operator.ToString() -eq 'QuestionQuestionEquals') {
            $violations.Add("$($file.Name):$($node.Extent.StartLineNumber): null-coalescing assignment operator '??=' is not supported on Windows PowerShell 5.1 (added in PowerShell 7).")
            continue
        }

        if (($typeName -eq 'MemberExpressionAst' -or $typeName -eq 'InvokeMemberExpressionAst' -or $typeName -eq 'IndexExpressionAst') -and
            $node.PSObject.Properties['NullConditional'] -and $node.NullConditional) {
            $violations.Add("$($file.Name):$($node.Extent.StartLineNumber): null-conditional operator '?.'/'?[' is not supported on Windows PowerShell 5.1 (added in PowerShell 7).")
        }
    }
}

if ($violations.Count -gt 0) {
    Write-Host "Verify-PowerShellCompatibility failures:" -ForegroundColor Red
    foreach ($violation in $violations) { Write-Host "  - $violation" -ForegroundColor Red }
    throw "Verify-PowerShellCompatibility found $($violations.Count) issue(s) that would break scripts/*.ps1 under Windows PowerShell 5.1. See above."
}

$fileCount = @(Get-ChildItem -LiteralPath $scriptsDir -Filter '*.ps1').Count
Write-Host "Verify-PowerShellCompatibility passed: all $fileCount scripts/*.ps1 files are free of known Windows PowerShell 5.1 syntax/parameter incompatibilities."
