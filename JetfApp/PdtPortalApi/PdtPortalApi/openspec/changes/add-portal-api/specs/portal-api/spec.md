## ADDED Requirements

### Requirement: 統一 API 回應格式

所有 Portal API 端點 MUST 回傳共用回應模型，包含是否成功、狀態碼、訊息、錯誤碼、時間戳記，以及需要時的資料內容。

#### Scenario: 成功回應有資料

- **WHEN** API 成功且有資料內容
- **THEN** 回傳 `ApiResponse<T>`
- **AND** `IsSuccess` 為 `true`
- **AND** `Code` 為 `200`
- **AND** `Message` 預設為 `操作成功`
- **AND** `Data` 包含實際資料

#### Scenario: 失敗回應

- **WHEN** API 處理失敗
- **THEN** 回傳 `ApiResponse` 或 `ApiResponse<T>`
- **AND** `IsSuccess` 為 `false`
- **AND** `Code` 預設為 `400`
- **AND** 允許包含 `ErrorCode`

### Requirement: 提供登入 API

系統 MUST 提供登入 API，透過 `Account` 查詢 `jetf.dbo.USER_MASTER` 的 `USER_ID` 判斷登入是否成功。

#### Scenario: 帳號存在

- **WHEN** 呼叫登入 API 並傳入 `Account`
- **AND** `jetf.dbo.USER_MASTER` 可查到相符 `USER_ID`
- **THEN** API 回傳 `ApiResponse<bool>`
- **AND** `Data` 為 `true`

#### Scenario: 帳號不存在

- **WHEN** 呼叫登入 API 並傳入 `Account`
- **AND** `jetf.dbo.USER_MASTER` 查無相符資料
- **THEN** API 回傳 `ApiResponse<bool>`
- **AND** `Data` 為 `false`

### Requirement: 提供貨件來源查詢 API

系統 MUST 提供貨件來源查詢 API，回傳 `jetf.dbo.ShipmentInboundSourceType` 的 `Id` 與 `SourceType`。

#### Scenario: 查詢貨件來源清單

- **WHEN** 呼叫貨件來源查詢 API
- **THEN** API 回傳 `ApiResponse<IEnumerable<ShipmentInboundSourceTypeDto>>`
- **AND** 資料來自 `SELECT Id, SourceType FROM jetf.dbo.ShipmentInboundSourceType`

### Requirement: 提供原始入庫資料檢查 API

系統 MUST 提供入庫資料檢查 API，使用 `TrackingNo` 檢查海運原單、空運原單 TrackingNo、空運原單 DeliveryNo 是否存在原始資料。

#### Scenario: 海運原始資料存在

- **WHEN** 呼叫檢查 API 並傳入 `TrackingNo`
- **AND** `DATA_CENTER.dbo.SEA_ORDER_ORIGINAL` 查得到 `JETF_SERIAL`
- **THEN** API 回傳 `ApiResponse<bool>`
- **AND** `Data` 為 `true`

#### Scenario: 空運原始資料以 TrackingNo 存在

- **WHEN** 呼叫檢查 API 並傳入 `TrackingNo`
- **AND** `DATA_CENTER.dbo.ORIGINALLIST` 可查到相符 `TRACKINGNO`
- **THEN** API 回傳 `ApiResponse<bool>`
- **AND** `Data` 為 `true`

#### Scenario: 空運原始資料以 DeliveryNo 存在

- **WHEN** 呼叫檢查 API 並傳入 `TrackingNo`
- **AND** `DATA_CENTER.dbo.ORIGINALLIST` 可查到相符 `DELIVERYNO`
- **THEN** API 回傳 `ApiResponse<bool>`
- **AND** `Data` 為 `true`

#### Scenario: 原始資料不存在

- **WHEN** 呼叫檢查 API 並傳入 `TrackingNo`
- **AND** `DATA_CENTER.dbo.SEA_ORDER_ORIGINAL`、`DATA_CENTER.dbo.ORIGINALLIST.TRACKINGNO` 與 `DATA_CENTER.dbo.ORIGINALLIST.DELIVERYNO` 皆查無資料
- **THEN** API 回傳 `ApiResponse<bool>`
- **AND** `Data` 為 `false`

### Requirement: 提供入庫寫入 API

系統 MUST 提供入庫寫入 API，接收入庫資料，依海運或空運原始資料補齊欄位後寫入 `jetf.dbo.ShipmentInbound`。

#### Scenario: 入庫資料重複

