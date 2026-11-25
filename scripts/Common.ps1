#requires -Version 5.1
Set-StrictMode -Version Latest

<#
.SYNOPSIS
Shared helpers for AiEcoSys release, recovery, and diagnostics scripts.

.DESCRIPTION
All helpers keep application data and logs on non-C: drives. They do not read
or write credentials; credentials remain in Windows Credential Manager.
#>

function Write-AiEcoSysLog {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][string]$Message,
        [ValidateSet('INFO', 'WARN', 'ERROR', 'PASS', 'FAIL')][string]$Level = 'INFO',
        [string]$LogPath
    )

    $line = '{0} [{1}] {2}' -f (Get-Date).ToString('s'), $Level, $Message
    if (-not [string]::IsNullOrWhiteSpace($LogPath)) {
        $directory = Split-Path -Parent $LogPath
        if (-not [string]::IsNullOrWhiteSpace($directory)) {
            New-Item -ItemType Directory -Force -Path $directory | Out-Null
        }
        Add-Content -Path $LogPath -Value $line -Encoding UTF8
    }

    Write-Host $line
}

function Assert-AiEcoSysSafePath {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][string]$CandidatePath,
        [string]$Name = 'Path'
    )

    if ([string]::IsNullOrWhiteSpace($CandidatePath)) {
        throw "$Name cannot be empty."
    }

    if (-not [System.IO.Path]::IsPathRooted($CandidatePath)) {
        throw "$Name must be an absolute path."
    }

    $full = [System.IO.Path]::GetFullPath($CandidatePath)
    $root = [System.IO.Path]::GetPathRoot($full)
    if ($root -match '^[Cc]:') {
        throw "$Name must not be on the C: drive."
    }

    return $full
}

function Get-AiEcoSysDataDirectory {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][string]$InstallDir,
        [string]$DataDir
    )

    if (-not [string]::IsNullOrWhiteSpace($DataDir)) {
        return Assert-AiEcoSysSafePath -CandidatePath $DataDir -Name 'Data directory'
    }

    $pathsFile = Join-Path $InstallDir 'aiecoSys.paths.json'
    if (Test-Path -LiteralPath $pathsFile) {
        try {
            $config = Get-Content -LiteralPath $pathsFile -Raw | ConvertFrom-Json
            $property = $config.PSObject.Properties['DataDirectory']
            if ($null -ne $property -and -not [string]::IsNullOrWhiteSpace([string]$property.Value)) {
                return Assert-AiEcoSysSafePath -CandidatePath ([string]$property.Value) -Name 'Data directory'
            }
        }
        catch {
            throw "Could not read ${pathsFile}: $($_.Exception.Message)"
        }
    }

    return 'D:\AiEcoSys\Data'
}

function New-AiEcoSysLogPath {
    [CmdletBinding()]
    param([Parameter(Mandatory = $true)][string]$Name)

    $directory = 'D:\AiEcoSys\Logs'
    New-Item -ItemType Directory -Force -Path $directory | Out-Null
    return Join-Path $directory ('{0}-{1}.log' -f $Name, (Get-Date).ToString('yyyyMMdd-HHmmss'))
}

function Get-AiEcoSysVersion {
    [CmdletBinding()]
    param([Parameter(Mandatory = $true)][string]$ExePath)

    if (-not (Test-Path -LiteralPath $ExePath)) {
        return $null
    }

    return (Get-Item -LiteralPath $ExePath).VersionInfo.FileVersion
}

function Get-AiEcoSysProcess {
    [CmdletBinding()]
    param([string]$ExePath)

    if ([string]::IsNullOrWhiteSpace($ExePath)) {
        return @(Get-Process -Name 'AiEcoSys' -ErrorAction SilentlyContinue)
    }

    $full = [System.IO.Path]::GetFullPath($ExePath)
    return @(Get-Process -Name 'AiEcoSys' -ErrorAction SilentlyContinue | Where-Object {
        try { $_.Path -eq $full } catch { $false }
    })
}

function Test-AiEcoSysIsUnder {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][string]$Child,
        [Parameter(Mandatory = $true)][string]$Parent
    )

    $childFull = [System.IO.Path]::GetFullPath($Child).TrimEnd('\', '/')
    $parentFull = [System.IO.Path]::GetFullPath($Parent).TrimEnd('\', '/')
    return $childFull.Equals($parentFull, [System.StringComparison]::OrdinalIgnoreCase) -or
           $childFull.StartsWith($parentFull + '\', [System.StringComparison]::OrdinalIgnoreCase)
}
