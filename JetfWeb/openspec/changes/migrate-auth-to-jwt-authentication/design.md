## Overview

此變更要將目前以 Session 為核心的登入驗證，調整為以 JWT 為核心、Claims 為主體、Session 為過渡相容層的架構。考量目前專案是 ASP.NET MVC 5 網頁系統，而非單頁應用，因此不建議把 Token 放在 localStorage 後由前端自行拼 Authorization Header；那會迫使大量 Razor 頁面、表單與既有 AJAX 寫法一起改造。較務實的作法是由後端簽發 JWT，放入 HttpOnly Cookie，並在 OWIN 管線驗證後建立使用者 Claims。

## Assessment

- 可行性：高。專案已具備 OWIN 啟動點，且執行環境已含 Microsoft.Owin 與 JWT 相關組件。
- 風險：中高。程式內仍有大量直接讀取 Session 使用者資訊的寫法，若不保留相容層，切換當天就會造成大面積功能失效。
- 建議路線：先完成 JWT 驗證骨架與 Session 橋接，再逐步將 Controller/View 改為讀取 Claims 或統一的 CurrentUserContext。

## Token Strategy

### Access Token

- 載體：HttpOnly、Secure、SameSite=Lax Cookie。
- 建議名稱：JETF_AT。
- 用途：每次請求驗證身分與權限。
- 時效：30 分鐘。

### Refresh Token

- 載體：HttpOnly、Secure、SameSite=Lax Cookie。
- 建議名稱：JETF_RT。
- 用途：Access Token 過期時換發新 Token。
- 時效：480 分鐘，對齊現行 Session timeout 的使用習慣。
- 需支援 Rotation 與撤銷，避免單一長效 Token 長期有效。

### Why Cookie-Based JWT

- 系統主要入口仍是 Razor 頁面，不是純 API。
- Layout 與多數頁面目前預期伺服器在請求時就知道登入者資訊。
- 使用 HttpOnly Cookie 可避免前端程式直接讀取 Token，降低 XSS 竊取風險。
- 相較 pure Bearer header 模式，可用更小的改動維持現有頁面導航與檔案下載流程。

## Claim Design

JWT 至少包含以下 Claims：

- sub：使用者帳號或唯一識別碼，對應既有 user_id。
- name：使用者名稱，對應既有 user_name。
- partner：多值 Claim，對應既有 user_partner。
- authority：多值 Claim，對應既有 user_auth。
- jti：唯一 Token 編號，用於追蹤與撤銷。
- iat：簽發時間。
- exp：到期時間。
- iss：簽發者。
- aud：受眾。

partner 與 authority 應維持與目前 AccountService.GetAuthority 回傳資料一致，避免切換後選單顯示與授權結果改變。

## Request Pipeline Design

### Startup.cs

- 在 OWIN Startup 中加入 JWT 驗證中介層。
- 驗證成功後建立 ClaimsIdentity / ClaimsPrincipal。
- 將 HttpContext.User 與 Thread.CurrentPrincipal 統一到 JWT 驗證後的 Principal。

### Global.asax.cs

- Application_AcquireRequestState 不再把 Session 視為唯一來源，而是優先讀取 ClaimsPrincipal。
- 在遷移期間，若 Claims 已存在且目前 Handler 具 SessionState，則回填以下 Session 鍵值：
  - user_id
  - user_name
  - user_partner
  - user_auth
- 這個橋接僅作為過渡相容層，長期目標是拔除對這些 Session 鍵值的依賴。

## Login Flow

### Current Flow

- AccountController.Login 驗證驗證碼、帳密。
- 驗證成功後直接寫入 Session。

### Target Flow

- 保留現有驗證碼與帳密檢查。
- 驗證成功後組裝 Claims 資料。
- 產生 Access Token 與 Refresh Token。
- 將兩個 Token 寫入 Cookie。
- 不再以登入成功為主體去寫 Session；Session 僅由後續請求中的相容橋接補齊。
- Login API 仍回傳既有成功/失敗 JSON 結構，避免前端登入頁一起大改。

