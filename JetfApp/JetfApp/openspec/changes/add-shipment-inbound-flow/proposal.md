## Why

現有專案仍是 Android 預設骨架，尚未提供 DT 40 手持式裝置可用的入庫作業流程、版本檢查、登入與 API 串接能力。倉儲現場需要在 Android 10 開發環境下交付一套能相容 Android 9 的固定流程介面，並支援掃描與快速按鍵操作。

## What Changes

- 新增符合 DT 40 使用情境的 APP 啟動畫面，顯示 Logo、版本號，並在進入系統前呼叫版本檢查 API。
- 新增登入、主選單、入庫設定、入庫作業四個核心畫面與固定底部功能鍵規則。
- 新增入庫來源查詢、單號檢查、入庫寫入等 API 串接，包含共用回應格式解析與 HMAC-SHA256 簽章。
- 新增入庫流程狀態管理，處理儲位鎖定、條碼掃描/輸入、退件欄位顯示、流水號遞增與上限提示。
- 調整 Android 相容性設定與資源，確保 Android 10 開發、Android 9 裝置可執行。

## Capabilities

### New Capabilities
- `device-optimized-app-shell`: 定義 DT 40 啟動畫面、版本檢查、固定底部功能鍵與 Android 9 相容的全域 UI 規則。
- `shipment-inbound-workflow`: 定義登入、選單、入庫設定、入庫作業與相關 API 驗證/寫入流程。

### Modified Capabilities

無。

## Impact

- Android app module 的 Activity、Fragment、ViewModel、Repository、網路層、掃描整合與版面資源。
- 建置設定需支援 minSdk 28，並提供 Base URL 與 HMAC 金鑰的安全注入方式。
- 需串接 `/api/app/version-check`、`/api/auth/login`、`/api/shipmentinbound/source-types`、`/api/shipmentinbound/check`、`/api/shipmentinbound`。