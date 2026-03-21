## 1. 規格與設定

- [x] 1.1 建立 add-tax-document-query-api 的 proposal、design、specs 與任務文件
- [x] 1.2 在設定檔與程式啟動流程中加入 DATA_CENTER 連線字串與 FTP 設定綁定

## 2. 資料存取與認證

- [x] 2.1 擴充 JWT 簽發邏輯，將 UserId 寫入 claim，並讓受保護 API 可直接取用
- [x] 2.2 新增 jetf 與 DATA_CENTER 所需的 EF Core 實體與 DbContext 查詢模型

## 3. 稅金單查詢 API

- [x] 3.1 實作稅金單服務，依 TaxNumber 完成跨資料庫 CustCode 查詢、使用者關聯驗證與 PDF 路徑解析
- [x] 3.2 使用 FluentFTP 下載 PDF，並新增受保護控制器直接回傳 application/pdf

## 4. 驗證

- [x] 4.1 執行 dotnet build 驗證專案可成功編譯
- [x] 4.2 執行 openspec status 驗證 change artifacts 已齊備