- **WHEN** 呼叫入庫寫入 API
- **AND** `ShipmentInbound` 中存在相同 `TrackingNo`
- **AND** 該筆資料 `OutboundDate` 小於目前時間往前 3 天
- **THEN** API 回傳失敗
- **AND** 不新增資料
- **AND** 系統以 EF Core 實體新增取代手寫 SQL Insert

#### Scenario: 海運資料寫入

- **WHEN** 呼叫入庫寫入 API
- **AND** 請求通過 HMAC-SHA256 + Timestamp 驗證
- **AND** `DATA_CENTER.dbo.SEA_ORDER_ORIGINAL` 可依 `TrackingNo` 查到資料
- **THEN** `DataType` 設為 `海運`
- **AND** `IsOrderOriginal` 設為 `true`
- **AND** 以海運來源資料補齊 `ImporterAddr`、`ImporterPhone`、`Importer`、`CustCode`、`TransName`
- **AND** `Fee` 固定寫入 `30`
- **AND** 稅金資料來自 `jetf.dbo.FEE_MASTER`
- **AND** 成功寫入 `jetf.dbo.ShipmentInbound`

#### Scenario: 空運資料寫入

- **WHEN** 呼叫入庫寫入 API
- **AND** 請求通過 HMAC-SHA256 + Timestamp 驗證
- **AND** 海運查無資料
- **AND** `DATA_CENTER.dbo.ORIGINALLIST` 可依 `TrackingNo` 查到資料
- **THEN** `DataType` 設為 `空運`
- **AND** `IsOrderOriginal` 設為 `true`
- **AND** 以空運來源資料補齊 `ImporterAddr`、`ImporterPhone`、`Importer`、`CustCode`、`TransNo`
- **AND** `Fee` 固定寫入 `30`
- **AND** 稅金資料來自 `jetf.dbo.FEE_MASTER`
- **AND** 成功寫入 `jetf.dbo.ShipmentInbound`

#### Scenario: 查無任何原始資料仍可寫入

- **WHEN** 呼叫入庫寫入 API
- **AND** 請求通過 HMAC-SHA256 + Timestamp 驗證
- **AND** 海運與空運來源皆查無資料
- **THEN** `DataType` 以空字串寫入
- **AND** `IsOrderOriginal` 設為 `false`
- **AND** 未補齊的來源欄位以空字串或 0 寫入
- **AND** `Fee` 固定寫入 `30`
- **AND** 成功寫入 `jetf.dbo.ShipmentInbound`

#### Scenario: 簽章無效或逾時

- **WHEN** 呼叫入庫寫入 API
- **AND** `X-Timestamp` 不在 5 分鐘有效範圍內，或 `X-Signature` 與伺服器計算值不一致
- **THEN** API 回傳失敗
- **AND** `Code` 為 `401`
- **AND** `ErrorCode` 為 `INVALID_SIGNATURE`

### Requirement: HMAC 驗證規則

系統 MUST 使用 HMAC-SHA256 + Timestamp 驗證入庫寫入 API。

#### Scenario: 計算簽章

- **WHEN** 客戶端呼叫入庫寫入 API
- **THEN** 必須提供 `X-Timestamp` 與 `X-Signature` 標頭
- **AND** `X-Timestamp` 使用 Unix time seconds
- **AND** 伺服器以 UTF-8 將下列字串組合後進行 HMAC-SHA256 計算：
- **AND** `${X-Timestamp}\n${InboundDate:o}\n${TrackingNo}\n${SeqNo}\n${LocationCode}\n${SourceType}\n${ReturnTrackingNo}`
- **AND** `X-Signature` 使用十六進位小寫字串

### Requirement: 提供 OpenAPI 與 Scalar 文件

系統 MUST 產出 OpenAPI 文件，並透過 Scalar 提供可瀏覽的 API UI。

#### Scenario: 開啟 API 文件

- **WHEN** 啟動 API
- **THEN** 系統提供 OpenAPI JSON 端點
- **AND** 系統提供 Scalar UI 端點供開發人員檢視 API 文件

### Requirement: 資料查詢必須使用 EF Core LINQ

系統 MUST 使用 Entity Framework Core 的 LINQ 查詢存取業務資料，不得以 raw SQL 查詢取代登入、貨件來源、原始資料檢查、海運查詢、空運查詢與費用查詢邏輯。

#### Scenario: Portal 查詢資料

- **WHEN** 系統執行登入、貨件來源、原始資料檢查、海運資料查詢、空運資料查詢或費用查詢
- **THEN** 必須使用 EF Core 實體映射搭配 LINQ 查詢
- **AND** 不使用 `Database.SqlQuery` 進行上述資料讀取

