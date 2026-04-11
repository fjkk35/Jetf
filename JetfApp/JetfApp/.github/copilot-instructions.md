# JetfApp — Copilot 開發憲法

## 專案概述

本應用程式 (`com.example.jetfapp`) 為專為 **DT 40 工業手持式裝置**設計的 Android 應用程式。
目標市場為倉儲、物流、製造業等需要條碼掃描與資料蒐集的場景。

---

## 目標裝置規格

| 項目 | 規格 |
|------|------|
| 裝置型號 | DT 40 手持式終端機 |
| 裝置作業系統 | Android 9（API 28） |
| 開發目標版本 | Android 10（API 29） |
| 螢幕解析度 | 以 mdpi / hdpi 為主 |
| 輸入方式 | 實體按鍵 + 觸控螢幕 + 條碼掃描器 |

---

## Android 版本策略

- **compileSdk**: 36
- **targetSdk**: 36
- **minSdk**: 28（支援 DT 40 裝置的 Android 9）

> **重要**：目前 `build.gradle.kts` 的 `minSdk = 29`，需降至 `28` 才能在 DT 40（Android 9）上正常執行。
> 請確認所有使用的 API 均有提供 API 28 的向下相容版本。

### 相容性原則

- 優先使用 `androidx.*` 套件，確保向下相容。
- 使用 `Build.VERSION.SDK_INT` 進行版本條件判斷，避免 API 28 不支援的功能直接呼叫。
- 使用 `@RequiresApi` 和 `@SuppressLint` 時須附上明確的相容性說明註解。
- 避免使用僅在 API 29+ 才有的功能，若必須使用請加入 fallback 邏輯。

---

## 技術棧

| 類別 | 選用技術 |
|------|----------|
| 語言 | Kotlin（主要）|
| 最低 JVM Target | Java 11 |
| UI 框架 | Android View System（XML Layout）|
| 建置系統 | Gradle with Kotlin DSL (`.gradle.kts`) |
| 依賴管理 | `gradle/libs.versions.toml`（Version Catalog）|
| 架構模式 | MVVM（推薦）|

---

## 架構規範

### 目錄結構（建議）

```
com.example.jetfapp/
├── ui/
│   ├── main/           # 主畫面 Activity / Fragment
│   ├── scan/           # 掃描功能相關畫面
│   └── common/         # 通用 UI 元件
├── viewmodel/          # ViewModel 類別
├── data/
│   ├── repository/     # Repository 層
│   ├── local/          # Room 本地資料庫
│   └── model/          # 資料模型（Data Class）
├── scanner/            # 條碼掃描器整合模組
└── utils/              # 工具類別
```

### 架構原則

- 遵循 **MVVM** 架構，UI 層不直接存取資料來源。
- `ViewModel` 持有 `LiveData` 或 `StateFlow` 供 UI 觀察。
- `Repository` 負責抽象化所有資料存取（本地 / 網路）。
- Activity / Fragment 僅負責 UI 邏輯，業務邏輯下放至 ViewModel。

---

## DT 40 裝置開發注意事項

### 條碼掃描器整合

- DT 40 透過 **Intent Broadcast** 傳遞掃描結果，須在 Activity / Fragment 中註冊 `BroadcastReceiver`。
- 掃描結果通常以 `android.intent.ACTION_DECODE_DATA` 或裝置廠商自訂 Intent Action 傳送。
- 請依裝置廠商 SDK 文件設定 Intent Filter。
- 掃描器必須在 `onResume()` 啟動、`onPause()` 停止，避免資源洩漏。

```kotlin
// 範例：掃描器廣播接收
private val scanReceiver = object : BroadcastReceiver() {
    override fun onReceive(context: Context?, intent: Intent?) {
        val barcodeData = intent?.getStringExtra("SCAN_BARCODE_1") ?: return
        // 處理掃描結果
    }
}

override fun onResume() {
    super.onResume()
    val filter = IntentFilter("android.intent.action.SCANRESULT")
    registerReceiver(scanReceiver, filter)
}

override fun onPause() {
    super.onPause()
    unregisterReceiver(scanReceiver)
}
```

### 實體按鍵處理

