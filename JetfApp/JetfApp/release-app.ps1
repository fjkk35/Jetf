<#
.SYNOPSIS
    發行 JetfApp 正式版 APK。

.DESCRIPTION
    1. 輸入發行路徑與版本號（版本號必須大於目前版本，格式 x.y.z，y、z 為 0~99）
    2. 更新 app/build.gradle.kts 的 appVersionName
    3. 更新 PdtPortalApi/appsettings.json 的 AppVersion.LatestVersionCode
    4. 使用 jks/jetf.jks 簽章並建置 release APK
    5. 確認 APK 內的版本號與輸入相同
    6. 將 APK 複製到發行路徑（檔名 JetfApp.apk）
    任一步驟失敗時會自動還原 APP 與 API 的版本號。

.EXAMPLE
    .\release-app.ps1
    .\release-app.ps1 -ReleasePath 'F:\本機發行\apk' -Version 0.0.6
#>
param(
    [string]$ReleasePath,
    [string]$Version
)

$ErrorActionPreference = 'Stop'

$appRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$gradleFile = Join-Path $appRoot 'app\build.gradle.kts'
$apiSettingsFile = Join-Path (Split-Path -Parent $appRoot) 'PdtPortalApi\PdtPortalApi\appsettings.json'
$keystoreFile = Join-Path $appRoot 'jks\jetf.jks'
$apkOutputFile = Join-Path $appRoot 'app\build\outputs\apk\release\app-release.apk'
$releaseApkName = 'JetfApp.apk'

# 簽章設定（可用環境變數覆寫）
$keystorePassword = if ($env:JETF_KEYSTORE_PASSWORD) { $env:JETF_KEYSTORE_PASSWORD } else { '123456' }
$keyAlias = if ($env:JETF_KEY_ALIAS) { $env:JETF_KEY_ALIAS } else { 'key0' }
$keyPassword = if ($env:JETF_KEY_PASSWORD) { $env:JETF_KEY_PASSWORD } else { $keystorePassword }

$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function Get-CurrentVersion {
    $content = [System.IO.File]::ReadAllText($gradleFile)
    $match = [regex]::Match($content, 'val\s+appVersionName\s*=\s*"(?<version>[^"]+)"')
    if (-not $match.Success) {
        throw "找不到 $gradleFile 的 appVersionName"
    }
    return $match.Groups['version'].Value
}

function ConvertTo-VersionNumber([string]$text) {
    # 格式 x.y.z；y、z 0~99（versionCode = x*10000 + y*100 + z）
    if ($text -notmatch '^(?<major>\d+)\.(?<minor>\d{1,2})\.(?<patch>\d{1,2})$') {
        return $null
    }
    return [version]::new([int]$Matches['major'], [int]$Matches['minor'], [int]$Matches['patch'])
}

function Set-GradleVersion([string]$newVersion) {
    $content = [System.IO.File]::ReadAllText($gradleFile)
    $updated = [regex]::Replace($content, '(val\s+appVersionName\s*=\s*")[^"]+(")', "`${1}$newVersion`${2}")
    [System.IO.File]::WriteAllText($gradleFile, $updated, $utf8NoBom)
}

function Get-ApiLatestVersion {
    $settings = [System.IO.File]::ReadAllText($apiSettingsFile) | ConvertFrom-Json
    return [string]$settings.AppVersion.LatestVersionCode
}

function Set-ApiLatestVersion([string]$newVersion) {
    # 只替換 LatestVersionCode 的值，保留 appsettings.json 原本的排版
    $content = [System.IO.File]::ReadAllText($apiSettingsFile)
    $pattern = '("LatestVersionCode"\s*:\s*")[^"]*(")'
    if (-not [regex]::IsMatch($content, $pattern)) {
        throw "找不到 $apiSettingsFile 的 AppVersion.LatestVersionCode"
    }
    $updated = [regex]::Replace($content, $pattern, "`${1}$newVersion`${2}")
    [System.IO.File]::WriteAllText($apiSettingsFile, $updated, $utf8NoBom)

    $savedVersion = Get-ApiLatestVersion
    if ($savedVersion -ne $newVersion) {
        throw "API 版本號更新失敗，目前為 $savedVersion"
    }
}

function Get-ApkVersionName([string]$apkFile) {
    # 使用 Android SDK build-tools 的 aapt2 讀取 APK 內的 versionName
    $sdkRoot = if ($env:ANDROID_HOME) { $env:ANDROID_HOME } else { Join-Path $env:LOCALAPPDATA 'Android\Sdk' }
    $aapt2 = Get-ChildItem (Join-Path $sdkRoot 'build-tools') -Directory -ErrorAction SilentlyContinue |
        Sort-Object { [version]($_.Name -replace '[^\d\.].*$', '') } -Descending |
        ForEach-Object { Join-Path $_.FullName 'aapt2.exe' } |
        Where-Object { Test-Path $_ } |
        Select-Object -First 1
    if (-not $aapt2) {
        return $null
    }

    $badging = & $aapt2 dump badging $apkFile 2>$null | Select-Object -First 1
    $match = [regex]::Match([string]$badging, "versionName='(?<version>[^']+)'")
    if ($match.Success) { return $match.Groups['version'].Value }
    return $null
}