## Refresh And Logout

### Refresh

- 提供 POST /Account/Refresh 或等效端點。
- 當 Access Token 過期但 Refresh Token 仍有效時，系統可重新簽發新的 Access Token 與 Refresh Token。
- Refresh Token 每次使用後必須旋轉，舊 Token 立即失效。

### Logout

- 提供登出動作，清除 JWT Cookies。
- 若存在 Refresh Token 儲存表，登出時必須撤銷目前 Refresh Token。
- 登出後再次請求受保護頁面時，系統必須視為未登入。

## Authorization Design

### UserAuthorizeAttribute

- 改為從 ClaimsPrincipal 讀取 authority Claims，而非 Session["user_auth"]。
- 未登入與無權限要分開處理：
  - 未登入：頁面請求導向登入頁；AJAX 回傳 401 與登入導向資訊。
  - 已登入但無權限：頁面請求導向 Home/Index 或既有允許頁；AJAX 回傳 403。

### LoginFilter

- 改為檢查目前 Principal 是否已驗證，不再直接讀 Session。

## UI And Layout Compatibility

- Shared Layout 與需要顯示使用者名稱、夥伴別、權限的 View，應逐步改為讀取 Claims 或統一的 ViewModel / Helper。
- 遷移初期可先靠 Session 橋接保持原行為，後續再分批拔除 View 內對 Session 的直接依賴。

## Security Design

### Signing Key

- JWT Signing Key 不得硬編碼在原始碼內。
- 金鑰應儲存在 Web.config appSettings 或部署環境設定，並支援更換。

### Cookie Policy

- 正式環境必須啟用 Secure。
- Cookie 必須設定 HttpOnly。
- SameSite 預設使用 Lax；若跨站整合需求明確，再另外評估調整。

### CSRF

- 因本方案使用 Cookie 承載驗證資訊，所有修改資料的 POST/PUT/DELETE 行為都必須納入 CSRF 防護設計。
- 初版至少要在新增或調整過的登入、登出、Refresh、關鍵寫入操作上套用 Anti-Forgery 或等效防護。

## Migration Plan

### Phase 1: 建立 JWT 骨架

- 加入 Token 簽發、驗證、Refresh、撤銷能力。
- Login/Logout 改為 JWT 模式。
- UserAuthorizeAttribute 改讀 Claims。

### Phase 2: 加入相容橋接

- 在每個已驗證請求中由 Claims 回填舊 Session 鍵值。
- 讓未改寫的 Controller/View 仍可運作。

### Phase 3: 分批移除 Session 依賴

- Controller 改為透過共用 Base Controller 或 CurrentUserContext 取得目前使用者。
- View 改為從 Claims 或 ViewModel 取值。
- 清理 Session user_* 讀取與寫入。

### Phase 4: 移除橋接

- 當所有受影響功能已完成改寫與驗證後，移除 Session 回填邏輯。

## Data And Persistence

若採 Refresh Token 機制，需新增儲存欄位或資料表，至少包含：

- UserId
- TokenId 或 TokenHash
- ExpiresAt
- CreatedAt
- RevokedAt
- ReplacedByTokenId
- ClientIp 或裝置資訊（若需要稽核）

Refresh Token 應儲存雜湊值而非明文。

## Error Handling

- Token 缺失或驗證失敗：視為未登入。
- Token 過期但可 Refresh：前端可先導向 Refresh 流程，再重試原請求。
- Refresh Token 無效、過期或已撤銷：清空 Cookies，要求重新登入。
- AJAX 若收到 401，回應格式需與既有前端可理解的 Redirect 資訊相容。

## Non-goals

- 不在本變更中把整個系統改為前後端分離 SPA。
- 不在本變更中一次重寫所有 Controller 與 View 的使用者資訊取得方式。
- 不在本變更中處理外部第三方單點登入整合。