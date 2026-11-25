#requires -Version 5.1
<#
.SYNOPSIS
Verifies an installed AiEcoSys copy, including files, data paths, and launch.

.PARAMETER InstallDir
Installed program directory.

.PARAMETER DataDir
Expected data directory. If omitted, it is read from the installation.

.PARAMETER SkipLaunch
Skip the runtime launch check.

.EXAMPLE
.\Verify-AiEcoSys.ps1 -InstallDir D:\AiEcoSys\Application
#>
[CmdletBinding()]
param(
    [string]$InstallDir = 'D:\AiEcoSys\Application',
    [string]$DataDir,
    [switch]$SkipLaunch,
    [string]$LogPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')

if ([string]::IsNullOrWhiteSpace($LogPath)) {
    $LogPath = New-AiEcoSysLogPath -Name 'verify'
}

$failures = New-Object System.Collections.Generic.List[string]
$checks = New-Object System.Collections.Generic.List[object]

function Add-Check {
    param([string]$Name, [bool]$Passed, [string]$Detail)
    $checks.Add([pscustomobject]@{ Name = $Name; Passed = $Passed; Detail = $Detail }) | Out-Null
    if ($Passed) {
        Write-AiEcoSysLog -Message "$Name : PASS - $Detail" -Level PASS -LogPath $LogPath
    }
    else {
        $failures.Add("$Name : $Detail") | Out-Null
        Write-AiEcoSysLog -Message "$Name : FAIL - $Detail" -Level FAIL -LogPath $LogPath
    }
}

try {
    $installRoot = Assert-AiEcoSysSafePath -CandidatePath $InstallDir -Name 'Install directory'
    Add-Check -Name 'Install directory exists' -Passed (Test-Path -LiteralPath $installRoot -PathType Container) -Detail $installRoot

    $exe = Join-Path $installRoot 'AiEcoSys.exe'
    $catalogue = Join-Path $installRoot 'catalogue\api.json'
    $pathsFile = Join-Path $installRoot 'aiecoSys.paths.json'
    $license = Join-Path $installRoot 'LICENSE'

    Add-Check -Name 'Application executable' -Passed (Test-Path -LiteralPath $exe -PathType Leaf) -Detail $exe
    Add-Check -Name 'Bundled catalogue' -Passed (Test-Path -LiteralPath $catalogue -PathType Leaf) -Detail $catalogue
    Add-Check -Name 'Path configuration' -Passed (Test-Path -LiteralPath $pathsFile -PathType Leaf) -Detail $pathsFile
    Add-Check -Name 'Licence file' -Passed (Test-Path -LiteralPath $license -PathType Leaf) -Detail $license

    $version = Get-AiEcoSysVersion -ExePath $exe
    Add-Check -Name 'Version metadata' -Passed (-not [string]::IsNullOrWhiteSpace($version)) -Detail ([string]$version)

    if ([string]::IsNullOrWhiteSpace($DataDir)) {
        $dataRoot = Get-AiEcoSysDataDirectory -InstallDir $installRoot
    }
    else {
        $dataRoot = Assert-AiEcoSysSafePath -CandidatePath $DataDir -Name 'Data directory'
    }

    Add-Check -Name 'Data directory' -Passed (Test-Path -LiteralPath $dataRoot -PathType Container) -Detail $dataRoot
    Add-Check -Name 'Settings directory' -Passed (Test-Path -LiteralPath (Join-Path $dataRoot 'settings') -PathType Container) -Detail (Join-Path $dataRoot 'settings')
    Add-Check -Name 'Snapshot directory' -Passed (Test-Path -LiteralPath (Join-Path $dataRoot 'snapshots') -PathType Container) -Detail (Join-Path $dataRoot 'snapshots')

    $settings = Join-Path $dataRoot 'settings\app-settings.json'
    if (Test-Path -LiteralPath $settings) {
        try {
            $settingsObject = Get-Content -LiteralPath $settings -Raw | ConvertFrom-Json
            $settingsData = [string]$settingsObject.DataLocation
            Add-Check -Name 'Settings JSON valid' -Passed $true -Detail $settings
            Add-Check -Name 'Settings data path off C:' -Passed (-not $settingsData.StartsWith('C:', [System.StringComparison]::OrdinalIgnoreCase)) -Detail $settingsData
        }
        catch {
            Add-Check -Name 'Settings JSON valid' -Passed $false -Detail $_.Exception.Message
        }
    }
    else {
        Write-AiEcoSysLog -Message 'No settings file yet; it is created on first save.' -Level WARN -LogPath $LogPath
    }

    if (-not $SkipLaunch) {
        $running = @(Get-AiEcoSysProcess -ExePath $exe)
        if ($running.Count -gt 0) {
            Add-Check -Name 'No existing process' -Passed $false -Detail 'AiEcoSys is already running.'
        }
        else {
            $process = Start-Process -FilePath $exe -PassThru
            try {
                $idle = $process.WaitForInputIdle(15000)
                Start-Sleep -Seconds 3
                $process.Refresh()
                Add-Check -Name 'Launch reaches input idle' -Passed ([bool]$idle) -Detail "WaitForInputIdle=$idle"
                Add-Check -Name 'Window title' -Passed ($process.MainWindowTitle -like 'AiEcoSys*') -Detail $process.MainWindowTitle
                Add-Check -Name 'Process responds' -Passed ([bool]$process.Responding) -Detail "Responding=$($process.Responding)"
            }
            finally {
                if (-not $process.HasExited) {
                    $process.CloseMainWindow() | Out-Null
                    if (-not $process.WaitForExit(10000)) {
                        $process.Kill()
                        Add-Check -Name 'Clean shutdown' -Passed $false -Detail 'Process had to be killed.'
                    }
                    else {
                        Add-Check -Name 'Clean shutdown' -Passed ($process.ExitCode -eq 0) -Detail "ExitCode=$($process.ExitCode)"
                    }
                }
            }
        }
    }

    Write-AiEcoSysLog -Message "Checks: $($checks.Count); failures: $($failures.Count)" -LogPath $LogPath
    if ($failures.Count -gt 0) {
        Write-AiEcoSysLog -Message 'Verification failed.' -Level FAIL -LogPath $LogPath
        exit 1
    }

    Write-AiEcoSysLog -Message 'Verification passed.' -Level PASS -LogPath $LogPath
    exit 0
}
catch {
    Write-AiEcoSysLog -Message $_.Exception.Message -Level ERROR -LogPath $LogPath
    exit 1
}