### Requirement: 方法必須具備註解、例外處理與記錄

系統 MUST 為控制器與服務方法提供 XML 註解，並以 try-catch 包住方法主體，在例外發生時記錄 log。

#### Scenario: 方法正常執行

- **WHEN** 控制器或服務方法正常執行
- **THEN** 方法具備 XML 註解說明用途、參數與回傳

#### Scenario: 方法發生例外

- **WHEN** 控制器或服務方法執行失敗
- **THEN** 方法以 try-catch 攔截例外
- **AND** 記錄錯誤 log
- **AND** 控制器回傳標準化失敗回應，或服務層保留例外語意後往上拋出

### Requirement: Log 僅保留 7 天

系統 MUST 將應用程式 log 寫入檔案，並自動刪除超過 7 天保留期限的舊 log。

#### Scenario: 產生與清理 log

- **WHEN** API 執行並產生 log
- **THEN** log 會寫入每日滾動的檔案
- **AND** 系統自動保留最近 7 天 log
- **AND** 超過 7 天的舊 log 會自動刪除

### Requirement: Model 類別必須具備 XML 註解

系統 MUST 為 Models 目錄下的 request、response、dto、entity 類別與主要屬性補上 XML 註解，至少涵蓋欄位用途說明。

#### Scenario: 檢視 model 類別

- **WHEN** 開發人員檢視 model 類別
- **THEN** 類別與主要屬性具有 XML 註解
- **AND** ShipmentInbound 相關欄位說明應反映資料型態、單號、儲位、貨件來源、客戶、承運商、收件人、稅費與上傳狀態等用途

### Requirement: 提供 App 版本檢查 API

系統 MUST 提供 App 版本檢查 API，在登入前回傳目前允許版本、APK 下載位置與是否必須強制更新。

#### Scenario: 版本一致

- **WHEN** 呼叫 `GET /api/app/version-check`
- **AND** `versionCode` 以字串形式傳入，例如 `0.0.1`
- **AND** `versionCode` 與後端設定的字串 `LatestVersionCode` 相同
- **THEN** API 回傳 `ApiResponse<AppVersionCheckResponse>`
- **AND** `Data.forceUpdate` 為 `false`
- **AND** `Data.message` 提示可正常使用

#### Scenario: 版本不一致且強制更新

- **WHEN** 呼叫 `GET /api/app/version-check`
- **AND** `versionCode` 與後端設定的字串 `LatestVersionCode` 不相同
- **AND** 後端設定 `ForceUpdate` 為 `true`
- **THEN** API 回傳 `ApiResponse<AppVersionCheckResponse>`
- **AND** `Data.forceUpdate` 為 `true`
- **AND** `Data.message` 提示必須更新後才能使用

#### Scenario: 版本不一致但不強制更新

- **WHEN** 呼叫 `GET /api/app/version-check`
- **AND** `versionCode` 與後端設定的字串 `LatestVersionCode` 不相同
- **AND** 後端設定 `ForceUpdate` 為 `false`
- **THEN** API 回傳 `ApiResponse<AppVersionCheckResponse>`
- **AND** `Data.forceUpdate` 為 `false`
- **AND** `Data.message` 提示可選擇更新或繼續使用

### Requirement: Login 必須驗證 App 版本

系統 MUST 在 Login API 再次驗證 App 版本，避免略過版本檢查直接登入。

#### Scenario: Login 版本不符

- **WHEN** 呼叫 Login API
- **AND** `versionCode` 與後端設定的字串 `LatestVersionCode` 不相同
- **AND** 後端設定 `ForceUpdate` 為 `true`
- **THEN** API 回傳失敗
- **AND** `Code` 為 `426` 或適當拒絕狀態碼
- **AND** `ErrorCode` 為 `APP_VERSION_EXPIRED`
- **AND** 使用者不得登入

#### Scenario: Login 版本不符但未強制更新

- **WHEN** 呼叫 Login API
- **AND** `versionCode` 與後端設定的字串 `LatestVersionCode` 不相同
- **AND** 後端設定 `ForceUpdate` 為 `false`
- **THEN** 系統仍允許繼續執行帳號存在性驗證

#### Scenario: Login 版本正確

- **WHEN** 呼叫 Login API
- **AND** `versionCode` 與後端設定的 `LatestVersionCode` 相同
- **THEN** 系統才繼續執行帳號存在性驗證