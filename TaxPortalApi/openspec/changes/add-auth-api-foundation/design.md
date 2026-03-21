## Context

TaxPortalApi 目前僅有最小化的 ASP.NET Core 啟動程式與 Swagger/Knife4j 文件設定，尚未註冊資料庫連線、JWT 認證、控制器契約模型與錯誤處理。此次變更屬於跨設定、資料存取、服務層、控制器與 OpenAPI 文件的橫切性基礎建設，且涉及安全性設計，因此需要先明確定義技術決策。

## Goals / Non-Goals

**Goals:**
- 建立以 SQL Server dbo.TaxPortalUser 為來源的登入驗證流程。
- 以 JWT Bearer 作為唯一認證方式，預設有效期為 30 分鐘。
- 所有控制器回應統一包裝為 ApiResponse，包含成功、驗證失敗與未預期例外。
- Swagger/Knife4j 能直接輸入 Bearer Token 測試受保護 API。
- 所有新增 API 透過 XML 註解輸出中文摘要文件。

**Non-Goals:**
- 不在此次變更中實作註冊、刷新 Token、角色權限或多因素驗證。
- 不在此次變更中建立完整使用者管理 CRUD。
- 不處理既有明文密碼資料遷移；資料表預期已採雜湊密碼儲存。

## Decisions

### 使用 EF Core DbContext 直接存取 dbo.TaxPortalUser
以 EF Core 搭配 SQL Server provider 建立最小可用的 DbContext 與實體映射，符合專案既定技術棧，也便於後續擴充 Repository 或其他資料表。相較於直接 ADO.NET，此做法能減少樣板碼並維持一致性。

### 以 BCrypt 驗證密碼雜湊
登入流程採用 BCrypt 驗證資料庫中的密碼雜湊值，不接受明文比對。相較於自製加密邏輯，BCrypt 已具備成熟的單向雜湊驗證流程，較能符合密碼安全要求。

### 以服務層封裝登入與 Token 簽發邏輯
控制器僅負責接收請求與回應，帳密驗證與 JWT 產生分別放入 AuthService 與 JwtTokenService，避免控制器混入業務邏輯，也讓後續測試與擴充較容易。

### 以全域例外處理中介軟體統一失敗回應
已知的登入錯誤由服務層以領域例外回傳明確錯誤碼；未預期錯誤則由中介軟體轉成 ApiResponse，避免回應格式不一致。相較於在每個控制器重複 try/catch，可降低重複碼。

### 在 SwaggerGen 設定全域 Bearer Security Requirement
OpenAPI 文件以 ApiKey 型式的 Authorization Header 定義 Bearer Token，並全域套用 Security Requirement，讓 Swagger 與 Knife4j 都能直接啟用鎖頭授權。此方式相容於目前使用的 Swashbuckle 與 Knife4jUI 套件。

## Risks / Trade-offs

- [資料庫尚未建立 TaxPortalUser 或結構不同] → 以明確 Entity Mapping 指向 dbo.TaxPortalUser，若資料庫尚未同步，啟動後會在登入時暴露可診斷錯誤。
- [BCrypt 與現有資料庫雜湊格式不一致] → 先採用標準 BCrypt；若既有資料使用其他演算法，需後續補充相容策略。
- [所有失敗都包裝為 200 會破壞前端語意] → 保留 HTTP 狀態碼，同時在 body 內統一提供 ApiResponse 欄位。
- [JWT Key 設定過短導致安全性不足] → 於設定綁定時檢查最小長度並在啟動時失敗，避免弱金鑰上線。

## Migration Plan

1. 在 appsettings 中加入 ConnectionStrings 與 Jwt 設定。
2. 部署前確認 SQL Server 已存在 dbo.TaxPortalUser，且 Password 欄位內容可由 BCrypt 驗證。
3. 發布後先以 /api/auth/login 驗證登入，再用取得的 Token 測試受保護端點與 Knife4j 授權流程。
4. 若需回滾，可移除本次新增的認證註冊與控制器，恢復到原始無登入狀態。

## Open Questions

- 目前 Id 欄位規格允許 int 或 guid；此次實作以字串化 claims 儲存，避免在 JWT 層綁死單一型別。
- 若後續需要角色授權，需再擴充資料表欄位與 claims 組成。