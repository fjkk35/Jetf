# Pdt Portal API 規格文件

本文件提供 Android APP 開發使用，內容依目前後端實作整理。

## Base URL

依部署位置不同，Base URL 可能不同。

- IIS 子路徑部署範例: `https://service.jet-f.com/PdtPortalAPI`
- 本機 Kestrel 範例: `http://localhost:5260`

API 文件頁面:

- Scalar: `/scalar`
- OpenAPI JSON: `/openapi/v1.json`

## 共用回應格式

所有 API 都使用共用包裝格式。

### Response Envelope

```json
{
  "isSuccess": true,
  "code": 200,
  "message": "操作成功",
  "errorCode": "",
  "timestamp": "2026-04-11T10:30:00+08:00",
  "data": {}
}
```

### 共用欄位說明

| 欄位 | 型別 | 說明 |
| --- | --- | --- |
| isSuccess | bool | 是否成功 |
| code | int | 回應狀態碼 |
| message | string | 訊息內容 |
| errorCode | string | 錯誤代碼，成功時通常為空字串 |
| timestamp | string | 後端產生時間 |
| data | object / array / bool / null | 實際回傳資料 |

## 1. App 版本檢查

登入前請先呼叫此 API。

### Route

`GET /api/app/version-check`

### Query Parameters

| 參數 | 型別 | 必填 | 說明 |
| --- | --- | --- | --- |
| versionCode | string | 是 | APP 目前版本號，例如 `0.0.1` |

### 成功回應 data 欄位

| 欄位 | 型別 | 說明 |
| --- | --- | --- |
| latestVersionCode | string | 後端目前設定的最新版本號 |
| apkUrl | string | APK 下載網址 |
| forceUpdate | bool | 是否必須強制更新 |
| message | string | 提示訊息 |

### 成功回應範例

```json
{
  "isSuccess": true,
  "code": 200,
  "message": "操作成功",
  "errorCode": "",
  "timestamp": "2026-04-11T10:30:00+08:00",
  "data": {
    "latestVersionCode": "0.0.1",
    "apkUrl": "https://your-domain.com/app.apk",
    "forceUpdate": false,
    "message": "版本正確，可正常使用"
  }
}
```

### 版本判斷規則

- 當 `versionCode == latestVersionCode` 時，可正常使用
- 當 `versionCode != latestVersionCode` 且後端 `ForceUpdate = true` 時，必須更新
- 當 `versionCode != latestVersionCode` 且後端 `ForceUpdate = false` 時，可選擇更新或繼續使用

### 可能錯誤

| HTTP Status | errorCode | 說明 |
| --- | --- | --- |
| 400 | VALIDATION_ERROR | 未帶 versionCode |
| 500 | INTERNAL_SERVER_ERROR | 系統錯誤 |

## 2. Login

### Route

`POST /api/auth/login`

### Request Body

| 欄位 | 型別 | 必填 | 說明 |
| --- | --- | --- | --- |
| account | string | 是 | 使用者帳號 |
| versionCode | string | 是 | APP 目前版本號，例如 `0.0.1` |

### Request 範例

```json
{
  "account": "demo",
  "versionCode": "0.0.1"
}
```

### 成功回應 data 欄位

| 欄位 | 型別 | 說明 |
| --- | --- | --- |
| data | bool | 帳號存在為 `true`，不存在為 `false` |

### 成功回應範例

```json
{
  "isSuccess": true,
  "code": 200,
  "message": "登入成功",
  "errorCode": "",
  "timestamp": "2026-04-11T10:30:00+08:00",
  "data": true
}
```

### Login 規則

- Login 會再次驗證 `versionCode`
- 若後端設定 `ForceUpdate = true` 且版本不一致，會拒絕登入
- 若 `ForceUpdate = false`，即使版本不一致仍可登入
- 帳號存在條件: `jetf.dbo.USER_MASTER.USER_ID = account`

### 可能錯誤

| HTTP Status | errorCode | 說明 |
| --- | --- | --- |
| 400 | VALIDATION_ERROR | account 或 versionCode 未提供 |
| 426 | APP_VERSION_EXPIRED | 版本過舊且被設定為強制更新 |
| 500 | INTERNAL_SERVER_ERROR | 系統錯誤 |

## 3. 取得貨件來源

### Route

`GET /api/shipmentinbound/source-types`

### Request

無 request body。

### 成功回應 data 欄位

`data` 會是一個陣列，每個元素欄位如下:

| 欄位 | 型別 | 說明 |
| --- | --- | --- |
| id | int | 貨件來源識別碼 |
| sourceType | string | 貨件來源名稱 |

### 成功回應範例

```json
{
  "isSuccess": true,
  "code": 200,
  "message": "操作成功",
  "errorCode": "",
  "timestamp": "2026-04-11T10:30:00+08:00",
  "data": [
    {
      "id": 1,
      "sourceType": "APP"
    },
    {
      "id": 2,
      "sourceType": "人工"
    }
  ]
}
```

### 可能錯誤

| HTTP Status | errorCode | 說明 |
| --- | --- | --- |
| 500 | INTERNAL_SERVER_ERROR | 系統錯誤 |

## 4. 檢查是否有原單資料

### Route

`POST /api/shipmentinbound/check`

### Request Body

