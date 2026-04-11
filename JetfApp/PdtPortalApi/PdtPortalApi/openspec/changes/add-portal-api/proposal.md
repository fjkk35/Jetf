## Why

Portal API 目前只有 ASP.NET Core 預設範本，尚未提供實際入庫整合能力，也缺少統一回應模型、資料庫存取與 API 文件入口。

## What Changes

- 新增共用 ApiResponse / ApiResponse<T> 回應模型，統一所有 API 回傳格式
- 導入 Entity Framework Core SQL Server，分別連接 jetf 與 DATA_CENTER 資料庫
- 新增登入、貨件來源查詢、原始入庫資料檢查、入庫寫入 API
- 新增 HMAC-SHA256 + Timestamp 驗證機制，保護入庫寫入 API
- 導入 Scalar.AspNetCore，提供 OpenAPI / API UI 文件
- 將所有查詢改為 EF Core LINQ，避免以 raw SQL 查詢業務資料
- 補齊方法 XML 註解、例外處理與檔案式 log
- 新增 Android App 版本檢查 API，並於 Login 強制驗證版本一致
- 補齊 model 類別與屬性的 XML 註解

## Impact

- 需要新增資料庫設定、HMAC 驗證設定與相關服務
- API 文件入口將從開發用 Swagger UI 改為 OpenAPI + Scalar
- 寫入入庫資料時，會依據海運/空運來源查詢補齊欄位並寫入 ShipmentInbound
- 將新增 EF 實體映射與檔案 log 保留策略，log 僅保留最近 7 天
- Login request 與版本設定將新增 App 版本欄位與設定項目