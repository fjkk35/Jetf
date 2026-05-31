# ECCS API 整合規格（ASP.NET）

## ASP.NET 登入畫面需求

### 開發目標

在 ASP.NET 系統新增 ECCS 業者登入畫面。此畫面供操作人員輸入業者登入資料、查看並刷新驗證碼，以及執行登入。

本章節是前端畫面與互動需求。後方既有 API 文件維持不變，實作時請搭配第 3 節登入流程使用。

### 畫面欄位

| 顯示名稱 | 建議欄位名稱 | UI 元件 | 必填 | 說明 |
| --- | --- | --- | --- | --- |
| 統一編號 | `CompanyId` | Textbox | 是 | 對應登入 API 的 `idNo` |
| 帳號 | `Account` | Textbox | 是 | 對應登入 API 的 `userId` |
| 密碼 | `Password` | Password textbox | 是 | 對應登入 API 的 `userPwd`；畫面不可顯示明文 |
| 驗證碼 | `Captcha` | Textbox | 依 API 狀態 | 顯示驗證碼圖片時必填；對應登入 API 的 `captcha` |
| 驗證碼圖片 | `CaptchaImageBase64` | Image | 依 API 狀態 | 使用登入驗證碼 API 回傳的 `data.image` 顯示 |

畫面內部另外保存下列狀態，不提供操作人員編輯：

| 狀態名稱 | 用途 |
| --- | --- |
| `CaptchaCode` | 保存登入驗證碼 API 回傳的 `data.code`；登入時對應 API 的 `code` |
| `CaptchaRequired` | 保存登入驗證碼 API 回傳狀態；決定是否顯示驗證碼欄位與圖片 |
| `AnonymousToken` | 保存 `auth/token` 回傳的登入前匿名 token |
| `IsSubmitting` | 防止使用者重複送出登入 |
| `ErrorMessage` | 顯示 API 錯誤訊息 |

### 畫面按鈕

| 顯示名稱 | 建議事件名稱 | 說明 |
| --- | --- | --- |
| 刷新驗證碼 | `RefreshCaptchaAsync` | 重新呼叫登入驗證碼 API，覆蓋保存最新圖片與 `CaptchaCode` |
| 登入 | `LoginAsync` | 驗證輸入欄位後呼叫登入 API |

### 初始載入流程

畫面第一次開啟時：

```text
呼叫 POST /APECCS/ezway/auth/token
  ↓
保存 AnonymousToken
  ↓
呼叫 GET /APECCS/ezway/v1/system/verfiryCode
  ↓
如果 status == "Y"
  顯示驗證碼輸入框、圖片與刷新按鈕
  保存 data.code 至 CaptchaCode
  ↓
如果 status != "Y"
  隱藏驗證碼輸入框與圖片
```

### 刷新驗證碼流程

操作人員點擊「刷新驗證碼」時：

```text
重新呼叫 GET /APECCS/ezway/v1/system/verfiryCode
  ↓
清空目前輸入的 Captcha
  ↓
覆蓋 CaptchaImageBase64、CaptchaCode 與 CaptchaRequired
```

刷新驗證碼 API 必須使用目前的 `AnonymousToken`，並重新產生新的 `Timestamp` 與 `Sign`。

### 登入按鈕流程

操作人員點擊「登入」時：

1. 驗證 `CompanyId`、`Account` 與 `Password` 不可為空。
2. 如果 `CaptchaRequired == true`，驗證 `Captcha` 不可為空。
3. 將登入按鈕設為 disabled，避免重複提交。
4. 視 API 狀態處理服務條款確認流程。
5. 呼叫 `POST /APECCS/ezway/wlogin`。
6. 登入成功後保存正式 JWT 與 ECCS 內部 `userId`。
7. 登入失敗時顯示 API 回傳的 `msg`。
8. 完成後恢復登入按鈕狀態；如果驗證碼可能已失效，重新刷新驗證碼。

### 登入 Request 對照

| ASP.NET 畫面或狀態 | ECCS 登入 API 欄位 |
| --- | --- |
| `CompanyId` | `idNo` |
| `Account` | `userId` |
| `Password` | `userPwd` |
| 固定值 | `userType = "CUSTOMER"` |
| 固定值 | `lang = "TW"` |
| `CaptchaCode` | `code` |
| `Captcha` | `captcha` |
| 驗證碼 API 狀態 | `result` |
| 服務條款完成狀態 | `personCheck` |

### 驗證與安全要求

- 密碼欄位必須使用 password input，不可回填或顯示明文。
- 不可將密碼、JWT、captcha 或匿名 token 寫入 log、exception、telemetry、URL 或瀏覽器 local storage。
- 伺服器端保存 token 時，使用受保護的 session 或安全儲存機制。
- 每次 API request 都重新產生 `Timestamp` 與 `Sign`。
- 驗證碼必須由操作人員辨識輸入，不可自動辨識或繞過。
- 呼叫登入 API 時應透過 HTTPS。
- API 呼叫進行中，停用登入與刷新按鈕，避免重複送出。
- API 錯誤訊息應顯示給操作人員，但不得顯示密碼或 token。

## ASP.NET 登入成功後查詢畫面需求

### 開發目標

