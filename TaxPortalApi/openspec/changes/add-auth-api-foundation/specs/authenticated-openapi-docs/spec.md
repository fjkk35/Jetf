## ADDED Requirements

### Requirement: OpenAPI 文件必須支援 Bearer Token 授權
系統 MUST 在 Swagger/Knife4j 的 OpenAPI 文件中定義 Bearer 認證方案，讓使用者可於介面中輸入 Authorization: Bearer {token} 後測試受保護 API。此認證方案 MUST 全域套用至文件中需要授權的 API。

#### Scenario: 文件介面顯示 Bearer 授權輸入
- **WHEN** 使用者開啟 Swagger 或 Knife4j 文件頁面
- **THEN** 介面提供 Bearer Token 授權輸入，且授權後可帶入後續 API 測試請求

### Requirement: OpenAPI 文件必須呈現中文 XML 摘要
系統 MUST 啟用 XML 文件輸出並在 OpenAPI 產生器中載入 XML 註解，使控制器與 API 方法的中文 summary 能顯示於 Swagger/Knife4j。

#### Scenario: 控制器摘要顯示於文件中
- **WHEN** 控制器與 API 方法包含 XML summary 註解且系統成功載入 XML 文件
- **THEN** Swagger 與 Knife4j 文件頁面顯示對應的中文摘要內容