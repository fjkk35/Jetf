## ADDED Requirements

### Requirement: 已授權使用者可依稅單號碼下載 PDF
系統 MUST 提供受 JWT 保護的稅金單查詢 API，讓已登入使用者可透過 TaxNumber 查詢並下載 PDF。API MUST 先依 DATA_CENTER.dbo.CLEARANCE_TAX 取得 DATA_TYPE 與 MERGE_NUMBER，再依資料類型查出 CustCode，並使用目前登入者的 UserId 與 jetf.dbo.TaxPortalCustomer 驗證是否有權限存取。驗證成功後，系統 MUST 從 jetf.dbo.Clearance_Tax_Pdf 找出對應檔案路徑，並自 FTP 下載後以 application/pdf 回傳。

#### Scenario: FTZ 或 TACT 類型稅單下載成功
- **GIVEN** 使用者已帶有效 JWT，且 Token 內含有效 UserId claim
- **AND** DATA_CENTER.dbo.CLEARANCE_TAX 可依 TaxNumber 找到資料，且 DATA_TYPE 為 FTZ 或 TACT
- **AND** DATA_CENTER.dbo.ORIGINALLIST 可依 MERGE_NUMBER 對應的 DELIVERYNO 找到 DESPATCHNO
- **AND** jetf.dbo.TaxPortalCustomer 存在 TaxPortalUserId 與 CustCode 的對應資料
- **AND** jetf.dbo.Clearance_Tax_Pdf 與 FTP 上都存在對應 PDF
- **WHEN** 使用者呼叫稅金單查詢 API
- **THEN** 系統回傳 HTTP 200
- **AND** Content-Type 為 application/pdf
- **AND** 回應內容為該稅金單的 PDF 檔案位元內容

#### Scenario: 非 FTZ 或 TACT 類型稅單下載成功
- **GIVEN** 使用者已帶有效 JWT，且 Token 內含有效 UserId claim
- **AND** DATA_CENTER.dbo.CLEARANCE_TAX 可依 TaxNumber 找到資料，且 DATA_TYPE 不為 FTZ 或 TACT
- **AND** DATA_CENTER.dbo.SEA_ORDER_ORIGINAL 可依 MERGE_NUMBER 對應的 JETF_SERIAL 找到 DESPATCH_NAME
- **AND** jetf.dbo.TaxPortalCustomer、jetf.dbo.Clearance_Tax_Pdf 與 FTP 都存在對應資料
- **WHEN** 使用者呼叫稅金單查詢 API
- **THEN** 系統回傳 HTTP 200 與 PDF 檔案內容

#### Scenario: 使用者未綁定對應 CustCode
- **GIVEN** 稅單資料與 CustCode 存在
- **AND** 目前登入者的 UserId 在 jetf.dbo.TaxPortalCustomer 查無對應 CustCode
- **WHEN** 使用者呼叫稅金單查詢 API
- **THEN** 系統回傳 HTTP 404
- **AND** 回應內容符合 ApiResponse 統一錯誤格式

#### Scenario: 找不到稅單或 PDF 檔案
- **GIVEN** 使用者已帶有效 JWT
- **WHEN** CLEARANCE_TAX、CustCode、Clearance_Tax_Pdf 或 FTP 檔案任一環節查無資料
- **THEN** 系統回傳 HTTP 404
- **AND** 回應內容符合 ApiResponse 統一錯誤格式

#### Scenario: 未帶有效 Token 呼叫 API
- **WHEN** 使用者未提供 Authorization Header 或提供無效 Bearer Token 呼叫稅金單查詢 API
- **THEN** 系統回傳 HTTP 401
- **AND** 回應內容符合 ApiResponse 統一錯誤格式

### Requirement: JWT 必須攜帶可直接取用的 UserId claim
系統 MUST 在登入成功後簽發的 JWT 中包含使用者識別 claim，讓受保護 API 可以直接從 ClaimsPrincipal 取得目前登入者 UserId，而不需要再次查詢 TaxPortalUser。

#### Scenario: 登入成功後 Token 含有 UserId claim
- **WHEN** 使用者成功登入並取得 JWT
- **THEN** Token 內含使用者名稱 claim
- **AND** Token 內含可解析為數值的 UserId claim
- **AND** 受保護 API 可直接使用該 claim 作為 TaxPortalCustomer 查詢條件