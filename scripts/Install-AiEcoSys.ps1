#requires -Version 5.1
<#
.SYNOPSIS
Installs or upgrades AiEcoSys from the compiled Windows installer.

.PARAMETER InstallerPath
Path to AiEcoSys-Setup.exe.

.PARAMETER InstallDir
Program installation directory. Must be absolute and not on C:.

.PARAMETER DataDir
User data directory. Must be absolute and not on C:.

.PARAMETER DesktopShortcut
Also create a desktop shortcut.

.PARAMETER LogPath
Optional log file. Defaults to D:\AiEcoSys\Logs.
#>
[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Medium')]
param(
    [string]$InstallerPath = 'D:\AiEcoSys\Release\Installer\AiEcoSys-Setup.exe',
    [string]$InstallDir = 'D:\AiEcoSys\Application',
    [string]$DataDir = 'D:\AiEcoSys\Data',
    [switch]$DesktopShortcut,
    [string]$LogPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')

if ([string]::IsNullOrWhiteSpace($LogPath)) {
    $LogPath = New-AiEcoSysLogPath -Name 'install'
}

try {
    $installer = Assert-AiEcoSysSafePath -CandidatePath $InstallerPath -Name 'Installer path'
    $installRoot = Assert-AiEcoSysSafePath -CandidatePath $InstallDir -Name 'Install directory'
    $dataRoot = Assert-AiEcoSysSafePath -CandidatePath $DataDir -Name 'Data directory'

    if (-not (Test-Path -LiteralPath $installer -PathType Leaf)) {
        throw "Installer not found: $installer"
    }

    Write-AiEcoSysLog -Message "Installer: $installer" -LogPath $LogPath
    Write-AiEcoSysLog -Message "Install directory: $installRoot" -LogPath $LogPath
    Write-AiEcoSysLog -Message "Data directory: $dataRoot" -LogPath $LogPath

    $arguments = @(
        '/VERYSILENT',
        '/SUPPRESSMSGBOXES',
        '/NORESTART',
        '/SP-',
        "/DIR=$installRoot",
        "/DATA=$dataRoot"
    )
    if ($DesktopShortcut) {
        $arguments += '/TASKS=desktopicon'
    }

    if ($PSCmdlet.ShouldProcess($installRoot, 'Install or upgrade AiEcoSys')) {
        $process = Start-Process -FilePath $installer -ArgumentList $arguments -Wait -PassThru
        Write-AiEcoSysLog -Message "Installer exit code: $($process.ExitCode)" -LogPath $LogPath
        if ($process.ExitCode -ne 0) {
            throw "Installer failed with exit code $($process.ExitCode)."
        }
    }

    $exe = Join-Path $installRoot 'AiEcoSys.exe'
    $pathsFile = Join-Path $installRoot 'aiecoSys.paths.json'
    $catalogue = Join-Path $installRoot 'catalogue\api.json'

    foreach ($required in @($exe, $pathsFile, $catalogue)) {
        if (-not (Test-Path -LiteralPath $required -PathType Leaf)) {
            throw "Installation verification failed; missing $required"
        }
    }

    $resolvedData = Get-AiEcoSysDataDirectory -InstallDir $installRoot
    if (-not $resolvedData.Equals($dataRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Installed path configuration points to '$resolvedData', expected '$dataRoot'."
    }

    foreach ($directory in @($dataRoot, (Join-Path $dataRoot 'settings'), (Join-Path $dataRoot 'snapshots'))) {
        if (-not (Test-Path -LiteralPath $directory -PathType Container)) {
            throw "Installation verification failed; missing data directory $directory"
        }
    }

    Write-AiEcoSysLog -Message "Installed version $((Get-AiEcoSysVersion -ExePath $exe)) at $installRoot" -Level PASS -LogPath $LogPath
    Write-AiEcoSysLog -Message "Log: $LogPath" -LogPath $LogPath
    exit 0
}
catch {
    Write-AiEcoSysLog -Message $_.Exception.Message -Level ERROR -LogPath $LogPath
    exit 1
}
