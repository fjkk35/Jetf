$ErrorActionPreference = 'Stop'

$appRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $appRoot
$apiProjectDir = Join-Path $repoRoot 'PdtPortalApi\PdtPortalApi'
$adbPath = Join-Path $env:LOCALAPPDATA 'Android\Sdk\platform-tools\adb.exe'

function Stop-PortalApiProcess {
    $processes = Get-Process PdtPortalApi -ErrorAction SilentlyContinue | Where-Object {
        $_.Path -and $_.Path.StartsWith($apiProjectDir, [System.StringComparison]::OrdinalIgnoreCase)
    }

    if ($processes) {
        $processes | Stop-Process -Force
        Start-Sleep -Milliseconds 600
    }
}

function Wait-ApiReady {
    param(
        [int]$TimeoutSeconds = 30
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $response = Invoke-WebRequest -Uri 'http://127.0.0.1:5260/api/app/version-check?versionCode=1.0' -UseBasicParsing -TimeoutSec 3
            if ($response.StatusCode -eq 200) {
                return
            }
        } catch {
        }

        Start-Sleep -Milliseconds 500
    }

    throw 'PortalAPI did not become ready on http://127.0.0.1:5260 within 30 seconds.'
}

function Assert-AdbDeviceReady {
    $adbOutput = & $adbPath devices
    $deviceLines = $adbOutput | Where-Object { $_ -match '\tdevice$' }

    if (-not $deviceLines) {
        throw 'No ADB device detected. Please connect and authorize the DT40 first.'
    }
}

Write-Host 'Stopping old PortalAPI process...'
Stop-PortalApiProcess

Write-Host 'Checking ADB device...'
Assert-AdbDeviceReady

Write-Host 'Starting local PortalAPI...'
$apiProcess = Start-Process -FilePath 'dotnet' -ArgumentList @('run', '--launch-profile', 'http') -WorkingDirectory $apiProjectDir -PassThru

if ($apiProcess.HasExited) {
    throw 'Failed to start local PortalAPI.'
}

Wait-ApiReady

Write-Host 'Configuring USB reverse...'
& $adbPath reverse tcp:5260 tcp:5260

Write-Host 'Installing Android debug APK...'
Set-Location $appRoot
& .\gradlew.bat :app:installDebug

Write-Host 'Launching app on device...'
& $adbPath shell am force-stop com.example.jetfapp
& $adbPath shell am start -n com.example.jetfapp/.MainActivity

Write-Host ''
Write-Host 'APP debug flow is ready.'
Write-Host 'PortalAPI PID:' $apiProcess.Id
Write-Host 'If you need to restart backend manually, stop PID' $apiProcess.Id 'first.'