# ---- 讀取目前版本 ----
$currentVersionText = Get-CurrentVersion
$currentVersion = ConvertTo-VersionNumber $currentVersionText
if (-not (Test-Path $apiSettingsFile)) {
    throw "找不到 API 設定檔 $apiSettingsFile"
}
$currentApiVersionText = Get-ApiLatestVersion
Write-Host "目前版本：APP $currentVersionText / API $currentApiVersionText" -ForegroundColor Cyan

# ---- 輸入發行路徑 ----
while ([string]::IsNullOrWhiteSpace($ReleasePath)) {
    $ReleasePath = (Read-Host '請輸入發行路徑（APK 會複製到此資料夾）').Trim().Trim('"')
}
$ReleasePath = [System.IO.Path]::GetFullPath($ReleasePath)

# ---- 輸入版本號（必須大於目前版本） ----
while ($true) {
    if ([string]::IsNullOrWhiteSpace($Version)) {
        $Version = (Read-Host "請輸入發行版本號（需大於 $currentVersionText，格式 x.y.z）").Trim()
    }

    $newVersion = ConvertTo-VersionNumber $Version
    if ($null -eq $newVersion) {
        Write-Host "版本號格式錯誤：$Version（格式 x.y.z，第二、三碼 0~99）" -ForegroundColor Red
    }
    elseif ($null -ne $currentVersion -and $newVersion -le $currentVersion) {
        Write-Host "版本號 $Version 必須大於目前版本 $currentVersionText" -ForegroundColor Red
    }
    else {
        break
    }
    $Version = $null
}

Write-Host ''
Write-Host "發行路徑：$ReleasePath"
Write-Host "APP 版本：$currentVersionText -> $Version"
Write-Host "API 版本：$currentApiVersionText -> $Version"

if (-not (Test-Path $keystoreFile)) {
    throw "找不到簽章檔 $keystoreFile"
}

# ---- 更新版本號 ----
$gradleBackup = [System.IO.File]::ReadAllText($gradleFile)
$apiSettingsBackup = [System.IO.File]::ReadAllText($apiSettingsFile)

try {
    Set-GradleVersion $Version
    Set-ApiLatestVersion $Version
    Write-Host "API 版本號已更新：$Version" -ForegroundColor Green

    # ---- 建置簽章版 APK ----
    Write-Host ''
    Write-Host '建置 release APK...' -ForegroundColor Cyan
    if (Test-Path $apkOutputFile) {
        Remove-Item $apkOutputFile -Force
    }

    Push-Location $appRoot
    try {
        & .\gradlew.bat :app:assembleRelease `
            "-Pandroid.injected.signing.store.file=$keystoreFile" `
            "-Pandroid.injected.signing.store.password=$keystorePassword" `
            "-Pandroid.injected.signing.key.alias=$keyAlias" `
            "-Pandroid.injected.signing.key.password=$keyPassword"
        if ($LASTEXITCODE -ne 0) {
            throw "Gradle 建置失敗（exit code $LASTEXITCODE）"
        }
    }
    finally {
        Pop-Location
    }

    if (-not (Test-Path $apkOutputFile)) {
        throw "找不到建置產出的 APK：$apkOutputFile"
    }

    # ---- 確認 APK 版本號 ----
    $apkVersion = Get-ApkVersionName $apkOutputFile
    if ($null -eq $apkVersion) {
        Write-Warning '找不到 aapt2，無法確認 APK 版本號'
    }
    elseif ($apkVersion -ne $Version) {
        throw "APK 版本號 $apkVersion 與輸入的 $Version 不符"
    }
    else {
        Write-Host "APK 版本號確認：$apkVersion" -ForegroundColor Green
    }
}
catch {
    # 任一步驟失敗，還原 APP 與 API 版本號
    [System.IO.File]::WriteAllText($gradleFile, $gradleBackup, $utf8NoBom)
    [System.IO.File]::WriteAllText($apiSettingsFile, $apiSettingsBackup, $utf8NoBom)
    Write-Host "發行失敗，已還原版本號：APP $currentVersionText / API $currentApiVersionText" -ForegroundColor Red
    throw
}

# ---- 複製到發行路徑 ----
New-Item -ItemType Directory -Force -Path $ReleasePath | Out-Null
$releaseApkFile = Join-Path $ReleasePath $releaseApkName
Copy-Item $apkOutputFile $releaseApkFile -Force

Write-Host ''
Write-Host '發行完成' -ForegroundColor Green
Write-Host "APK　　：$releaseApkFile"
Write-Host "版本號　：$Version"
Write-Host '已更新　：app/build.gradle.kts (appVersionName)'
Write-Host '　　　　　PdtPortalApi/appsettings.json (AppVersion.LatestVersionCode)'
