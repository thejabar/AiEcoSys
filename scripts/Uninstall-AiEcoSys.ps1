#requires -Version 5.1
<#
.SYNOPSIS
Uninstalls AiEcoSys while preserving user data by default.

.PARAMETER InstallDir
Installed program directory.

.PARAMETER DataDir
User data directory. If omitted, it is read from aiecoSys.paths.json or the
per-user registry.

.PARAMETER DeleteUserData
Explicitly delete settings, snapshots, and cached catalogue data after the
application files have been removed.

.PARAMETER Force
Skip confirmation prompts. Use with care together with -DeleteUserData.
#>
[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [string]$InstallDir = 'D:\AiEcoSys\Application',
    [string]$DataDir,
    [switch]$DeleteUserData,
    [switch]$Force,
    [string]$LogPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')

if ([string]::IsNullOrWhiteSpace($LogPath)) {
    $LogPath = New-AiEcoSysLogPath -Name 'uninstall'
}

try {
    $installRoot = Assert-AiEcoSysSafePath -CandidatePath $InstallDir -Name 'Install directory'

    if ([string]::IsNullOrWhiteSpace($DataDir)) {
        $registry = Get-ItemProperty -Path 'HKCU:\Software\AiEcoSys' -ErrorAction SilentlyContinue
        if ($null -ne $registry) {
            $property = $registry.PSObject.Properties['DataDirectory']
            if ($null -ne $property) {
                $DataDir = [string]$property.Value
            }
        }
    }
    if ([string]::IsNullOrWhiteSpace($DataDir)) {
        $DataDir = Get-AiEcoSysDataDirectory -InstallDir $installRoot
    }
    $dataRoot = Assert-AiEcoSysSafePath -CandidatePath $DataDir -Name 'Data directory'

    $uninstaller = Join-Path $installRoot 'unins000.exe'
    if (-not (Test-Path -LiteralPath $uninstaller -PathType Leaf)) {
        throw "Uninstaller not found: $uninstaller"
    }

    $running = @(Get-AiEcoSysProcess -ExePath (Join-Path $installRoot 'AiEcoSys.exe'))
    if ($running.Count -gt 0) {
        throw 'AiEcoSys is running. Close it before uninstalling.'
    }

    # The Inno uninstaller only removes installed application files; it never
    # prompts about user data. The uninstaller itself asks before deleting
    # user data, and the -DeleteUserData switch here must be used explicitly.
    $process = Start-Process -FilePath $uninstaller -ArgumentList @(
        '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART'
    ) -Wait -PassThru
    Write-AiEcoSysLog -Message "Uninstaller exit code: $($process.ExitCode)" -LogPath $LogPath
    if ($process.ExitCode -ne 0) {
        throw "Uninstaller failed with exit code $($process.ExitCode)."
    }

    if (Test-Path -LiteralPath (Join-Path $installRoot 'AiEcoSys.exe')) {
        throw "Uninstall verification failed; AiEcoSys.exe still exists in $installRoot"
    }

    if ($DeleteUserData) {
        if ($dataRoot -eq [System.IO.Path]::GetPathRoot($dataRoot)) {
            throw "Refusing to delete a drive root: $dataRoot"
        }
        if (Test-AiEcoSysIsUnder -Child $installRoot -Parent $dataRoot) {
            throw "Refusing to delete data directory '$dataRoot' because it contains the install directory."
        }

        if ($Force -or $PSCmdlet.ShouldProcess($dataRoot, 'Delete AiEcoSys user data')) {
            if (Test-Path -LiteralPath $dataRoot) {
                Remove-Item -LiteralPath $dataRoot -Recurse -Force
            }
            Write-AiEcoSysLog -Message "Deleted user data: $dataRoot" -Level WARN -LogPath $LogPath
        }
    }
    else {
        Write-AiEcoSysLog -Message "User data preserved: $dataRoot" -LogPath $LogPath
    }

    Write-AiEcoSysLog -Message 'Uninstall completed.' -Level PASS -LogPath $LogPath
    exit 0
}
catch {
    Write-AiEcoSysLog -Message $_.Exception.Message -Level ERROR -LogPath $LogPath
    exit 1
}