登入成功後顯示預先委任確認查詢畫面。畫面預設使用「查詢」模式，提供分提單號輸入區、查詢模式切換、送出查詢、匯出 Excel 與清除功能。

畫面視覺可沿用下列版型：

```text
查詢方式
(●) 查詢  ( ) 整批查詢

分提單號
┌──────────────────────────────┐
│ 請輸入分提單號               │
│                              │
│ 每行一筆分提單號             │
└──────────────────────────────┘

[查詢] [匯出 Excel] [清除]
```

### Radio 選項

| 顯示名稱 | 建議值 | 預設 | 說明 |
| --- | --- | --- | --- |
| 查詢 | `Single` | 是 | 依序呼叫單筆查詢 API |
| 整批查詢 | `Batch` | 否 | 將 Excel 檔案上傳至整批查詢 API |

建議 ASP.NET ViewModel：

```csharp
public enum EccsQueryMode
{
    Single,
    Batch
}
```

### 共用畫面狀態

| 狀態名稱 | 型別 | 用途 |
| --- | --- | --- |
| `QueryMode` | `EccsQueryMode` | 保存 radio 選擇；預設為 `Single` |
| `QueryCaptchaRequired` | `bool` | 是否要求查詢驗證碼 |
| `QueryCaptchaImageBase64` | `string?` | 查詢驗證碼圖片 |
| `QueryCaptchaCode` | `string?` | 查詢驗證碼 API 回傳的 `data.code` |
| `QueryCaptcha` | `string?` | 操作人員輸入的查詢驗證碼 |
| `IsQuerying` | `bool` | API 執行期間停用按鈕 |
| `QueryErrorMessage` | `string?` | 顯示查詢錯誤 |
| `QueryResults` | Collection | 保存查詢結果，供畫面顯示與匯出 Excel |

### 查詢模式畫面

選擇 radio「查詢」時，顯示：

| 顯示名稱 | 建議欄位名稱 | UI 元件 | 必填 | 說明 |
| --- | --- | --- | --- | --- |
| 分提單號 | `HawbNumbersText` | Textarea | 是 | placeholder：`請輸入分提單號`；每行輸入一筆分提單號 |
| 查詢驗證碼 | `QueryCaptcha` | Textbox | 依 API 狀態 | `QueryCaptchaRequired == true` 時顯示 |
| 查詢驗證碼圖片 | `QueryCaptchaImageBase64` | Image | 依 API 狀態 | 顯示 `query/setting` 回傳圖片 |

按鈕：

| 顯示名稱 | 建議事件名稱 | 說明 |
| --- | --- | --- |
| 查詢 | `QuerySingleAsync` | 逐筆呼叫單筆查詢 API |
| 刷新驗證碼 | `RefreshQueryCaptchaAsync` | 重新呼叫查詢驗證碼設定 API |
| 匯出 Excel | `ExportResultsToExcelAsync` | 將目前 `QueryResults` 匯出為 Excel |
| 清除 | `ClearQueryForm` | 清空輸入內容、錯誤訊息與查詢結果 |

單筆模式輸入規則：

1. 使用換行拆分 `HawbNumbersText`。
2. 移除每行前後空白。
3. 忽略空白行。
4. 視需求移除重複的分提單號。
5. 每筆分提單號呼叫一次 `POST /v4/realname/preverify-result`。
6. 每次查詢前依第 9 節檢查查詢驗證碼設定。
7. 每次查詢完成後，再次呼叫 `query/setting` 更新下一次查詢使用的驗證碼狀態。
8. 將解密後的結果加入 `QueryResults`。

### 整批查詢模式畫面

選擇 radio「整批查詢」時，顯示：

| 顯示名稱 | 建議欄位名稱 | UI 元件 | 必填 | 說明 |
| --- | --- | --- | --- | --- |
| Excel 檔案 | `BatchFile` | File input | 是 | 接受 `.xls` 或 `.xlsx` |
| 查詢驗證碼 | `QueryCaptcha` | Textbox | 依 API 狀態 | `QueryCaptchaRequired == true` 時顯示 |
| 查詢驗證碼圖片 | `QueryCaptchaImageBase64` | Image | 依 API 狀態 | 顯示 `query/setting` 回傳圖片 |

按鈕：

| 顯示名稱 | 建議事件名稱 | 說明 |
| --- | --- | --- |
| 整批查詢 | `QueryBatchAsync` | 呼叫整批查詢 API |
| 刷新驗證碼 | `RefreshQueryCaptchaAsync` | 重新呼叫查詢驗證碼設定 API |
| 下載範本 | `DownloadBatchTemplate` | 下載 ECCS 提供的分提單號 Excel 範本 |
| 匯出 Excel | `ExportResultsToExcelAsync` | 將目前 `QueryResults` 匯出為 Excel |
| 清除 | `ClearQueryForm` | 清空檔案、錯誤訊息與查詢結果 |

整批模式流程：

1. 驗證 `BatchFile` 已選擇且副檔名為 `.xls` 或 `.xlsx`。
2. 查詢前依第 9 節檢查查詢驗證碼設定。
3. 呼叫 `POST /v1/realname/preverify-result-batch`。
4. 查詢完成後，再次呼叫 `query/setting` 更新下一次查詢使用的驗證碼狀態。
5. 將回傳陣列加入 `QueryResults`。

