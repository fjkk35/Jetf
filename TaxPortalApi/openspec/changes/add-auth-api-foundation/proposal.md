## Why

目前 TaxPortalApi 僅具備 Swagger 與 Knife4j 的基礎文件輸出，尚未建立正式的 JWT 登入流程、統一 API 回應模型與受保護端點的授權規範。前端若要串接登入、統一錯誤處理與透過 Knife4j 測試授權 API，必須先補齊這些基礎契約。

## What Changes

- 新增 JWT Bearer 認證機制，從資料庫 dbo.TaxPortalUser 驗證使用者並簽發 30 分鐘有效期的 Token。
- 新增統一的 ApiResponse 回應模型，讓成功、失敗與例外回應維持一致格式。
- 新增 Auth Controller 的登入端點與至少一個受保護端點，明確使用 ActionResult<ApiResponse<T>> 作為回傳型別。
- 擴充 Swagger/Knife4j 設定，加入 Bearer Token 鎖頭設定與 XML 註解輸出。
- 補齊應用程式設定、資料存取與例外處理基礎設施，支撐後續受保護 API 開發。

## Capabilities

### New Capabilities
- `jwt-authentication`: 提供以資料庫使用者資料驗證帳密並簽發 JWT 的能力。
- `api-response-envelope`: 提供所有 API 一致的成功與失敗回應封裝能力。
- `authenticated-openapi-docs`: 提供 Swagger/Knife4j 對 JWT Bearer 的授權輸入與中文 XML 文件呈現能力。

### Modified Capabilities

None.

## Impact

- 影響後端設定檔、DI 註冊、Swagger 設定與 ASP.NET Core 認證授權管線。
- 新增資料模型、DbContext、服務層、控制器與例外處理元件。
- 新增 SQL Server 與 JWT 相關 NuGet 依賴。
- 對外新增 /api/auth/login 與受 JWT 保護的認證示範端點。