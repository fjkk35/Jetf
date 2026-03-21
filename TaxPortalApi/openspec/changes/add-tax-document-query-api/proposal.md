## Why

目前 TaxPortalApi 雖然已具備 JWT 登入基礎，但尚未提供「依稅單號碼查詢並下載稅金單 PDF」的正式 API。現行需求涉及跨資料庫查詢、登入使用者與客戶代碼綁定驗證，以及從 FTP 下載實體 PDF 檔案，若沒有明確的契約與技術設計，後續前端與外部系統將難以穩定串接。

## What Changes

- 新增受 JWT 保護的稅金單查詢 API，依 TaxNumber 取得對應 PDF 並直接回傳 PDF 檔案內容。
- 新增 DATA_CENTER 專用 DbContext，使用 EF Core 查詢 CLEARANCE_TAX、ORIGINALLIST 與 SEA_ORDER_ORIGINAL。
- 擴充 jetf DbContext，查詢 TaxPortalCustomer 與 Clearance_Tax_Pdf，以登入者與 CustCode 驗證稅單存取資格。
- 調整 JWT 簽發內容，將使用者 Id 寫入 claim，避免後續 API 再回查使用者資料。
- 新增 FluentFTP 下載流程與相關設定，負責根據資料庫中的 FilePath 抓取 PDF 檔案。

## Capabilities

### New Capabilities
- tax-document-query: 提供已授權使用者依稅單號碼查詢並下載本人可存取的稅金單 PDF。

### Modified Capabilities
- jwt-authentication: JWT Token 需攜帶使用者識別 claim，供受保護 API 直接使用。

## Impact

- 影響 ASP.NET Core DI 設定、DbContext 註冊、JWT claim 內容與受保護控制器。
- 新增 DATA_CENTER 與 FTP 設定節點。
- 新增 TaxDocumentsController、TaxDocumentService 與相關查詢模型。
- 新增 FluentFTP 套件依賴。