### 登入成功後初始化流程

登入成功並保存正式 JWT 與 ECCS 內部 `userId` 後：

```text
導向預先委任確認查詢畫面
  ↓
將 QueryMode 預設為 Single
  ↓
呼叫 GET /APECCS/ezway/v1/system/query/setting?userId=<登入使用者 ID>
  ↓
保存最新 QueryCaptchaRequired、QueryCaptchaImageBase64 與 QueryCaptchaCode
  ↓
等待操作人員輸入分提單號或切換整批查詢
```

### 顯示結果與匯出 Excel

查詢結果至少顯示並支援匯出下列欄位：

| 顯示名稱 | API 欄位 |
| --- | --- |
| 預報關日期 | `importDate` |
| 報單號碼 | `declNo` |
| 主提單號碼 | `mawbNo` |
| 分提單號碼 | `hawbNo` |
| 電話號碼 | `telNo` |
| 證件號碼 | `idNo` |
| 實名委任日期 | `replyDate`, `replyTime` |
| 認證結果 | `isReply` |
| 核准文號 | `authorizeDocNo` |
| 海關回覆結果 | `authorizeReply` |
| 海關回覆日期 | `authorizeDatm` |
| 阻擋原因 | `blockReason` |

### UI 與安全要求

- 預設選取 radio「查詢」。
- 切換 radio 時，清除另一模式不適用的輸入資料，避免誤送。
- API 呼叫進行中停用查詢、刷新驗證碼、匯出 Excel 與清除按鈕。
- 驗證碼必須由操作人員輸入，不可自動辨識或繞過。
- 不可將正式 JWT、查詢驗證碼或查詢結果中的個資寫入 log。
- 查詢結果可能包含電話號碼與證件號碼，匯出的 Excel 檔案應依系統個資保護規範保存。

## 1. 文件說明

本文件依 2026-05-31 ECCS 網站正式環境目前載入的前端程式與實際操作畫面整理，供 ASP.NET 與 AI 開發工具實作登入、單筆查詢及整批查詢功能。

此文件不是 ECCS 官方公開規格。ECCS 更新前端或後端版本後，端點、欄位與簽名規則仍可能變動。

## 2. API 端點總表

Base URL：

```text
https://eccs.tradevan.com.tw/APECCS/ezway/
```

| 順序 | 功能 | HTTP Method | Path |
| --- | --- | --- | --- |
| 1 | 取得登入前匿名 token | `POST` | `auth/token` |
| 2 | 取得登入驗證碼設定或圖片 | `GET` | `v1/system/verfiryCode` |
| 3 | 取得待確認服務條款，視登入狀態呼叫 | `POST` | `v1/system/web_get_announcement` |
| 4 | 記錄服務條款同意，視登入狀態呼叫 | `POST` | `v1/system/web_announcement` |
| 5 | 業者登入 | `POST` | `wlogin` |
| 6 | 取得查詢驗證碼設定或圖片 | `GET` | `v1/system/query/setting?userId=<登入使用者 ID>` |
| 7 | 預先委任確認單筆查詢 | `POST` | `v4/realname/preverify-result` |
| 8 | 預先委任確認整批查詢 | `POST` | `v1/realname/preverify-result-batch` |

注意：登入驗證碼端點的 `verfiryCode` 是 ECCS 目前實際使用的拼字，串接時不要自行更正為 `verifyCode`。

## 3. 登入流程

### 3.1 登入流程摘要

```text
POST auth/token
  ↓
取得登入前匿名 token
  ↓
GET v1/system/verfiryCode
  ↓
若 status == "Y"，顯示圖片並取得人工輸入的 captcha
  ↓
必要時顯示並記錄服務條款同意
  ↓
POST wlogin
  ↓
若 status == "Y"，保存正式 token 與 userId
```

登入流程中的 API 也使用第 5 節的 `Timestamp` 與 `Sign` 規則。

### 3.2 取得登入前匿名 token

```http
POST https://eccs.tradevan.com.tw/APECCS/ezway/auth/token
Content-Type: application/json
Timestamp: <Unix timestamp 秒數>
Sign: <12 碼 nonce><32 碼 MD5 lowercase hex digest>
```

Request body：

```json
{
  "authId": "",
  "lang": "TW"
}
```

成功 response 概念格式：

```json
{
  "status": "Y",
  "msg": "",
  "data": {
    "token": "<登入前匿名 token>"
  }
}
```

取得 token 後，後續登入驗證碼與登入 API 使用：

```http
Authorization: Bearer <登入前匿名 token>
```

### 3.3 取得登入驗證碼

```http
GET https://eccs.tradevan.com.tw/APECCS/ezway/v1/system/verfiryCode
Authorization: Bearer <登入前匿名 token>
Timestamp: <Unix timestamp 秒數>
Sign: <12 碼 nonce><32 碼 MD5 lowercase hex digest>
```

Request 不需要 query string 或 JSON body。

Response 概念格式：

```json
{
  "status": "Y",
  "msg": "",
  "data": {
    "image": "<Base64 圖片>",
    "code": "<驗證碼識別碼>"
  }
}
```

