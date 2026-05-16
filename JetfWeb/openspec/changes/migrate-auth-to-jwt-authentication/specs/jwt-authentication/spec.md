## ADDED Requirements

### Requirement: 系統必須在登入成功後簽發 JWT 驗證憑證
系統 MUST 在使用者通過驗證碼與帳密驗證後，簽發 JWT Access Token，並以 HttpOnly Cookie 保存。若系統採用續期機制，系統 MUST 同時簽發 Refresh Token。JWT 內容 MUST 包含識別目前使用者與權限所需的 Claims，至少包含 user_id、user_name、partner 與 authority 對應資料。

#### Scenario: 使用者登入成功
- **GIVEN** 使用者輸入正確驗證碼、帳號與密碼
- **WHEN** 使用者送出登入
- **THEN** 系統簽發 JWT Access Token
- **AND** 系統以 HttpOnly Cookie 回傳 Token
- **AND** Token Claims 包含使用者代號、名稱、夥伴別與權限
- **AND** 登入 API 仍回傳成功結果供既有前端流程判斷

#### Scenario: 驗證碼錯誤或帳密錯誤
- **WHEN** 使用者輸入錯誤驗證碼或錯誤帳密
- **THEN** 系統 MUST 不簽發任何 JWT
- **AND** 系統回傳既有錯誤訊息

### Requirement: 系統必須在每個已驗證請求中由 JWT 重建目前使用者 Claims
系統 MUST 在每個帶有有效 JWT 的請求中驗證 Token，並建立 ClaimsPrincipal 作為目前使用者內容來源。後續授權、使用者資訊顯示與記錄 MUST 以 ClaimsPrincipal 為主，而非登入當下寫入的 Session。

#### Scenario: 請求帶有有效 JWT
- **GIVEN** 使用者瀏覽器帶有有效的 JWT Cookie
- **WHEN** 使用者發出受保護請求
- **THEN** 系統驗證 JWT 簽章、到期時間、Issuer 與 Audience
- **AND** 系統建立 ClaimsPrincipal
- **AND** 後續授權判斷以該 ClaimsPrincipal 為依據

#### Scenario: JWT 無效或過期
- **GIVEN** 使用者請求帶有無效、過期或遭竄改的 JWT
- **WHEN** 系統驗證 Token
- **THEN** 系統 MUST 將該請求視為未登入
- **AND** 系統 MUST 不建立已驗證的 ClaimsPrincipal

### Requirement: 系統必須在遷移期間維持既有 user_* Session 相容性
在所有 Controller 與 View 完成 Claims 化之前，系統 MUST 在已驗證請求進入具 SessionState 的處理流程時，將 Claims 中的使用者資訊回填到 user_id、user_name、user_partner、user_auth 等既有 Session 鍵值，確保未完成改寫的功能仍可運作。

#### Scenario: 舊 Controller 仍讀取 Session user_id
- **GIVEN** 某功能尚未改寫，仍直接讀取 Session["user_id"]
- **AND** 使用者攜帶有效 JWT
- **WHEN** 請求進入該功能
- **THEN** 系統 MUST 先將 Claims 的使用者代號回填到 Session["user_id"]
- **AND** 舊功能可依既有方式取得目前登入者

#### Scenario: 已無有效 JWT
- **GIVEN** 使用者沒有有效 JWT
- **WHEN** 系統進入需要使用者資訊的請求
- **THEN** 系統 MUST 不回填 user_* Session
- **AND** 後續流程需依未登入狀態處理

### Requirement: 系統必須以 Claims 進行權限判斷
系統 MUST 以 JWT 內的 authority Claims 取代 Session["user_auth"] 作為授權判斷基礎。所有使用 UserAuthorizeAttribute 或等效授權邏輯的功能 MUST 依 Claims 內的權限值判定是否允許存取。

#### Scenario: 使用者具有要求權限
- **GIVEN** 使用者的 JWT authority Claims 包含某功能要求的 Authority 值
- **WHEN** 使用者請求該功能
- **THEN** 系統 MUST 允許請求通過

#### Scenario: 使用者已登入但缺少權限
- **GIVEN** 使用者已登入且 JWT 有效
- **AND** authority Claims 不包含某功能要求的 Authority 值
- **WHEN** 使用者請求該功能
- **THEN** 系統 MUST 將該請求視為已登入但無權限
- **AND** 頁面請求與 AJAX 請求需依無權限規則分別處理

### Requirement: 系統必須區分未登入與無權限的回應
系統 MUST 區分未登入與無權限兩種狀態。未登入時，頁面請求 MUST 導向登入頁，AJAX 請求 MUST 回傳 401 與前端可處理的登入導向資訊；已登入但無權限時，AJAX 請求 MUST 回傳 403，頁面請求 MUST 導向既有允許頁或無權限頁。

#### Scenario: 頁面請求未登入
- **GIVEN** 使用者沒有有效 JWT
- **WHEN** 使用者直接瀏覽受保護頁面
- **THEN** 系統 MUST 導向登入頁

#### Scenario: AJAX 請求未登入
- **GIVEN** 使用者沒有有效 JWT
- **WHEN** 使用者發出 AJAX 請求到受保護端點
- **THEN** 系統 MUST 回傳 401
- **AND** 回應內容包含前端可用的 Redirect 資訊

#### Scenario: AJAX 請求已登入但無權限
- **GIVEN** 使用者 JWT 有效但缺少所需 authority
- **WHEN** 使用者發出 AJAX 請求到受保護端點
- **THEN** 系統 MUST 回傳 403

### Requirement: 系統必須支援 Token 續期與登出失效
若系統採用 Access Token 與 Refresh Token 雙 Token 模式，系統 MUST 支援 Refresh Token 換發新 Token，且 Refresh Token MUST 在每次續期後旋轉。系統 MUST 在登出時清除 JWT Cookies，並使目前 Refresh Token 失效。

#### Scenario: Access Token 過期但 Refresh Token 仍有效
- **GIVEN** 使用者的 Access Token 已過期
- **AND** Refresh Token 仍有效且未撤銷
- **WHEN** 使用者執行續期流程
- **THEN** 系統 MUST 簽發新的 Access Token
- **AND** 系統 MUST 旋轉為新的 Refresh Token
- **AND** 舊 Refresh Token MUST 立即失效

#### Scenario: 使用者登出
- **GIVEN** 使用者目前處於已登入狀態
- **WHEN** 使用者執行登出
- **THEN** 系統 MUST 清除 JWT Cookies
- **AND** 系統 MUST 使目前 Refresh Token 失效
- **AND** 後續請求必須視為未登入

### Requirement: 系統必須安全保存與驗證 JWT
系統 MUST 使用伺服器端設定提供的簽章金鑰來簽發與驗證 JWT，且不得將金鑰硬編碼於程式碼。正式環境中的 JWT Cookie MUST 啟用 HttpOnly 與 Secure 屬性。

#### Scenario: 正式環境簽發 Token
- **WHEN** 系統於正式環境簽發 JWT Cookie
- **THEN** Cookie MUST 設定 HttpOnly
- **AND** Cookie MUST 設定 Secure

#### Scenario: 金鑰設定缺失
- **GIVEN** 系統缺少 JWT 簽章金鑰設定
- **WHEN** 系統嘗試啟動或簽發 Token
- **THEN** 系統 MUST 拒絕進入可正常登入的狀態
- **AND** 系統 MUST 記錄明確錯誤