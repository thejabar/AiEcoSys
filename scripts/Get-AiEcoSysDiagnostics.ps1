#requires -Version 5.1
<#
.SYNOPSIS
Collects AiEcoSys diagnostics without credentials or secret values.

.PARAMETER InstallDir
Installed program directory.

.PARAMETER DataDir
Data directory. If omitted, it is read from the installation.

.PARAMETER OutputPath
Diagnostics text file. Defaults to D:\AiEcoSys\Logs\diagnostics-<timestamp>.txt.
#>
[CmdletBinding()]
param(
    [string]$InstallDir = 'D:\AiEcoSys\Application',
    [string]$DataDir,
    [string]$OutputPath,
    [string]$LogPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')

if ([string]::IsNullOrWhiteSpace($LogPath)) {
    $LogPath = New-AiEcoSysLogPath -Name 'diagnostics'
}

try {
    $installRoot = Assert-AiEcoSysSafePath -CandidatePath $InstallDir -Name 'Install directory'

    if ([string]::IsNullOrWhiteSpace($DataDir)) {
        $dataRoot = Get-AiEcoSysDataDirectory -InstallDir $installRoot
    }
    else {
        $dataRoot = Assert-AiEcoSysSafePath -CandidatePath $DataDir -Name 'Data directory'
    }

    if ([string]::IsNullOrWhiteSpace($OutputPath)) {
        $OutputPath = Join-Path 'D:\AiEcoSys\Logs' ('diagnostics-' + (Get-Date).ToString('yyyyMMdd-HHmmss') + '.txt')
    }
    $output = Assert-AiEcoSysSafePath -CandidatePath $OutputPath -Name 'Output path'
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $output) | Out-Null

    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add('AiEcoSys diagnostics') | Out-Null
    $lines.Add('Generated: ' + (Get-Date).ToString('o')) | Out-Null
    $lines.Add('') | Out-Null
    $lines.Add('[System]') | Out-Null
    $lines.Add('Computer: ' + $env:COMPUTERNAME) | Out-Null
    $lines.Add('User: ' + $env:USERNAME) | Out-Null
    $lines.Add('OS: ' + [System.Environment]::OSVersion.VersionString) | Out-Null
    $lines.Add('PowerShell: ' + $PSVersionTable.PSVersion.ToString()) | Out-Null
    $lines.Add('64-bit process: ' + [System.Environment]::Is64BitProcess) | Out-Null
    $lines.Add('') | Out-Null

    $lines.Add('[Application]') | Out-Null
    $exe = Join-Path $installRoot 'AiEcoSys.exe'
    $lines.Add('Install directory: ' + $installRoot) | Out-Null
    $lines.Add('Executable exists: ' + (Test-Path -LiteralPath $exe)) | Out-Null
    if (Test-Path -LiteralPath $exe) {
        $item = Get-Item -LiteralPath $exe
        $lines.Add('Size: ' + $item.Length) | Out-Null
        $lines.Add('Version: ' + $item.VersionInfo.FileVersion) | Out-Null
        $lines.Add('Product: ' + $item.VersionInfo.ProductName) | Out-Null
        $lines.Add('SHA-256: ' + (Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash.ToLowerInvariant()) | Out-Null
    }

    $pathsFile = Join-Path $installRoot 'aiecoSys.paths.json'
    $lines.Add('Path configuration exists: ' + (Test-Path -LiteralPath $pathsFile)) | Out-Null
    if (Test-Path -LiteralPath $pathsFile) {
        $lines.Add('Path configuration: ' + (Get-Content -LiteralPath $pathsFile -Raw).Trim()) | Out-Null
    }
    $catalogue = Join-Path $installRoot 'catalogue\api.json'
    $lines.Add('Bundled catalogue exists: ' + (Test-Path -LiteralPath $catalogue)) | Out-Null
    if (Test-Path -LiteralPath $catalogue) {
        $lines.Add('Catalogue size: ' + (Get-Item -LiteralPath $catalogue).Length) | Out-Null
    }
    $lines.Add('') | Out-Null

    $lines.Add('[Data]') | Out-Null
    $lines.Add('Data directory: ' + $dataRoot) | Out-Null
    $lines.Add('Data directory exists: ' + (Test-Path -LiteralPath $dataRoot)) | Out-Null
    if (Test-Path -LiteralPath $dataRoot) {
        foreach ($entry in Get-ChildItem -LiteralPath $dataRoot -Force) {
            $lines.Add(('  {0}  {1}' -f $entry.Name, $(if ($entry.PSIsContainer) { '<dir>' } else { $entry.Length }))) | Out-Null
        }
    }
    $settings = Join-Path $dataRoot 'settings\app-settings.json'
    if (Test-Path -LiteralPath $settings) {
        $settingsText = Get-Content -LiteralPath $settings -Raw
        $settingsText = [regex]::Replace(
            $settingsText,
            '("(?:[^"]*(?:key|token|secret|password)[^"]*)"\s*:\s*)"[^"]*"',
            '$1"[REDACTED]"',
            [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
        $lines.Add('Settings:') | Out-Null
        $lines.Add($settingsText.Trim()) | Out-Null
    }
    $lines.Add('') | Out-Null

    $lines.Add('[Processes]') | Out-Null
    $processes = @(Get-Process -Name 'AiEcoSys' -ErrorAction SilentlyContinue)
    if ($processes.Count -eq 0) {
        $lines.Add('AiEcoSys is not running.') | Out-Null
    }
    else {
        foreach ($process in $processes) {
            $lines.Add(('PID {0}; Responding={1}; Title={2}' -f $process.Id, $process.Responding, $process.MainWindowTitle)) | Out-Null
        }
    }
    $lines.Add('') | Out-Null

    $lines.Add('[Storage]') | Out-Null
    foreach ($drive in [System.IO.DriveInfo]::GetDrives() | Where-Object { $_.IsReady }) {
        $lines.Add(('{0} {1} free of {2}' -f $drive.Name, $drive.AvailableFreeSpace, $drive.TotalSize)) | Out-Null
    }
    $lines.Add('') | Out-Null

    $lines.Add('[Registry]') | Out-Null
    $registry = Get-ItemProperty -Path 'HKCU:\Software\AiEcoSys' -ErrorAction SilentlyContinue
    if ($null -eq $registry) {
        $lines.Add('HKCU\Software\AiEcoSys not present.') | Out-Null
    }
    else {
        $lines.Add('InstallDirectory: ' + $registry.InstallDirectory) | Out-Null
        $lines.Add('DataDirectory: ' + $registry.DataDirectory) | Out-Null
        $lines.Add('Version: ' + $registry.Version) | Out-Null
    }

    Set-Content -LiteralPath $output -Value $lines -Encoding UTF8
    Write-AiEcoSysLog -Message "Diagnostics written to $output" -Level PASS -LogPath $LogPath
    Write-Output $output
    exit 0
}
catch {
    Write-AiEcoSysLog -Message $_.Exception.Message -Level ERROR -LogPath $LogPath
    exit 1
}