| Response 欄位 | 說明 |
| --- | --- |
| `status` | `Y` 時顯示驗證碼輸入欄位；`N` 時依 `msg` 處理 |
| `data.image` | 組成 `data:image/png;base64,...` 後提供操作人員辨識 |
| `data.code` | 登入時透過 `code` 欄位送回 |

登入驗證碼必須由操作人員輸入，不應自動辨識或繞過。

### 3.4 業者登入

```http
POST https://eccs.tradevan.com.tw/APECCS/ezway/wlogin
Authorization: Bearer <登入前匿名 token>
Timestamp: <Unix timestamp 秒數>
Sign: <12 碼 nonce><32 碼 MD5 lowercase hex digest>
Content-Type: application/json
```

Request body：

| 欄位 | 型別 | 必填 | 範例 | 說明 |
| --- | --- | --- | --- | --- |
| `lang` | String | 是 | `TW` | 語系 |
| `idNo` | String | 是 | `<公司統一編號>` | 業者登入畫面的公司統一編號 |
| `userId` | String | 是 | `<帳號>` | 業者帳號 |
| `userPwd` | String | 是 | `<密碼>` | 業者密碼 |
| `userType` | String | 是 | `CUSTOMER` | 業者登入固定使用 `CUSTOMER` |
| `code` | String | 條件式 | `<驗證碼識別碼>` | 登入驗證碼 API 回傳的 `data.code` |
| `captcha` | String | 條件式 | `<使用者輸入值>` | 顯示登入驗證碼時由操作人員輸入 |
| `result` | String | 條件式 | `Y` | 前端保存的登入驗證碼狀態 |
| `personCheck` | String | 是 | `Y` | 已完成服務條款確認後送出 |

Request body 範例：

```json
{
  "lang": "TW",
  "idNo": "<公司統一編號>",
  "userId": "<帳號>",
  "userPwd": "<密碼>",
  "userType": "CUSTOMER",
  "code": "<驗證碼識別碼>",
  "captcha": "<使用者輸入值>",
  "result": "Y",
  "personCheck": "Y"
}
```

未要求登入驗證碼時，不要傳送空的 `code`、`captcha` 與 `result`。

### 3.5 登入 Response

登入成功 response 概念格式：

```json
{
  "status": "Y",
  "msg": "",
  "data": {
    "token": "<正式登入 JWT>",
    "userId": "<ECCS 內部使用者 ID>"
  }
}
```

登入成功後：

1. 以 `data.token` 取代登入前匿名 token。
2. 後續 API 使用 `Authorization: Bearer <正式登入 JWT>`。
3. 保存 `data.userId`，後續 `query/setting`、單筆查詢與整批查詢都會使用。

前端已觀察到的登入狀態：

| `status` | 行為 |
| --- | --- |
| `Y` | 登入成功，保存正式 token 與 `userId` |
| `P` | 要求處理密碼變更 |
| `W` | 顯示密碼即將過期提示 |

其他失敗情況應顯示 response `msg`，不要假設只有帳密錯誤一種原因。

### 3.6 服務條款確認

業者登入可能需要先顯示並記錄服務條款或個資聲明。前端會使用：

```http
POST v1/system/web_get_announcement
POST v1/system/web_announcement
```

`web_get_announcement` 用於取得待確認內容，`web_announcement` 用於記錄使用者同意。兩者都會帶入登入資料與驗證碼欄位。

實作時必須顯示服務條款內容並由操作人員主動同意，不要直接將 `personCheck` 寫死為 `Y` 以繞過確認。服務條款流程完成後，再呼叫 `wlogin`。

取得待確認內容：

```http
POST https://eccs.tradevan.com.tw/APECCS/ezway/v1/system/web_get_announcement
Authorization: Bearer <登入前匿名 token>
Timestamp: <Unix timestamp 秒數>
Sign: <12 碼 nonce><32 碼 MD5 lowercase hex digest>
Content-Type: application/json
```

```json
{
  "userId": "<帳號>",
  "idNo": "<公司統一編號>",
  "userPwd": "<密碼>",
  "code": "<驗證碼識別碼>",
  "captcha": "<使用者輸入值>",
  "type": "C",
  "lang": "TW"
}
```

有待確認內容時，response 概念格式：

```json
{
  "status": "Y",
  "msg": "",
  "data": [
    {
      "context": "<服務條款 HTML>"
    }
  ]
}
```

記錄使用者同意：

```http
POST https://eccs.tradevan.com.tw/APECCS/ezway/v1/system/web_announcement
Authorization: Bearer <登入前匿名 token>
Timestamp: <Unix timestamp 秒數>
Sign: <12 碼 nonce><32 碼 MD5 lowercase hex digest>
Content-Type: application/json
```

```json
{
  "userId": "<帳號>",
  "idNo": "<公司統一編號>",
  "userPwd": "<密碼>",
  "code": "<驗證碼識別碼>",
  "captcha": "<使用者輸入值>"
}
```

成功時 response `status` 為 `Y`。完成後才將登入 request 的 `personCheck` 設為 `Y` 並呼叫 `wlogin`。

如果登入時未要求驗證碼，省略空的 `code` 與 `captcha`。

### 3.7 登入實作注意事項

