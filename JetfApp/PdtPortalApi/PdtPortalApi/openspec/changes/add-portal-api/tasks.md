## 1. Specification

- [x] 1.1 定義 Portal API 的共用回應格式
- [x] 1.2 定義四支 API 的請求、回應與錯誤處理
- [x] 1.3 定義 HMAC-SHA256 + Timestamp 驗證規則

## 2. Implementation

- [x] 2.1 導入 EF Core SQL Server 與 Scalar.AspNetCore 套件
- [x] 2.2 建立資料庫設定、查詢服務與回應模型
- [x] 2.3 實作登入、來源查詢、資料檢查與入庫寫入 API
- [x] 2.4 整合 OpenAPI 與 Scalar API UI
- [x] 2.5 驗證專案可編譯
- [x] 2.6 將查詢改為 EF Core LINQ
- [x] 2.7 為方法補齊 XML 註解、try-catch 與 log
- [x] 2.8 導入保留 7 天的檔案 log 機制
- [x] 2.9 為 model 類別補齊 XML 註解
- [x] 2.10 新增 App 版本檢查 API 與設定
- [x] 2.11 在 Login API 加入版本驗證