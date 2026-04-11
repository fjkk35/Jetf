## Context

目前 Android app 僅有單一 Activity 與預設 Hello World 版面，尚未具備登入、版本檢查、入庫流程、掃描整合與 API 通訊能力。此變更需在 Android 10 開發環境下完成，但最終須能在 DT 40 的 Android 9 裝置上執行，因此不能依賴 API 29 以上限定功能，也必須避免將 HMAC 金鑰直接寫入原始碼。

入庫作業是連續、高頻且以掃描器/實體鍵操作為主的流程，因此畫面切換、固定底部功能鍵、欄位鎖定與異常提示都需要明確狀態管理。後端 API 已定義共用 response envelope 與 HMAC 簽章規則，APP 需統一解析成功/失敗與錯誤碼。

## Goals / Non-Goals

**Goals:**
- 以單一 Activity 搭配多個 Fragment 建立 Splash、Login、Menu、Inbound Settings、Inbound Work 流程。
- 建立共享 ViewModel 管理登入狀態、版本檢查結果、入庫設定、當前流水號與底部按鍵行為。
- 建立 Retrofit/OkHttp 網路層，支援 response envelope、版本檢查、登入、來源查詢、檢查、寫入與 HMAC 簽章。
- 支援 DT 40 掃描廣播輸入與手動輸入兩種作業方式。
- 使用 build-time 設定注入 Base URL 與 HMAC Key，避免將敏感值硬編碼在 repo。

**Non-Goals:**
- 不實作 APK 自動下載或靜默更新，只提供更新提示與阻擋/放行流程。
- 不建立本地資料庫或離線佇列，所有作業皆以即時 API 為準。
- 不在本次變更實作完整硬體廠商 SDK 整合，先以 DT 40 廣播掃描模式為主。

## Decisions

### 1. 採用單一 Activity + Fragment 容器
以 `MainActivity` 作為固定底部功能鍵與導覽宿主，所有畫面使用 Fragment 呈現。這樣可以讓底部 F3/F4 區域固定在 Activity 內，不會因 Fragment 內容滾動或鍵盤彈出而重建，也能集中處理實體按鍵與掃描廣播。

替代方案是多 Activity 流程，但那會讓底部功能列、按鍵映射與共用狀態分散，DT 40 的連續作業體驗較難維持一致。

### 2. 採用 MVVM 與共享狀態模型
建立 `AppViewModel` 與 `ShipmentInboundViewModel`，用 `StateFlow` 或 `LiveData` 暴露畫面狀態。`ShipmentInboundViewModel` 持有貨件來源、起始流水號、當前流水號、儲位鎖定狀態、退件單號可見性與 API 執行狀態。這可以讓掃描輸入、按鍵操作與 API 回應都透過單一狀態源更新 UI，避免 Fragment 間以 bundle 零散傳值。

### 3. 採用 Retrofit + OkHttp + Gson
使用 Retrofit 定義 API 介面，OkHttp 處理 request/response 與 header 注入，Gson 解析 envelope/data。對這個中小型 APP 來說，這比手寫 `HttpURLConnection` 更容易維護，也能較清楚封裝簽章邏輯與錯誤處理。

### 4. 以 BuildConfig 注入環境參數
`API_BASE_URL` 與 `PDT_HMAC_KEY` 由 `local.properties` 或 Gradle properties 注入 `BuildConfig`。若未設定，APP 仍可編譯，但在需要呼叫 API 時明確提示設定缺漏。這符合專案安全要求，也避免把 HMAC key 直接提交到版本庫。

### 5. 掃描輸入集中由 Activity 分派
`MainActivity` 在前景生命週期註冊掃描 `BroadcastReceiver`，收到掃描資料後轉交給目前可接收掃描的 Fragment 或共享 ViewModel。這樣能確保掃描器生命週期與畫面一致，也便於後續擴充不同 action/extra key。

### 6. 寫入流程採兩段式檢查
在入庫作業畫面輸入或掃描單號後，先呼叫 `/api/shipmentinbound/check`。若回傳 `false`，顯示「不明貨」對話框，按確認才繼續寫入；若回傳 `true`，直接呼叫寫入 API。寫入成功後再遞增流水號並清空單號/退件欄位，以降低重複掃描風險。

### 7. 流水號遞增由純 Kotlin 工具類處理
建立專用 formatter/validator 處理 `AA0001` 到 `AA9999` 的驗證與遞增，避免把字串規則散落在 UI 層。若已到 `9999`，停止遞增並提示使用者回上一步變更。

## Risks / Trade-offs

- [DT 40 廣播 action 或 extra key 可能與實機設定不同] → 預設提供集中常數與可調整 action/key 名稱，必要時只需改單一位置。
- [固定底部列可能被系統 inset 或鍵盤影響] → 使用 `adjustNothing` 與 Activity 固定容器版面，讓內容區單獨處理 inset。
- [未設定 Base URL 或 HMAC key 時功能無法使用] → 在 Splash/Login 階段及 API 呼叫前提供可辨識錯誤訊息，避免靜默失敗。
- [API envelope 中 data 型別多樣] → 針對布林、物件、陣列建立明確 model，必要時使用泛型包裝與對應 DTO。
- [Android 9 相容性] → minSdk 降至 28，避免使用 API 29+ 限定行為，必要功能以 AndroidX 實作。

## Migration Plan

- 將 `minSdk` 從 29 調整為 28。
- 新增 Fragment、ViewModel、Repository、network 與 util 套件結構。
- 導入必要依賴後完成 UI 版面與 API 串接。
- 於 `local.properties` 提供 `API_BASE_URL`、`PDT_HMAC_KEY` 後進行實機驗證。
- 若需回滾，可移除新流程並保留原始單一 Activity 骨架。

## Open Questions

- 「新竹退件」對應的 `sourceType` 名稱是否固定為完整中文字串，或需以後端 id 判斷。
- DT 40 實機最終使用的掃描廣播 action 與 extra key 是否與通用範例一致。
- 版本不一致但非強制更新時，更新提示文案是否需要額外提供「稍後再說」以外的導流行為。