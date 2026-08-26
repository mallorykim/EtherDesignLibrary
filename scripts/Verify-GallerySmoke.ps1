[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    [int]$TimeoutSeconds = 60,
    [switch]$SkipRuntimeSmoke
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($TimeoutSeconds -lt 1) {
    throw 'TimeoutSeconds must be at least 1.'
}

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$projectPath = Join-Path $repoRoot 'samples\Ether.DesignSystem.Gallery\Ether.DesignSystem.Gallery.csproj'
$outputPath = Join-Path $repoRoot "samples\Ether.DesignSystem.Gallery\bin\x64\$Configuration\net8.0-windows10.0.19041.0"
$executablePath = Join-Path $outputPath 'Ether.DesignSystem.Gallery.exe'
$artifactDirectory = Join-Path $repoRoot 'artifacts\gallery-smoke'
$resultPath = Join-Path $artifactDirectory ("result-{0}.json" -f [guid]::NewGuid().ToString('N'))

Push-Location $repoRoot
try {
    & dotnet build $projectPath -c $Configuration -p:Platform=x64
    if ($LASTEXITCODE -ne 0) {
        throw "Gallery build failed with exit code $LASTEXITCODE."
    }

    if ($SkipRuntimeSmoke) {
        Write-Host 'Gallery runtime smoke skipped by explicit -SkipRuntimeSmoke after the x64 build.'
        return
    }

    if (-not (Test-Path -LiteralPath $executablePath -PathType Leaf)) {
        throw "Gallery executable was not produced at $executablePath."
    }

    New-Item -ItemType Directory -Path $artifactDirectory -Force | Out-Null
    $previousSmokeValue = [Environment]::GetEnvironmentVariable('ETHER_GALLERY_SMOKE', 'Process')
    $previousResultPath = [Environment]::GetEnvironmentVariable('ETHER_GALLERY_SMOKE_RESULT_PATH', 'Process')
    $smokeProcess = $null
    try {
        [Environment]::SetEnvironmentVariable('ETHER_GALLERY_SMOKE', '1', 'Process')
        [Environment]::SetEnvironmentVariable('ETHER_GALLERY_SMOKE_RESULT_PATH', $resultPath, 'Process')
        $smokeProcess = Start-Process -FilePath $executablePath -WorkingDirectory $outputPath -PassThru -WindowStyle Hidden
        $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
        while (-not $smokeProcess.HasExited -and [DateTime]::UtcNow -lt $deadline) {
            Start-Sleep -Milliseconds 250
        }

        if (-not $smokeProcess.HasExited) {
            Stop-Process -Id $smokeProcess.Id -Force
            throw "Gallery smoke timed out after $TimeoutSeconds seconds; stopped only smoke PID $($smokeProcess.Id)."
        }

        if ($smokeProcess.ExitCode -ne 0) {
            throw "Gallery smoke process $($smokeProcess.Id) exited with code $($smokeProcess.ExitCode)."
        }

        if (-not (Test-Path -LiteralPath $resultPath -PathType Leaf)) {
            throw "Gallery smoke process exited without the required result marker: $resultPath"
        }

        $result = Get-Content -LiteralPath $resultPath -Raw | ConvertFrom-Json
        $rounds = @($result.rounds)
        $lightRound = @($rounds | Where-Object { $_.theme -eq 'Light' })
        $darkRound = @($rounds | Where-Object { $_.theme -eq 'Dark' })
        $expectedRoundPageCount = @($result.pageTypes).Count
        if ($result.marker -ne 'ETHER_GALLERY_SMOKE' -or
            $result.outcome -ne 'success' -or
            $expectedRoundPageCount -ne 20 -or
            $rounds.Count -ne 2 -or
            $lightRound.Count -ne 1 -or
            $darkRound.Count -ne 1 -or
            $lightRound[0].totalPageCount -ne $expectedRoundPageCount -or
            $darkRound[0].totalPageCount -ne $expectedRoundPageCount -or
            $lightRound[0].pageCount -ne $expectedRoundPageCount -or
            $darkRound[0].pageCount -ne $expectedRoundPageCount -or
            @($lightRound[0].pageTypes).Count -ne $expectedRoundPageCount -or
            @($darkRound[0].pageTypes).Count -ne $expectedRoundPageCount -or
            $result.pageCount -ne ($expectedRoundPageCount * 2) -or
            $result.totalPageCount -ne ($expectedRoundPageCount * 2) -or
            [string]::IsNullOrWhiteSpace($lightRound[0].backgroundCanvasColor) -or
            [string]::IsNullOrWhiteSpace($darkRound[0].backgroundCanvasColor) -or
            $lightRound[0].backgroundCanvasColor -eq $darkRound[0].backgroundCanvasColor) {
            throw "Gallery smoke did not report success: $(Get-Content -LiteralPath $resultPath -Raw)"
        }

        Write-Host "Gallery smoke passed: Light $($lightRound[0].pageCount)/$expectedRoundPageCount; Dark $($darkRound[0].pageCount)/$expectedRoundPageCount; BackgroundCanvas $($lightRound[0].backgroundCanvasColor) -> $($darkRound[0].backgroundCanvasColor). Evidence: $resultPath"
    }
    finally {
        if ($null -ne $smokeProcess -and -not $smokeProcess.HasExited) {
            Stop-Process -Id $smokeProcess.Id -Force
        }
        [Environment]::SetEnvironmentVariable('ETHER_GALLERY_SMOKE', $previousSmokeValue, 'Process')
        [Environment]::SetEnvironmentVariable('ETHER_GALLERY_SMOKE_RESULT_PATH', $previousResultPath, 'Process')
    }
}
finally {
    Pop-Location
}
