#requires -Version 5.1
<#
.SYNOPSIS
Restores AiEcoSys settings and snapshots from a backup ZIP.

.PARAMETER BackupFile
Backup ZIP produced by Backup-AiEcoSys.ps1.

.PARAMETER DataDir
AiEcoSys data directory to restore into.

.PARAMETER Force
Overwrite existing settings and snapshots without prompting.
#>
[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [Parameter(Mandatory = $true)][string]$BackupFile,
    [string]$DataDir = 'D:\AiEcoSys\Data',
    [switch]$Force,
    [string]$LogPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
Add-Type -AssemblyName System.IO.Compression.FileSystem

if ([string]::IsNullOrWhiteSpace($LogPath)) {
    $LogPath = New-AiEcoSysLogPath -Name 'restore'
}

try {
    $backup = Assert-AiEcoSysSafePath -CandidatePath $BackupFile -Name 'Backup file'
    $dataRoot = Assert-AiEcoSysSafePath -CandidatePath $DataDir -Name 'Data directory'

    if (-not (Test-Path -LiteralPath $backup -PathType Leaf)) {
        throw "Backup file not found: $backup"
    }

    $running = @(Get-AiEcoSysProcess)
    if ($running.Count -gt 0) {
        throw 'AiEcoSys is running. Close it before restoring data.'
    }

    $zip = [System.IO.Compression.ZipFile]::OpenRead($backup)
    try {
        foreach ($entry in $zip.Entries) {
            if ([System.IO.Path]::IsPathRooted($entry.FullName) -or
                $entry.FullName -match '(^|[\\/])\.\.([\\/]|$)') {
                throw "Backup contains an unsafe path: $($entry.FullName)"
            }
        }
    }
    finally {
        $zip.Dispose()
    }

    if (-not $Force -and -not $PSCmdlet.ShouldProcess($dataRoot, 'Restore AiEcoSys settings and snapshots')) {
        Write-AiEcoSysLog -Message 'Restore cancelled.' -Level WARN -LogPath $LogPath
        exit 0
    }

    $staging = Join-Path 'D:\AiEcoSys\.temp' ("restore-" + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Force -Path $staging | Out-Null

    try {
        [System.IO.Compression.ZipFile]::ExtractToDirectory($backup, $staging)

        $safetyRoot = Join-Path $dataRoot 'backups'
        New-Item -ItemType Directory -Force -Path $safetyRoot | Out-Null
        $safetyFile = Join-Path $safetyRoot ('pre-restore-' + (Get-Date).ToString('yyyyMMdd-HHmmss') + '.zip')
        $current = @()
        foreach ($name in @('settings', 'snapshots')) {
            $source = Join-Path $dataRoot $name
            if (Test-Path -LiteralPath $source) { $current += $source }
        }
        if ($current.Count -gt 0) {
            Compress-Archive -Path $current -DestinationPath $safetyFile -Force
            Write-AiEcoSysLog -Message "Current data backed up to $safetyFile" -LogPath $LogPath
        }

        New-Item -ItemType Directory -Force -Path $dataRoot | Out-Null
        foreach ($name in @('settings', 'snapshots')) {
            $source = Join-Path $staging $name
            if (Test-Path -LiteralPath $source -PathType Container) {
                $destination = Join-Path $dataRoot $name
                New-Item -ItemType Directory -Force -Path $destination | Out-Null
                Copy-Item -Path (Join-Path $source '*') -Destination $destination -Recurse -Force
            }
        }

        $settings = Join-Path $dataRoot 'settings\app-settings.json'
        if (Test-Path -LiteralPath $settings) {
            Get-Content -LiteralPath $settings -Raw | ConvertFrom-Json | Out-Null
        }

        Write-AiEcoSysLog -Message "Restore completed from $backup" -Level PASS -LogPath $LogPath
    }
    finally {
        if (Test-Path -LiteralPath $staging) {
            Remove-Item -LiteralPath $staging -Recurse -Force
        }
    }

    exit 0
}
catch {
    Write-AiEcoSysLog -Message $_.Exception.Message -Level ERROR -LogPath $LogPath
    exit 1
}

