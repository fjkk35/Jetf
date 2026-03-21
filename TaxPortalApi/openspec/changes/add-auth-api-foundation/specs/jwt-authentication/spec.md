## ADDED Requirements

### Requirement: 使用者登入取得 JWT Token
系統 MUST 提供 POST /api/auth/login 端點，接收帳號與密碼，並以資料庫 dbo.TaxPortalUser 的資料驗證使用者。當帳號密碼正確時，系統 MUST 回傳 ApiResponse<string>，其 Data 欄位包含 Bearer Token，且 Token 有效期為 30 分鐘。

#### Scenario: 登入成功並取得 Token
- **WHEN** 用戶提供存在於 dbo.TaxPortalUser 的正確帳號與密碼呼叫 POST /api/auth/login
- **THEN** 系統回傳 HTTP 200 與 ApiResponse<string>，其中 isSuccess 為 true、code 為 200，且 data 包含 JWT Token 字串

#### Scenario: 帳號或密碼錯誤
- **WHEN** 用戶提供不存在的帳號或錯誤密碼呼叫 POST /api/auth/login
- **THEN** 系統回傳 HTTP 401 與 ApiResponse<string>，其中 isSuccess 為 false、errorCode 為 AUTH_001，且 data 為 null

### Requirement: 受保護 API 必須使用 Bearer Token 授權
所有受保護 API MUST 使用 ASP.NET Core 的 Bearer Token 認證機制，並明確標註 [Authorize] 屬性。當請求缺少 Token、Token 無效或 Token 過期時，系統 MUST 拒絕存取。

#### Scenario: 帶有效 Token 存取受保護 API
- **WHEN** 用戶在 Authorization Header 提供有效 Bearer Token 呼叫受保護 API
- **THEN** 系統允許請求進入控制器，並回傳該端點定義的 ApiResponse 結果

#### Scenario: 未帶 Token 存取受保護 API
- **WHEN** 用戶未提供 Authorization Header 或提供無效 Bearer Token 呼叫受保護 API
- **THEN** 系統回傳 HTTP 401，且回應內容符合 ApiResponse 統一格式