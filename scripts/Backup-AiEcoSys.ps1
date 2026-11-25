#requires -Version 5.1
<#
.SYNOPSIS
Creates a timestamped backup of AiEcoSys settings and snapshots.

.DESCRIPTION
The backup contains user settings and catalogue snapshots only. Credentials
are not included because they live in Windows Credential Manager.

.PARAMETER DataDir
AiEcoSys data directory.

.PARAMETER BackupDir
Destination directory for the ZIP and SHA-256 file.

.PARAMETER IncludeCatalogue
Also include a copy of the catalogue from the data directory, when present.
#>
[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Low')]
param(
    [string]$DataDir = 'D:\AiEcoSys\Data',
    [string]$BackupDir = 'D:\AiEcoSys\Backups',
    [switch]$IncludeCatalogue,
    [string]$LogPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')

if ([string]::IsNullOrWhiteSpace($LogPath)) {
    $LogPath = New-AiEcoSysLogPath -Name 'backup'
}

try {
    $dataRoot = Assert-AiEcoSysSafePath -CandidatePath $DataDir -Name 'Data directory'
    $backupRoot = Assert-AiEcoSysSafePath -CandidatePath $BackupDir -Name 'Backup directory'

    if (-not (Test-Path -LiteralPath $dataRoot -PathType Container)) {
        throw "Data directory not found: $dataRoot"
    }

    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $backupFile = Join-Path $backupRoot "AiEcoSys-backup-$stamp.zip"
    $staging = Join-Path 'D:\AiEcoSys\.temp' ("backup-" + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Force -Path $staging | Out-Null

    try {
        foreach ($name in @('settings', 'snapshots')) {
            $source = Join-Path $dataRoot $name
            if (Test-Path -LiteralPath $source -PathType Container) {
                Copy-Item -LiteralPath $source -Destination $staging -Recurse -Force
            }
        }

        if ($IncludeCatalogue) {
            $catalogue = Join-Path $dataRoot 'catalogue'
            if (Test-Path -LiteralPath $catalogue -PathType Container) {
                Copy-Item -LiteralPath $catalogue -Destination $staging -Recurse -Force
            }
        }

        $manifest = [pscustomobject]@{
            Product = 'AiEcoSys'
            CreatedUtc = (Get-Date).ToUniversalTime().ToString('o')
            SourceDataDirectory = $dataRoot
            IncludesCatalogue = [bool]$IncludeCatalogue
            FormatVersion = 1
        }
        $manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $staging 'backup-manifest.json') -Encoding UTF8

        if ($PSCmdlet.ShouldProcess($backupFile, 'Create AiEcoSys backup')) {
            New-Item -ItemType Directory -Force -Path $backupRoot | Out-Null
            Compress-Archive -Path (Join-Path $staging '*') -DestinationPath $backupFile -Force
        }
    }
    finally {
        if (Test-Path -LiteralPath $staging) {
            Remove-Item -LiteralPath $staging -Recurse -Force
        }
    }

    if (-not (Test-Path -LiteralPath $backupFile -PathType Leaf)) {
        throw "Backup file was not created: $backupFile"
    }

    $hash = (Get-FileHash -LiteralPath $backupFile -Algorithm SHA256).Hash.ToLowerInvariant()
    $hashFile = "$backupFile.sha256"
    "$hash  $(Split-Path -Leaf $backupFile)" | Set-Content -LiteralPath $hashFile -Encoding ASCII

    Write-AiEcoSysLog -Message "Backup: $backupFile" -Level PASS -LogPath $LogPath
    Write-AiEcoSysLog -Message "Size: $((Get-Item -LiteralPath $backupFile).Length) bytes" -LogPath $LogPath
    Write-AiEcoSysLog -Message "SHA-256: $hash" -LogPath $LogPath
    Write-Output $backupFile
    exit 0
}
catch {
    Write-AiEcoSysLog -Message $_.Exception.Message -Level ERROR -LogPath $LogPath
    exit 1
}
