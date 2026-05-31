# SSO 登入 API 開發文件

## 1. 用途說明

本文件提供外部系統串接 JETFTAX SSO 登入 API 使用。

呼叫端需將 userId、timestamp、sign 帶入登入網址。系統收到請求後，會先驗證參數、timestamp 與 sign，再確認使用者是否存在且可登入；全部通過後，系統會建立登入狀態並導向登入後頁面。

## 2. API 基本資訊

| 項目 | 說明 |
| --- | --- |
| HTTP Method | GET |
| Endpoint | /login/sso |
| 成功導向頁面 | /Upload/Seatax |
| 驗證演算法 | HMACSHA256 |
| Timestamp 格式 | UTC Unix Timestamp，單位為秒 |
| Timestamp 有效時間 | 3 分鐘 |

## 3. SecretKey

| 項目 | 值 |
| --- | --- |
| SecretKey | 7f3c6e9a-8b41-4e2d-9a6f-2c8d1e5b0a73 |

## 4. Request 規格

### 4.1 Query Parameters

| 參數 | 必填 | 說明 |
| --- | --- | --- |
| userId | 是 | 使用者代碼，對應 [jetf].[dbo].[USER_MASTER].[USER_ID] |
| timestamp | 是 | UTC Unix Timestamp，單位為秒 |
| sign | 是 | 依簽章規則產出的 HMACSHA256 小寫 16 進位字串 |

### 4.2 Request 範例

```http
GET /login/sso?userId=Jetf001&timestamp=1780202356&sign=941b19479afa204467ebecaa397438335f5951d0fa99b6e9d8100c7121624583 HTTP/1.1
Host: example.com
```

## 5. Sign 簽章規則

### 5.1 簽章原文格式

sign 原文必須依以下固定格式組成，欄位名稱、順序與符號都不可更動：

```text
userId={userId}&timestamp={timestamp}
```

### 5.2 簽章演算法

| 項目 | 說明 |
| --- | --- |
| 演算法 | HMACSHA256 |
| Key | SecretKey |
| 輸出格式 | 小寫 16 進位字串 |

### 5.3 簽章步驟

1. 先取得當下 UTC Unix Timestamp 秒數。
2. 依固定格式組成 sign 原文：userId={userId}&timestamp={timestamp}。
3. 以 SecretKey 對 sign 原文做 HMACSHA256 計算。
4. 將結果轉成小寫 16 進位字串。
5. 將該結果放入 sign 參數後呼叫 API。

### 5.4 簽章範例

| 項目 | 值 |
| --- | --- |
| userId | Jetf001 |
| timestamp | 1780202356 |
| sign 原文 | userId=Jetf001&timestamp=1780202356 |
| sign | 941b19479afa204467ebecaa397438335f5951d0fa99b6e9d8100c7121624583 |

## 6. Timestamp 驗證規則

timestamp 使用 UTC Unix Timestamp，單位為秒。

系統目前的過期判斷條件如下：

```text
server current UTC timestamp - request timestamp > 180 seconds 時，判定為過期
```

串接端請使用產生連結當下的 UTC Unix Timestamp，避免因時間差造成驗證失敗。

## 7. 使用者驗證規則

當 timestamp 與 sign 驗證成功後，系統會再確認 userId 是否為可登入使用者。

若使用者不存在或不可登入，將回傳 005 錯誤。

## 8. Response 規格

### 8.1 驗證成功

驗證成功後，系統會建立登入狀態並導向登入後頁面。

| 項目 | 說明 |
| --- | --- |
| HTTP Status | 302 Found |
| Location Header | /Upload/Seatax |

Response Header 範例：

```http
HTTP/1.1 302 Found
Location: /Upload/Seatax
```

### 8.2 驗證失敗

失敗時會回傳 JSON 格式錯誤內容。

| Code | HTTP Status | Message |
| --- | --- | --- |
| 001 | 400 Bad Request | 缺少必要參數 |
| 002 | 400 Bad Request | Timestamp 格式錯誤 |
| 003 | 401 Unauthorized | Timestamp 已過期 |
| 004 | 401 Unauthorized | Sign 驗證失敗 |
| 005 | 403 Forbidden | 使用者不存在或不可登入 |

#### 001 缺少必要參數

```json
{
  "code": "001",
  "message": "缺少必要參數"
}
```

#### 002 Timestamp 格式錯誤

```json
{
  "code": "002",
  "message": "Timestamp 格式錯誤"
}
```

#### 003 Timestamp 已過期

```json
{
  "code": "003",
  "message": "Timestamp 已過期"
}
```

#### 004 Sign 驗證失敗

```json
{
  "code": "004",
  "message": "Sign 驗證失敗"
}
```

#### 005 使用者不存在或不可登入

```json
{
  "code": "005",
  "message": "使用者不存在或不可登入"
}
```

## 9. 串接注意事項

1. sign 原文中的欄位順序必須固定為 userId 在前、timestamp 在後。
2. sign 必須為小寫 16 進位字串，不可使用大寫。
3. timestamp 請使用 UTC 秒數，不可使用毫秒。
4. userId、timestamp、sign 都必須帶值，任一缺漏都會回 400。
5. 驗證成功回應為 302 Redirect，不是 JSON 成功訊息。