- `auth/token` 的 token 是登入前匿名 token，不是登入完成後的正式 JWT。
- 每支登入 API 都必須重新產生新的 `Timestamp` 與 `Sign`。
- `Authorization` 不參與 `Sign` 計算。
- 登入密碼目前由 HTTPS request body 傳送，前端未額外加密。
- 不要在 log、exception、telemetry 或設定檔中記錄密碼、JWT、captcha。

## 4. 共用 Request Headers

| Header | 必填 | 格式 | 說明 |
| --- | --- | --- | --- |
| `Authorization` | 條件式 | `Bearer <token>` | `auth/token` 不帶；登入階段使用匿名 token；登入成功後使用正式 JWT |
| `Timestamp` | 是 | Unix timestamp 秒數 | 例如：`1780156800` |
| `Sign` | 是 | 44 碼字串 | 12 碼 nonce 加上 32 碼 MD5 hex digest |
| `Content-Type` | 依 API | `application/json` 或 `multipart/form-data; boundary=...` | 整批查詢使用 `MultipartFormDataContent`，由 .NET 自動產生 boundary |

## 5. Sign 簽名規則

### 5.1 已確認的簽名參數

依目前 ECCS 正式前端的實際實作，`Sign` 只使用以下三個值：

| 順序 | 參數 | 說明 |
| --- | --- | --- |
| 1 | `nonce` | 每次呼叫重新產生的 12 碼隨機字串 |
| 2 | `Timestamp` | Request header 使用的 Unix timestamp 秒數 |
| 3 | 固定字串 | `+xH9x!&` |

以下內容不參與 `Sign` 計算：

- JWT
- API URL
- HTTP Method
- Excel 檔案內容
- Multipart FormData 欄位
- Request body

### 5.2 計算公式

```text
Timestamp = Unix timestamp 秒數
nonce     = 12 碼隨機字串
digest    = MD5(UTF8(nonce + Timestamp + "+xH9x!&"))
Sign      = nonce + digest.ToLowerHex()
```

`Sign` 總長度為 44 碼：

```text
12 碼 nonce + 32 碼 MD5 lowercase hex digest
```

### 5.3 nonce 格式

`nonce` 不需要向 ECCS API 取得，也不可固定寫死。每次 Request 都必須重新產生。

ECCS 網站目前前端使用 `Math.random().toString(20)` 產生 12 碼 nonce，因此字元來源相容於：

```text
0123456789abcdefghij
```

ASP.NET 串接時應沿用相同字元範圍與 12 碼長度。C# 實作使用 `RandomNumberGenerator.GetInt32()` 產生密碼學安全亂數，取代瀏覽器的 `Math.random()`：

```csharp
using System.Security.Cryptography;
using System.Text;

private static string CreateNonce(int length = 12)
{
    const string chars = "0123456789abcdefghij";
    var result = new StringBuilder(length);

    for (int i = 0; i < length; i++)
    {
        int index = RandomNumberGenerator.GetInt32(chars.Length);
        result.Append(chars[index]);
    }

    return result.ToString();
}
```

範例輸出：

```text
8j4b10i6c2ha
```

nonce 會放在 `Sign` 的前 12 碼，因此不需要另外新增 Request header 或 FormData 欄位：

```text
Sign = nonce + MD5(UTF8(nonce + Timestamp + "+xH9x!&"))
```

### 5.4 C# Sign 產生範例

```csharp
using System;
using System.Security.Cryptography;
using System.Text;

public static class EccsSignHelper
{
    private const string Secret = "+xH9x!&";
    private const string NonceChars = "0123456789abcdefghij";

    public static EccsSignHeaders Create()
    {
        string timestamp = DateTimeOffset.UtcNow
            .ToUnixTimeSeconds()
            .ToString();

        string nonce = CreateNonce(12);
        string raw = nonce + timestamp + Secret;

        byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes(raw));
        string digest = Convert.ToHexString(hash).ToLowerInvariant();

        return new EccsSignHeaders
        {
            Timestamp = timestamp,
            Sign = nonce + digest
        };
    }

    private static string CreateNonce(int length)
    {
        var value = new StringBuilder(length);

        for (int i = 0; i < length; i++)
        {
            int index = RandomNumberGenerator.GetInt32(NonceChars.Length);
            value.Append(NonceChars[index]);
        }

        return value.ToString();
    }
}

public sealed class EccsSignHeaders
{
    public string Timestamp { get; init; } = string.Empty;
    public string Sign { get; init; } = string.Empty;
}
```

使用方式：

```csharp
EccsSignHeaders signHeaders = EccsSignHelper.Create();

request.Headers.TryAddWithoutValidation("Timestamp", signHeaders.Timestamp);
request.Headers.TryAddWithoutValidation("Sign", signHeaders.Sign);
request.Headers.Authorization =
    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);
```

### 5.5 Sign 產生範例

以下範例使用固定輸入，方便開發與測試環境核對實作：

```text
nonce     = 0123456789ab
Timestamp = 1780156800
raw       = 0123456789ab1780156800+xH9x!&
MD5       = 9f7f81662993ec0369f6da180c88b6bf
Sign      = 0123456789ab9f7f81662993ec0369f6da180c88b6bf
```

可使用下列 C# 測試碼取得完整 Sign：

