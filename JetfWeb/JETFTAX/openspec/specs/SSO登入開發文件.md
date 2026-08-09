# 進口快遞管理系統 SSO 登入 API

## API 資訊

| 項目 | 內容 |
| --- | --- |
| Method | `GET` |
| URL | `http://192.168.1.9/JETF/login/sso` |
| 驗證方式 | `HMAC-SHA256` |
| Secret Key | `7f3c6e9a-8b41-4e2d-9a6f-2c8d1e5b0a73` |
| Timestamp 有效時間 | 30 秒 |

## Request

### Query Parameters

| 參數 | 必填 | 型別 | 說明 |
| --- | --- | --- | --- |
| `userId` | 是 | string | 進口快遞管理系統使用者代碼，帳號必須存在且為啟用狀態 |
| `timestamp` | 是 | long | 呼叫當下的 UTC Unix Timestamp，單位為秒 |
| `sign` | 是 | string | HMAC-SHA256 簽章，64 字元小寫十六進位 |

### Request URL 格式

```text
http://192.168.1.9/JETF/login/sso?userId={userId}&timestamp={timestamp}&sign={sign}
```

```http
GET /JETF/login/sso?userId=Jetf001&timestamp={目前的UnixTimestamp秒數}&sign={計算後的sign} HTTP/1.1
Host: 192.168.1.9
```

呼叫時請直接將使用者的瀏覽器導向此 URL，讓瀏覽器取得進口快遞管理系統登入 Session。

## Sign 產生方式

### 1. 組合簽章原文

欄位名稱、大小寫、順序及分隔符號必須完全一致：

```text
userId={userId}&timestamp={timestamp}
```

範例：

```text
userId=Jetf001&timestamp=1780202356
```

### 2. 使用 HMAC-SHA256 計算

```text
sign = HMAC-SHA256(
    key: UTF8(Secret Key),
    data: UTF8(簽章原文)
)
```

### 3. 將結果轉成小寫十六進位字串

```text
Secret Key：7f3c6e9a-8b41-4e2d-9a6f-2c8d1e5b0a73
簽章原文：userId=Jetf001&timestamp=1780202356
sign：941b19479afa204467ebecaa397438335f5951d0fa99b6e9d8100c7121624583
```

> 上述固定 timestamp 僅供核對簽章結果；實際呼叫必須使用當下的 UTC Unix Timestamp 秒數。

注意：

- `sign` 必須是小寫十六進位，不是 Base64。
- `userId`、`timestamp`、`sign` 前後不可有空白。
- 先計算簽章，再對 Query String 參數執行 URL Encode。
- `timestamp` 必須使用秒，不可使用毫秒。

## Response

### 登入成功

驗證成功後建立進口快遞管理系統 Session，回傳 `302 Found` 並導向登入成功頁。

```http
HTTP/1.1 302 Found
Location: /JETF/LoginSuccess/Index
Set-Cookie: ASP.NET_SessionId=...
```

成功時不回傳 JSON。

### 登入失敗

```json
{
  "code": "004",
  "message": "Sign 驗證失敗"
}
```

| HTTP Status | Code | Message |
| --- | --- | --- |
| `400 Bad Request` | `001` | `缺少必要參數` |
| `400 Bad Request` | `002` | `Timestamp 格式錯誤` |
| `401 Unauthorized` | `003` | `Timestamp 已過期` |
| `401 Unauthorized` | `004` | `Sign 驗證失敗` |
| `403 Forbidden` | `005` | `使用者不存在或不可登入` |
