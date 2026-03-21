# TaxPortalApi 專案 Copilot 指令

## 專案上下文
這是一個基於 .NET 8 的多專案 Web API 解決方案（TaxPortalApi）。
終端機使用 powershell

## 技術架構要求
- 使用 .NET 8 和 C# 12
- EFCore ORM
- JWT Token 認證機制
- Repository 設計模式
- 依賴注入模式
- 異步編程（async/await）
- 採用三層式架構（Clean Architecture）
- SQL Server
- Docker 容器化

## 代碼風格和約定
- 遵循 C# 命名慣例
- 遵循 SOLID 原則
- 使用版本化 API 路由（v1/v2），控制器仍置於 Controllers
- RESTful API 設計
- 統一的錯誤處理模式
- 使用繁體中文寫註解
- 命名空間需與資料夾結構一致

## 專案結構約定
### 三層式架構（Clean Architecture）分層
- 表現層（API）：`TaxPortalApi/Controllers/`、`TaxPortalApi/Core/`、`TaxPortalApi/Extensions/`、`TaxPortalApi/Infrastructure/`、`TaxPortalApi/Middleware/`
- 應用層（Use Cases）：`TaxPortalApi/Services/`、`TaxPortalApi/Models/`、`TaxPortalApi/Utilities/`

### 檔案位置對應
- API 控制器放在 `TaxPortalApi/Controllers/`
- 業務服務放在 `TaxPortalApi/Services/`
- DTO/Request/Response 模型放在 `TaxPortalApi/Models/`
- 共用擴充放在 `TaxPortalApi/Extensions/`
- 基礎設施放在 `TaxPortalApi/Infrastructure/`
- 工具與輔助放在 `TaxPortalApi/Utilities/`

## 安全要求
- 所有 API 端點使用 `[Authorize]` 屬性
- 適當的輸入驗證
- 參數化查詢防止 SQL 注入
- 敏感資訊不記錄日誌
- 密碼強度驗證
- CORS 設定

## 代碼生成要求
- 生成完整的 CRUD 操作
- 包含適當的異常處理
- 使用 DTO 模式進行資料傳輸
- 遵循現有的架構模式
- 添加適當的驗證屬性
- 支援分頁查詢
- 使用 Attribute 的方式來實現 Entity 的關聯

## 分層架構原則
### 控制器層（Controllers）
- 只負責 HTTP 請求/回應與輸入驗證
- 僅呼叫 Service，不放業務邏輯

### 業務服務層（Services）
- 所有業務邏輯集中於 Service
- 協調多個 Repository 的操作
- 可處理 交易 與資料轉換
- Service 之間可互相呼叫，但避免循環相依
- 以介面定義服務契約，透過依賴注入註冊

### 資料存取層（Repositories）
- 只負責資料庫 CRUD 與查詢
- 不包含業務邏輯


## 開發指導原則
- Controller 只負責接收請求與呼叫 Service
- Service 處理業務規則與流程
- Repository 負責資料存取
- 使用依賴注入註冊服務
- 每個DTO、Request、Response的欄位都需要加上XML註解