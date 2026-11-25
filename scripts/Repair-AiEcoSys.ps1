#requires -Version 5.1
<#
.SYNOPSIS
Repairs an AiEcoSys installation by reinstalling program files and rebuilding
missing data directories. User settings and snapshots are preserved.

.PARAMETER InstallerPath
Path to AiEcoSys-Setup.exe.

.PARAMETER InstallDir
Existing program directory.

.PARAMETER DataDir
Existing data directory. If omitted, it is read from the installation.
#>
[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Medium')]
param(
    [string]$InstallerPath = 'D:\AiEcoSys\Release\Installer\AiEcoSys-Setup.exe',
    [string]$InstallDir = 'D:\AiEcoSys\Application',
    [string]$DataDir,
    [string]$LogPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')

if ([string]::IsNullOrWhiteSpace($LogPath)) {
    $LogPath = New-AiEcoSysLogPath -Name 'repair'
}

try {
    $installer = Assert-AiEcoSysSafePath -CandidatePath $InstallerPath -Name 'Installer path'
    $installRoot = Assert-AiEcoSysSafePath -CandidatePath $InstallDir -Name 'Install directory'
    if (-not (Test-Path -LiteralPath $installer -PathType Leaf)) {
        throw "Installer not found: $installer"
    }
    if (-not (Test-Path -LiteralPath $installRoot -PathType Container)) {
        throw "AiEcoSys is not installed at $installRoot. Use Install-AiEcoSys.ps1 first."
    }

    if ([string]::IsNullOrWhiteSpace($DataDir)) {
        $dataRoot = Get-AiEcoSysDataDirectory -InstallDir $installRoot
    }
    else {
        $dataRoot = Assert-AiEcoSysSafePath -CandidatePath $DataDir -Name 'Data directory'
    }

    $running = @(Get-AiEcoSysProcess -ExePath (Join-Path $installRoot 'AiEcoSys.exe'))
    if ($running.Count -gt 0) {
        throw 'AiEcoSys is running. Close it before repairing.'
    }

    $settingsDir = Join-Path $dataRoot 'settings'
    $backupDir = Join-Path $dataRoot ('backups\pre-repair-' + (Get-Date).ToString('yyyyMMdd-HHmmss'))
    if (Test-Path -LiteralPath $settingsDir) {
        New-Item -ItemType Directory -Force -Path $backupDir | Out-Null
        Copy-Item -LiteralPath $settingsDir -Destination $backupDir -Recurse -Force
        Write-AiEcoSysLog -Message "Settings backed up to $backupDir" -LogPath $LogPath
    }

    foreach ($directory in @($dataRoot, (Join-Path $dataRoot 'settings'), (Join-Path $dataRoot 'snapshots'))) {
        New-Item -ItemType Directory -Force -Path $directory | Out-Null
    }

    $arguments = @(
        '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', '/SP-',
        "/DIR=$installRoot", "/DATA=$dataRoot"
    )

    if ($PSCmdlet.ShouldProcess($installRoot, 'Repair AiEcoSys')) {
        $process = Start-Process -FilePath $installer -ArgumentList $arguments -Wait -PassThru
        if ($process.ExitCode -ne 0) {
            throw "Repair installer failed with exit code $($process.ExitCode)."
        }
    }

    foreach ($required in @(
        (Join-Path $installRoot 'AiEcoSys.exe'),
        (Join-Path $installRoot 'catalogue\api.json'),
        (Join-Path $installRoot 'aiecoSys.paths.json')
    )) {
        if (-not (Test-Path -LiteralPath $required -PathType Leaf)) {
            throw "Repair verification failed; missing $required"
        }
    }

    Write-AiEcoSysLog -Message 'Repair completed.' -Level PASS -LogPath $LogPath
    exit 0
}
catch {
    Write-AiEcoSysLog -Message $_.Exception.Message -Level ERROR -LogPath $LogPath
    exit 1
}

