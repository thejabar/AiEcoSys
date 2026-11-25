#requires -Version 5.1
<#
.SYNOPSIS
Updates an existing AiEcoSys installation while preserving user data.

.PARAMETER InstallerPath
Path to the newer AiEcoSys-Setup.exe.

.PARAMETER InstallDir
Existing program directory.

.PARAMETER DataDir
Existing data directory. If omitted, it is read from the installation.

.PARAMETER Force
Install even when the installer version is not newer.
#>
[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Medium')]
param(
    [string]$InstallerPath = 'D:\AiEcoSys\Release\Installer\AiEcoSys-Setup.exe',
    [string]$InstallDir = 'D:\AiEcoSys\Application',
    [string]$DataDir,
    [switch]$Force,
    [string]$LogPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')

if ([string]::IsNullOrWhiteSpace($LogPath)) {
    $LogPath = New-AiEcoSysLogPath -Name 'update'
}

try {
    $installer = Assert-AiEcoSysSafePath -CandidatePath $InstallerPath -Name 'Installer path'
    $installRoot = Assert-AiEcoSysSafePath -CandidatePath $InstallDir -Name 'Install directory'
    if (-not (Test-Path -LiteralPath $installer -PathType Leaf)) {
        throw "Installer not found: $installer"
    }
    if (-not (Test-Path -LiteralPath (Join-Path $installRoot 'AiEcoSys.exe') -PathType Leaf)) {
        throw "AiEcoSys is not installed at $installRoot. Use Install-AiEcoSys.ps1 first."
    }

    if ([string]::IsNullOrWhiteSpace($DataDir)) {
        $dataRoot = Get-AiEcoSysDataDirectory -InstallDir $installRoot
    }
    else {
        $dataRoot = Assert-AiEcoSysSafePath -CandidatePath $DataDir -Name 'Data directory'
    }

    $installedVersion = Get-AiEcoSysVersion -ExePath (Join-Path $installRoot 'AiEcoSys.exe')
    $installerVersion = (Get-Item -LiteralPath $installer).VersionInfo.FileVersion

    Write-AiEcoSysLog -Message "Installed version: $installedVersion" -LogPath $LogPath
    Write-AiEcoSysLog -Message "Installer version: $installerVersion" -LogPath $LogPath

    if (-not $Force) {
        try {
            $installed = [version]$installedVersion
            $candidate = [version]$installerVersion
            if ($candidate -le $installed) {
                throw "Installer version $candidate is not newer than installed version $installed. Use -Force to reinstall."
            }
        }
        catch [System.Management.Automation.RuntimeException] {
            throw
        }
        catch {
            throw "Could not compare versions. Use -Force to proceed. $($_.Exception.Message)"
        }
    }

    $running = @(Get-AiEcoSysProcess -ExePath (Join-Path $installRoot 'AiEcoSys.exe'))
    if ($running.Count -gt 0) {
        throw 'AiEcoSys is running. Close it before updating.'
    }

    $backupRoot = Join-Path $dataRoot 'backups'
    $backupDir = Join-Path $backupRoot ('pre-update-' + (Get-Date).ToString('yyyyMMdd-HHmmss'))
    $settingsDir = Join-Path $dataRoot 'settings'
    if (Test-Path -LiteralPath $settingsDir) {
        New-Item -ItemType Directory -Force -Path $backupDir | Out-Null
        Copy-Item -LiteralPath $settingsDir -Destination $backupDir -Recurse -Force
        Write-AiEcoSysLog -Message "Settings backed up to $backupDir" -LogPath $LogPath
    }

    $arguments = @(
        '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', '/SP-',
        "/DIR=$installRoot", "/DATA=$dataRoot"
    )

    if ($PSCmdlet.ShouldProcess($installRoot, 'Update AiEcoSys')) {
        $process = Start-Process -FilePath $installer -ArgumentList $arguments -Wait -PassThru
        if ($process.ExitCode -ne 0) {
            throw "Update installer failed with exit code $($process.ExitCode)."
        }
    }

    if (-not (Test-Path -LiteralPath (Join-Path $installRoot 'AiEcoSys.exe') -PathType Leaf)) {
        throw 'Update verification failed; AiEcoSys.exe is missing.'
    }
    if (-not (Test-Path -LiteralPath (Join-Path $installRoot 'catalogue\api.json') -PathType Leaf)) {
        throw 'Update verification failed; the bundled catalogue is missing.'
    }
    if (-not (Test-Path -LiteralPath (Join-Path $dataRoot 'settings'))) {
        throw 'Update verification failed; the settings directory is missing.'
    }

    Write-AiEcoSysLog -Message "Update completed; version $((Get-AiEcoSysVersion -ExePath (Join-Path $installRoot 'AiEcoSys.exe')))" -Level PASS -LogPath $LogPath
    exit 0
}
catch {
    Write-AiEcoSysLog -Message $_.Exception.Message -Level ERROR -LogPath $LogPath
    exit 1
}

