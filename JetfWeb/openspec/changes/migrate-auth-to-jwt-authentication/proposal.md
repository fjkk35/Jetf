## Why

目前專案的登入驗證仍以 Session 為核心。登入成功後由 AccountController 將 user_id、user_name、user_partner、user_auth 寫入 Session，而授權判斷、版面顯示與大量 Controller 行為都直接讀取這些 Session 欄位。這種做法在 IIS App Pool recycle、Session 遺失、跨程序部署與 AJAX 未登入處理上都較脆弱，也不利於後續導入更一致的 Claims-based 驗證模型。

評估目前程式結構後，直接改成前端自行保存 Bearer Token 的純 SPA 作法成本過高，因為本系統仍以 ASP.NET MVC 頁面、Razor Layout、jQuery AJAX 與多數傳統表單流程為主。較可行的路線是改為由伺服器簽發 JWT，並透過 HttpOnly Cookie 承載，再由 OWIN 在每個請求驗證 JWT 並重建 ClaimsPrincipal，同時在遷移期間保留 Session 相容橋接，逐步拔除舊程式對 Session 的依賴。

## What Changes

- 將登入成功後的驗證資料由 Session 改為 JWT Access Token 與 Refresh Token。
- 在 OWIN 啟動流程中加入 JWT 驗證，於每次請求建立 ClaimsPrincipal。
- 重寫 UserAuthorizeAttribute 與未登入判斷流程，改以 Claims 而非 Session 做授權。
- 在遷移期間保留 user_id、user_name、user_partner、user_auth 的 Session 相容橋接，避免一次改動近百處程式碼。
- 統一瀏覽器頁面與 AJAX 對未登入、無權限、Token 過期的回應行為。
- 新增 Token 續期與登出撤銷機制，降低長時效 JWT 無法即時失效的風險。

## Capabilities

### New Capabilities
- jwt-authentication: 系統可使用 JWT 作為登入後的主要驗證憑證，並以 Claims 驅動權限判斷與使用者內容取得。

## Impact

- 影響登入流程、登出流程、授權屬性、Global.asax 要求的目前使用者資訊、OWIN 啟動設定與 Web.config 安全設定。
- 影響 AccountController、App_Start/Startup.cs、Global.asax.cs、Shared Layout 與所有直接讀取 Session 使用者資訊的 Controller/View。
- 目前工作區內存在大量直接讀取 Session["user_id"]、Session["user_auth"] 等寫法，因此本變更必須採漸進式遷移，而非一次移除所有 Session 依賴。
- 若採用 Refresh Token 機制，需新增 Refresh Token 儲存與撤銷資料結構。
- 初版以站內 MVC 網頁與 jQuery AJAX 為目標，不將系統改造成獨立前後端分離 SPA。