## 1. JWT 驗證骨架

- [ ] 1.1 盤點並確認 AccountController、Startup.cs、Global.asax.cs、Shared Layout 與自訂授權屬性的改動邊界
- [ ] 1.2 新增 JWT 簽發、驗證與 Claims 組裝服務
- [ ] 1.3 在 OWIN Startup 加入 JWT 驗證中介層與必要設定
- [ ] 1.4 將 JWT Signing Key 與 Issuer/Audience 移出程式碼，改由設定檔或部署環境提供

## 2. 登入、續期與登出

- [ ] 2.1 調整 AccountController.Login，保留驗證碼與帳密檢查，但改為簽發 JWT Cookies
- [ ] 2.2 新增 Refresh Token 發放、旋轉與撤銷機制
- [ ] 2.3 新增或調整續期端點，讓 Access Token 過期時可重新換發
- [ ] 2.4 調整 LogOff，清除 JWT Cookies 並撤銷 Refresh Token

## 3. 授權與相容橋接

- [ ] 3.1 將 UserAuthorizeAttribute 改為讀取 authority Claims
- [ ] 3.2 將 LoginFilter 改為讀取 Principal 驗證狀態
- [ ] 3.3 在 Global.asax.cs 加入 Claims 到 user_* Session 的過渡橋接
- [ ] 3.4 統一頁面請求與 AJAX 在 401/403/Token 過期時的回應格式

## 4. 漸進式移除 Session 相依

- [ ] 4.1 新增統一的目前使用者存取方式，例如 Base Controller、Helper 或 CurrentUserContext
- [ ] 4.2 分批將高風險 Controller 改為使用 Claims/CurrentUserContext，而非直接讀取 Session
- [ ] 4.3 分批將 Shared Layout 與主要 View 改為使用 Claims 或 ViewModel，而非直接讀取 Session
- [ ] 4.4 盤點並清除登入成功時直接寫入 Session 的程式碼

## 5. 安全與驗證

- [ ] 5.1 驗證登入成功後 Cookie 屬性包含 HttpOnly、Secure、SameSite
- [ ] 5.2 驗證頁面請求在未登入、已過期、無權限時的導向與訊息
- [ ] 5.3 驗證 AJAX 請求在 401 與 403 時的 JSON 回應符合前端預期
- [ ] 5.4 驗證 Refresh Token 旋轉、重放拒絕與登出撤銷行為
- [ ] 5.5 驗證舊功能在 Session 橋接期間仍可正常取得 user_id、user_name、user_partner、user_auth