```csharp
using System;
using System.Security.Cryptography;
using System.Text;

string nonce = "0123456789ab";
string timestamp = "1780156800";
string raw = nonce + timestamp + "+xH9x!&";

string digest = Convert
    .ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(raw)))
    .ToLowerInvariant();

string sign = nonce + digest;

Console.WriteLine(sign);
```

## 6. 單筆查詢 API

### 6.1 API 基本資訊

| 項目 | 內容 |
| --- | --- |
| 功能 | 預先委任確認單筆查詢 |
| HTTP Method | `POST` |
| URL | `https://eccs.tradevan.com.tw/APECCS/ezway/v4/realname/preverify-result` |
| Content-Type | `application/json` |
| 驗證方式 | Bearer JWT 與自訂 `Sign` header |

單筆查詢使用與整批查詢相同的 Request headers：

```http
Authorization: Bearer <JWT>
Timestamp: <Unix timestamp 秒數>
Sign: <12 碼 nonce><32 碼 MD5 lowercase hex digest>
Content-Type: application/json
```

`Sign` 仍使用第 4 節的規則，不需要加入 API URL、HTTP Method 或 JSON body：

```text
Sign = nonce + MD5(UTF8(nonce + Timestamp + "+xH9x!&"))
```

### 6.2 Request JSON Body

| 欄位 | 型別 | 必填 | 範例 | 說明 |
| --- | --- | --- | --- | --- |
| `manual` | String | 是 | `Y` | `Y` 表示單筆查詢 |
| `userId` | String | 是 | `<登入使用者 ID>` | 登入後取得的 ECCS 內部使用者 ID |
| `brokerBan` | String | 是 | `<報關業者代碼>` | 業者帳號登入後由系統帶入 |
| `declType` | String | 是 | `G1` | 網站目前固定使用 `G1` |
| `status` | String | 是 | `A` | 委任狀態 |
| `lang` | String | 是 | `TW` | 語系 |
| `authorizeStatus` | String | 是 | `A` | 海關回覆狀態 |
| `startDate` | String | 否 | `20260501` | 預報關起始日期 |
| `endDate` | String | 否 | `20260530` | 預報關結束日期；使用日期查詢時與 `startDate` 一起傳送 |
| `declNo` | String | 否 | `<報單號碼>` | 報單號碼 |
| `mawbNo` | String | 否 | `<主提單號碼>` | 主提單號碼 |
| `hawbNo` | String | 否 | `901899975936` | 分提單號碼 |
| `code` | String | 條件式 | `<驗證碼識別碼>` | `query/setting` 回傳的 `data.code` |
| `captcha` | String | 條件式 | `<使用者輸入值>` | 畫面要求驗證碼時傳送 |

日期區間、報單號碼、主提單號碼與分提單號碼至少選擇一種查詢條件。

委任狀態：

| 值 | 說明 |
| --- | --- |
| `A` | 全部 |
| `Y` | 申請相符 |
| `N` | 申請不相符 |
| `W` | 未回覆 |

海關回覆狀態：

| 值 | 說明 |
| --- | --- |
| `A` | 全部 |
| `Y` | 核准 |
| `N` | 回覆錯誤 |

以前述分提單號查詢時，Request body 範例如下：

```json
{
  "manual": "Y",
  "userId": "<登入使用者 ID>",
  "brokerBan": "<報關業者代碼>",
  "declType": "G1",
  "hawbNo": "901899975936",
  "status": "A",
  "lang": "TW",
  "authorizeStatus": "A"
}
```

前端內部另有 `captchaImg` 與 `ieType` 畫面狀態。第三方串接通常不需要主動傳送這兩個欄位。

### 6.3 單筆查詢 Response

單筆查詢成功時，`data` 是 Base64 編碼的 AES-GCM 密文：

```json
{
  "status": "Y",
  "msg": "",
  "data": "<Base64 AES-GCM ciphertext and tag>",
  "returnMsg": null
}
```

### 6.4 AES-GCM 解密參數

| 參數 | 值 |
| --- | --- |
| 演算法 | `AES-256-GCM` |
| Key 編碼 | Base64 |
| Key | `vfqkS9So5y5CcyVCWhFYLTqlw27lvYhVo0QT+Hhbaa4=` |
| IV 編碼 | UTF-8 |
| IV | `NR55MPkVQH5YIxcm` |
| Authentication Tag | 密文最後 16 bytes |
| Tag Length | 128 bits |
| Additional Authenticated Data | 無 |
| 解密後格式 | UTF-8 JSON array |

### 6.5 C# AES-GCM 解密範例

```csharp
using System;
using System.Security.Cryptography;
using System.Text;

public static class EccsSingleQueryDecryptor
{
    private const string KeyBase64 =
        "vfqkS9So5y5CcyVCWhFYLTqlw27lvYhVo0QT+Hhbaa4=";

    private const string IvText = "NR55MPkVQH5YIxcm";
    private const int TagSize = 16;

    public static string Decrypt(string encryptedDataBase64)
    {
        byte[] key = Convert.FromBase64String(KeyBase64);
        byte[] iv = Encoding.UTF8.GetBytes(IvText);
        byte[] encrypted = Convert.FromBase64String(encryptedDataBase64);

        if (encrypted.Length < TagSize)
        {
            throw new ArgumentException(
                "AES-GCM data is shorter than the authentication tag.",
                nameof(encryptedDataBase64));
        }

        int ciphertextLength = encrypted.Length - TagSize;
        byte[] ciphertext = encrypted[..ciphertextLength];
        byte[] tag = encrypted[ciphertextLength..];
        byte[] plaintext = new byte[ciphertextLength];

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(iv, ciphertext, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }
}
```