| 欄位 | 型別 | 必填 | 說明 |
| --- | --- | --- | --- |
| trackingNo | string | 是 | 單號 |

### Request 範例

```json
{
  "trackingNo": "JETF0001"
}
```

### 成功回應 data 欄位

| 欄位 | 型別 | 說明 |
| --- | --- | --- |
| data | bool | 任一來源查到原單資料為 `true`，否則為 `false` |

### 判斷規則

符合以下任一條件就回傳 `true`:

- 海運: `SEA_ORDER_ORIGINAL.JETF_SERIAL = trackingNo`
- 空運: `ORIGINALLIST.TRACKINGNO = trackingNo`
- 空運: `ORIGINALLIST.DELIVERYNO = trackingNo`

### 成功回應範例

```json
{
  "isSuccess": true,
  "code": 200,
  "message": "操作成功",
  "errorCode": "",
  "timestamp": "2026-04-11T10:30:00+08:00",
  "data": true
}
```

### 可能錯誤

| HTTP Status | errorCode | 說明 |
| --- | --- | --- |
| 400 | VALIDATION_ERROR | trackingNo 未提供 |
| 500 | INTERNAL_SERVER_ERROR | 系統錯誤 |

## 5. 寫入入庫資料

### Route

`POST /api/shipmentinbound`

### Request Headers

| Header | 型別 | 必填 | 說明 |
| --- | --- | --- | --- |
| X-Timestamp | long | 是 | Unix time seconds |
| X-Signature | string | 是 | HMAC-SHA256 十六進位小寫簽章 |

### Request Body

| 欄位 | 型別 | 必填 | 說明 |
| --- | --- | --- | --- |
| inboundDate | string | 是 | 入庫日期，ISO 8601 格式 |
| trackingNo | string | 是 | 單號 |
| seqNo | string | 是 | 流水號 |
| locationCode | string | 是 | 儲位 |
| sourceType | byte | 否 | 貨件來源代碼 |
| returnTrackingNo | string | 否 | 退回的追蹤編號 |

### Request 範例

```json
{
  "inboundDate": "2026-04-10T09:00:00+08:00",
  "trackingNo": "JETF0001",
  "seqNo": "1",
  "locationCode": "A01-01",
  "sourceType": 1,
  "returnTrackingNo": ""
}
```

### HMAC 設定

| 項目 | 值 | 說明 |
| --- | --- | --- |
| HMAC Key | `4CE2DDD2-501F-40B8-B10B-C35B79B404EC` | Android 端與後端共用的簽章金鑰 |
| Allowed Clock Skew | `5` 分鐘 | `X-Timestamp` 與後端時間誤差不可超過 5 分鐘 |

### 簽章規則

後端會以 UTF-8 將以下字串組成 payload，並使用 HMAC Key 做 HMAC-SHA256:

```text
{X-Timestamp}
{InboundDate:o}
{TrackingNo}
{SeqNo}
{LocationCode}
{SourceType}
{ReturnTrackingNo}
```

簽章結果需轉成十六進位小寫字串後放入 `X-Signature`。

### 成功回應 data 欄位

| 欄位 | 型別 | 說明 |
| --- | --- | --- |
| data | bool | 成功寫入時固定為 `true` |

### 寫入規則

- 先檢查重複資料
- 若 `ShipmentInbound` 中已存在相同 `TrackingNo`，且 `OutboundDate < 現在時間 - 3天`，則視為重複
- 會優先查海運原單，海運查無才查空運原單
- 若海運有資料，`dataType = 海運`
- 若空運有資料，`dataType = 空運`
- 若都查不到，`dataType = ""`，`isOrderOriginal = false`
- 當 `tax` 或 `ccfee` 任一大於 `0` 時，`fee = 30`；否則 `fee = 0`

### 成功回應範例

```json
{
  "isSuccess": true,
  "code": 200,
  "message": "入庫資料寫入成功",
  "errorCode": "",
  "timestamp": "2026-04-11T10:30:00+08:00",
  "data": true
}
```

### 可能錯誤

| HTTP Status | errorCode | 說明 |
| --- | --- | --- |
| 400 | VALIDATION_ERROR | request body 缺欄位 |
| 401 | INVALID_SIGNATURE | 簽章錯誤或 timestamp 過期 |
| 409 | DUPLICATE_TRACKING_NO | 入庫資料重複 |
| 500 | INTERNAL_SERVER_ERROR | 系統錯誤 |

## Android 串接建議流程

1. APP 啟動後先呼叫 `GET /api/app/version-check`
2. 若 `forceUpdate = true`，引導使用者下載 APK，不允許繼續登入
3. 若可繼續使用，再呼叫 `POST /api/auth/login`
4. 入庫作業先呼叫 `POST /api/shipmentinbound/check` 判斷是否有原單
5. 送出入庫時呼叫 `POST /api/shipmentinbound`，並帶上 `X-Timestamp` 與 `X-Signature`

## 目前 API 清單總覽

| 方法 | 路由 | 用途 |
| --- | --- | --- |
| GET | `/api/app/version-check` | 檢查 APP 版本 |
| POST | `/api/auth/login` | 登入 |
| GET | `/api/shipmentinbound/source-types` | 取得貨件來源 |
| POST | `/api/shipmentinbound/check` | 檢查是否有原單資料 |
| POST | `/api/shipmentinbound` | 寫入入庫資料 |