- DT 40 配備實體功能鍵（如掃描觸發鍵），需透過 `onKeyDown()` 處理。
- 避免依賴純觸控操作，確保所有主要功能可透過實體按鍵完成。

### UI / UX 設計原則

- 使用**大字體**與**高對比色**，配合工業環境下的使用情境（戴手套操作、強光環境）。
- 按鈕高度不低於 `48dp`，觸控目標不小於 `48×48dp`（WCAG 標準）。
- 考慮橫向（Landscape）與直向（Portrait）兩種方向的 Layout。
- 盡量減少鍵盤輸入，優先使用條碼掃描或下拉選單輸入。
- 避免複雜的多層次導覽，保持 UI 流程線性化。

### 螢幕與資源

- 優先提供 `mdpi` 及 `hdpi` 密度的圖片資源。
- 使用 `ConstraintLayout` 作為主要佈局容器，自適應不同螢幕尺寸。
- 避免使用硬寫像素值（`px`），統一使用 `dp` / `sp`。

### 電池與效能

- 工業裝置電池容量有限，避免不必要的後台服務與 Wake Lock。
- 網路請求需設置適當 Timeout（連線 10 秒、讀取 30 秒）。
- 大量資料處理必須在背景執行緒（`Coroutine` / `WorkManager`）中完成，不得阻塞主執行緒。

---

## 程式碼規範

### Kotlin 編碼風格

- 遵循 [Kotlin 官方編碼慣例](https://kotlinlang.org/docs/coding-conventions.html)。
- 使用 `data class` 定義資料模型。
- 優先使用 `val` 而非 `var`，降低可變狀態。
- 使用 Kotlin Coroutines 處理非同步操作，禁止使用 `AsyncTask`（已棄用）。
- 善用擴充函式（Extension Functions）提升可讀性。

### 命名慣例

| 類型 | 命名規則 | 範例 |
|------|----------|------|
| Class / Object | PascalCase | `ScanResultViewModel` |
| 函式 / 變數 | camelCase | `parseBarcodeData()` |
| 常數 | UPPER_SNAKE_CASE | `MAX_RETRY_COUNT` |
| Layout 檔案 | snake_case，前綴類型 | `activity_main.xml`, `fragment_scan.xml` |
| Resource ID | snake_case | `btn_scan_trigger`, `tv_barcode_result` |

### 安全性規範（OWASP Mobile Top 10）

- 不得在程式碼中硬寫任何憑證、API Key 或密碼，使用 `local.properties` 或環境變數。
- 本地儲存的敏感資料必須加密（使用 `EncryptedSharedPreferences` 或 `EncryptedFile`）。
- 網路請求一律使用 HTTPS。
- 使用 Android Keystore 保存金鑰材料。
- 驗證所有外部輸入（掃描結果、Intent 資料），防範注入攻擊。

---

## 測試策略

| 層級 | 工具 | 說明 |
|------|------|------|
| 單元測試 | JUnit 4 + MockK | 測試 ViewModel / Repository 邏輯 |
| 整合測試 | AndroidX Test | 測試 Room DB / DAO |
| UI 測試 | Espresso | 測試關鍵使用者流程 |

- 測試覆蓋率目標：核心業務邏輯 ≥ 80%。
- 建議在真實 DT 40 裝置上進行掃描器功能的端對端測試。

---

## 建置與佈署

- 使用 Gradle Kotlin DSL（`.gradle.kts`）維護建置腳本。
- 依賴版本統一管理於 `gradle/libs.versions.toml`。
- Release 版本必須啟用 ProGuard / R8 混淆。
- 建議使用 CI/CD 自動化建置測試流程。

---

## 禁止事項

- 禁止在主執行緒執行 I/O、網路或資料庫操作。
- 禁止使用已棄用的 `AsyncTask`、`Handler` 進行非同步處理（改用 Coroutines）。
- 禁止忽略 `minSdk = 28` 相容性，每次引入新 API 前須確認支援版本。
- 禁止在 `AndroidManifest.xml` 中申請非必要的危險權限。
- 禁止保留任何 `TODO` / `FIXME` 未處理就進行版本發布。