解密後再反序列化 JSON：

```csharp
string json = EccsSingleQueryDecryptor.Decrypt(response.Data);
```

網站將 AES key 與固定 IV 放在前端 bundle 中，因此可確認上述解密方式與目前正式網站相容。固定 IV 並不是建議的 AES-GCM 安全設計；若 ECCS 更新前端版本，請重新核對參數。

## 7. 整批查詢 Multipart FormData 欄位

### 7.1 必填欄位

| 欄位 | 型別 | 範例 | 說明 |
| --- | --- | --- | --- |
| `manual` | String | `N` | `N` 表示整批查詢 |
| `file` | File | `分提單號.xlsx` | Excel 檔案，接受 `.xls` 或 `.xlsx` |
| `userId` | String | `<登入使用者 ID>` | 登入後取得的 ECCS 內部使用者 ID |
| `brokerBan` | String | `<報關業者代碼>` | 業者帳號登入後由系統帶入 |
| `declType` | String | `G1` | 網站目前固定使用 `G1` |
| `status` | String | `A` | 委任狀態；`A` 表示全部 |
| `lang` | String | `TW` | 語系 |
| `authorizeStatus` | String | `A` | 海關回覆狀態；`A` 表示全部 |

### 7.2 條件式欄位

| 欄位 | 型別 | 說明 |
| --- | --- | --- |
| `captcha` | String | ECCS 顯示驗證碼時填入 |
| `code` | String | 取得驗證碼圖片時一併取得的驗證碼識別碼 |

### 7.3 不需傳送的畫面狀態

下列欄位不是整批查詢的必要欄位：

| 欄位 | 說明 |
| --- | --- |
| `captchaImg` | 僅供瀏覽器顯示驗證碼圖片 |
| `ieType` | 網站目前預設為空值 |
| `startDate` | 單筆查詢條件 |
| `endDate` | 單筆查詢條件 |
| `declNo` | 單筆查詢條件 |
| `mawbNo` | 單筆查詢條件 |
| `hawbNo` | 單筆查詢條件 |

## 8. 整批查詢 Request 範例

```http
POST /APECCS/ezway/v1/realname/preverify-result-batch HTTP/1.1
Host: eccs.tradevan.com.tw
Authorization: Bearer <JWT>
Timestamp: <Unix timestamp 秒數>
Sign: <12 碼 nonce><32 碼 MD5 lowercase hex digest>
Content-Type: multipart/form-data; boundary=<boundary>

--<boundary>
Content-Disposition: form-data; name="manual"

N
--<boundary>
Content-Disposition: form-data; name="file"; filename="分提單號.xlsx"
Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet

<Excel binary>
--<boundary>
Content-Disposition: form-data; name="userId"

<登入使用者 ID>
--<boundary>
Content-Disposition: form-data; name="brokerBan"

<報關業者代碼>
--<boundary>
Content-Disposition: form-data; name="declType"

G1
--<boundary>
Content-Disposition: form-data; name="status"

A
--<boundary>
Content-Disposition: form-data; name="lang"

TW
--<boundary>
Content-Disposition: form-data; name="authorizeStatus"

A
--<boundary>--
```

## 9. 查詢驗證碼設定 API

### 9.1 用途

查詢頁會額外呼叫以下 API：

```http
GET https://eccs.tradevan.com.tw/APECCS/ezway/v1/system/query/setting?userId=<登入使用者 ID>
```

依目前 ECCS 正式前端程式，可確認此 API 用於取得查詢驗證碼設定，並在需要時刷新驗證碼圖片。

呼叫時機：

1. 進入查詢頁時呼叫一次。
2. 每次單筆或整批查詢完成後再次呼叫。
3. 使用者點擊刷新驗證碼按鈕時再次呼叫。

### 9.2 Request

| 項目 | 內容 |
| --- | --- |
| HTTP Method | `GET` |
| Query String | `userId=<登入使用者 ID>` |
| Headers | 與整批查詢 API 相同，包含 `Authorization`、`Timestamp` 與 `Sign` |

`Sign` 規則仍為：

```text
Sign = nonce + MD5(UTF8(nonce + Timestamp + "+xH9x!&"))
```

### 9.3 Response 與畫面行為

依前端程式使用方式，response 概念格式如下：

```json
{
  "status": "Y",
  "msg": "",
  "data": {
    "image": "<Base64 圖片>",
    "code": "<驗證碼識別碼>"
  }
}
```

前端處理方式：

| Response 欄位 | 用途 |
| --- | --- |
| `status` | 值為 `Y` 時，查詢頁顯示驗證碼輸入欄位 |
| `data.image` | 組成 `data:image/png;base64,...` 後顯示驗證碼圖片 |
| `data.code` | 查詢時透過 FormData 的 `code` 欄位送回伺服器 |

使用者辨識圖片後輸入的文字，查詢時透過 FormData 的 `captcha` 欄位送回伺服器。

