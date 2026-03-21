## ADDED Requirements

### Requirement: 所有 API 回應必須封裝為 ApiResponse
系統 MUST 提供統一的 ApiResponse 模型，至少包含 isSuccess、code、message、data、errorCode 與 timestamp 欄位。所有控制器成功與失敗回應 MUST 使用此模型包裝，以確保前端可用一致邏輯處理結果。

#### Scenario: 成功回應封裝資料
- **WHEN** 任一 API 成功處理請求
- **THEN** 系統回傳對應 HTTP 成功狀態碼，且 body 為 isSuccess=true 的 ApiResponse，其中 data 包含實際回傳資料

#### Scenario: 失敗回應封裝錯誤
- **WHEN** 任一 API 因驗證失敗、授權失敗或業務錯誤而無法完成請求
- **THEN** 系統回傳對應 HTTP 失敗狀態碼，且 body 為 isSuccess=false 的 ApiResponse，其中 message 與 errorCode 提供可供前端判斷的錯誤資訊

### Requirement: 未預期例外必須回傳統一格式
系統 MUST 以全域方式攔截未處理例外，並轉換成 ApiResponse 失敗格式，以避免前端收到不一致的錯誤內容。

#### Scenario: 伺服器發生未處理例外
- **WHEN** API 執行過程拋出未被處理的例外
- **THEN** 系統回傳 HTTP 500 與 ApiResponse，其中 isSuccess 為 false、errorCode 為 COMMON_500，且 data 為 null