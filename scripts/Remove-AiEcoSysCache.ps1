#requires -Version 5.1
<#
.SYNOPSIS
Removes AiEcoSys cache and temporary files without touching settings.

.PARAMETER DataDir
AiEcoSys data directory.

.PARAMETER IncludeSnapshots
Also remove the snapshots directory. This is destructive and requires
confirmation unless -Force is supplied.

.PARAMETER Force
Skip confirmation prompts.
#>
[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Medium')]
param(
    [string]$DataDir = 'D:\AiEcoSys\Data',
    [switch]$IncludeSnapshots,
    [switch]$Force,
    [string]$LogPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')

if ([string]::IsNullOrWhiteSpace($LogPath)) {
    $LogPath = New-AiEcoSysLogPath -Name 'remove-cache'
}

try {
    $dataRoot = Assert-AiEcoSysSafePath -CandidatePath $DataDir -Name 'Data directory'
    if (-not (Test-Path -LiteralPath $dataRoot -PathType Container)) {
        throw "Data directory not found: $dataRoot"
    }

    $targets = @('cache', 'temp')
    if ($IncludeSnapshots) { $targets += 'snapshots' }

    foreach ($name in $targets) {
        $target = Join-Path $dataRoot $name
        if (-not (Test-AiEcoSysIsUnder -Child $target -Parent $dataRoot)) {
            throw "Refusing to remove a path outside the data directory: $target"
        }
        if (-not (Test-Path -LiteralPath $target)) {
            Write-AiEcoSysLog -Message "Not present: $target" -LogPath $LogPath
            continue
        }

        if ($Force -or $PSCmdlet.ShouldProcess($target, "Remove $name")) {
            Remove-Item -LiteralPath $target -Recurse -Force
            Write-AiEcoSysLog -Message "Removed: $target" -Level WARN -LogPath $LogPath
        }
    }

    Write-AiEcoSysLog -Message 'Cache cleanup completed. Settings were not touched.' -Level PASS -LogPath $LogPath
    exit 0
}
catch {
    Write-AiEcoSysLog -Message $_.Exception.Message -Level ERROR -LogPath $LogPath
    exit 1
}