### 9.4 串接建議

ASP.NET 串接時，建議沿用 ECCS 網站前端的流程：

1. 進入查詢功能時，先呼叫一次此 API，取得目前的驗證碼設定。
2. 如果 response `status` 不是 `Y`，直接呼叫單筆或整批查詢 API。
3. 如果 response `status` 是 `Y`，將圖片提供給操作人員輸入驗證碼。
4. 呼叫單筆或整批查詢 API 時，額外帶入最新的 `code` 與使用者輸入的 `captcha`。
5. 每次單筆或整批查詢完成後，自動再次呼叫此 API，更新下一次查詢使用的驗證碼狀態、圖片與 `code`。

從前端可以確認此 API 控制驗證碼顯示與刷新。伺服器端依哪些條件決定 `status = Y`，例如查詢次數、帳號設定或風險規則，無法僅由前端程式判斷。

建議不要快取或重複使用前一次的 `code`。應以查詢完成後重新呼叫此 API 所取得的最新 response 為準。

### 9.5 建議呼叫流程

```text
進入查詢功能
  ↓
GET /v1/system/query/setting?userId=<登入使用者 ID>
  ↓
保存最新 status、image 與 code
  ↓
status == "Y"？
  ├─ 否：直接送出查詢
  └─ 是：顯示 image，取得人工輸入的 captcha
          送出查詢時附加 code 與 captcha
  ↓
POST 單筆或整批查詢 API
  ↓
查詢完成後，自動再次呼叫
GET /v1/system/query/setting?userId=<登入使用者 ID>
  ↓
覆蓋保存最新 status、image 與 code，供下一次查詢使用
```

`query/setting` API 本身也必須使用新的 `Timestamp` 與 `Sign`。每次自動刷新時，請重新產生簽名，不要沿用前一次 Request 的 headers。

## 10. Excel 檔案格式

畫面提供的範本：

```text
https://eccs.tradevan.com.tw/sample/REL00009/分提單號.xlsx
```

第一個工作表只使用一欄：

| 分提單號 |
| --- |
| `TEST1234` |
| `TEST2234` |

實際使用時，第一列保留欄名 `分提單號`，第二列起每列放入一筆分提單號。

## 11. 整批查詢 Response

### 11.1 成功回應

批次查詢回應為 JSON。與單筆查詢不同，批次查詢的 `data` 是明文 JSON array，不需要執行 AES-GCM 解密。

```json
{
  "status": "Y",
  "msg": "",
  "data": [
    {
      "id": "",
      "transactionId": "",
      "declType": "",
      "brokerName": "",
      "verifiedType": "",
      "importDate": "",
      "declNo": "",
      "mawbNo": "",
      "hawbNo": "",
      "replyDate": "",
      "replyTime": "",
      "isReply": "",
      "memo": "",
      "authorizeDocNo": "",
      "authorizeDatm": "",
      "authorizeReply": "",
      "idNo": "",
      "telNo": "",
      "blockReason": ""
    }
  ]
}
```

### 11.2 Response data 欄位

| 欄位 | 說明 |
| --- | --- |
| `id` | 資料識別碼 |
| `transactionId` | 交易識別碼 |
| `declType` | 報單類型 |
| `brokerName` | 報關業者名稱 |
| `verifiedType` | 驗證類型 |
| `importDate` | 預報關日期 |
| `declNo` | 報單號碼 |
| `mawbNo` | 主提單號碼 |
| `hawbNo` | 分提單號碼 |
| `replyDate` | 實名委任日期 |
| `replyTime` | 實名委任時間 |
| `isReply` | 認證結果 |
| `memo` | 備註 |
| `authorizeDocNo` | 核准文號 |
| `authorizeDatm` | 海關回覆日期時間 |
| `authorizeReply` | 海關回覆結果 |
| `idNo` | 證件號碼 |
| `telNo` | 電話號碼 |
| `blockReason` | 阻擋原因 |

## 12. 驗證範圍與注意事項

- 已核對 ECCS 正式前端程式：`Sign` 只使用 nonce、`Timestamp` 與固定字串 `+xH9x!&`。
- `Authorization` JWT 是必要的獨立驗證 header，但不參與 `Sign` 計算。
- `Timestamp` 與 `Sign` 必須在每次 Request 重新產生。
- 請使用 UTF-8 計算 MD5，並輸出 lowercase hex digest。
- 請讓 `MultipartFormDataContent` 自動產生 `Content-Type` boundary，不要手動寫死 boundary。
- `+xH9x!&` 位於網站前端 bundle，不應視為安全的 server-side secret。
- 尚未確認 ECCS 伺服器端的 Excel 筆數上限、檔案大小上限、JWT 有效期限與 `Timestamp` 容許時間差。

## 13. 參考來源

- 查詢頁：`https://eccs.tradevan.com.tw/ezway/REL00/REL00011`
- 共用 API bundle：`https://eccs.tradevan.com.tw/static/js/main.eca1d060.chunk.js`
- REL00011 bundle：`https://eccs.tradevan.com.tw/static/js/158.6648a4a5.chunk.js`
- Excel 範本：`https://eccs.tradevan.com.tw/sample/REL00009/分提單號.xlsx`
