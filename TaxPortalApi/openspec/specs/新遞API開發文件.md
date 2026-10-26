# 新遞金額與簽收單 API

新遞串接規格與 HMAC-SHA256 簽章產生說明。

## API 資訊

| 項目 | 內容 |
| --- | --- |
| Base URL | `{BaseUrl}`，由部署方提供 TaxPortalApi 的完整根網址，含 IIS 虛擬目錄（如有） |
| 金額查詢 | `GET {BaseUrl}/api/SeaShenzhen/get-amount` |
| 簽收單查詢 | `GET {BaseUrl}/api/SeaShenzhen/get-sign-receipts` |
| 驗證方式 | HMAC-SHA256，每次請求驗證；不需要 JWT |
| Secret Key | `29681c667764ca0a75973f6a3456b9ca0ab38dd05696e173db336080ca0d4348`，對應伺服器 `SeaShenzhenApi:SecretKey` |
| Timestamp 有效時間 | 3 分鐘（180 秒）；`0 <= 伺服器時間 - timestamp <= 180`，超過或使用未來時間皆回傳 401 |
| 編碼 | UTF-8；JSON 回應欄位使用 camelCase |

## Request

兩支 API 使用相同的 Query Parameters。物流貨號以完整值比對，不使用單號區間判斷。

| 參數 | 必填 | 型別 | 說明 |
| --- | --- | --- | --- |
| `trackinngNo` | 是 | string | 物流貨號，對應 SeaShenzhenOriginal.JetfSerial，最多 20 字元，前後不可有空白 |
| `timestamp` | 是 | long | 呼叫當下的 UTC Unix Timestamp，單位為秒 |
| `sign` | 是 | string | 64 字元小寫十六進位 HMAC-SHA256 簽章 |

```text
{BaseUrl}/api/SeaShenzhen/get-amount?trackinngNo={物流貨號}&timestamp={Unix秒數}&sign={簽章}
{BaseUrl}/api/SeaShenzhen/get-sign-receipts?trackinngNo={物流貨號}&timestamp={Unix秒數}&sign={簽章}
```

## Sign 產生方式

先依指定名稱、大小寫、順序及分隔符號組合原文：

```text
trackinngNo={trackinngNo}&timestamp={timestamp}
```

再以 UTF-8 編碼 Secret Key 與原文，計算 HMAC-SHA256，轉成小寫十六進位字串：

```text
sign = lowercase_hex(HMAC-SHA256(UTF8(Secret Key), UTF8(簽章原文)))
```

- 原文使用尚未 URL Encode 的參數值；完成簽名後，再對網址的 Query 參數做 URL Encode。
- timestamp 使用十進位 Unix 秒數，不加千分位、不使用毫秒。
- sign 使用小寫十六進位，長度固定 64 字元。
- 實際呼叫請使用當下時間；伺服器和呼叫端應保持時間同步。
- 同一貨號及 timestamp 的簽章可用於上述兩支查詢 API；有效時間內可重複查詢。

### 固定測試向量

以下使用本文件提供的 Secret Key 計算固定測試向量，可用來核對簽章結果。

```text
Secret Key：29681c667764ca0a75973f6a3456b9ca0ab38dd05696e173db336080ca0d4348
簽章原文：trackinngNo=1056864804&timestamp=1780202356
sign：7c5b53e4a092ed623b9e2c04b572fc6b9345efb68a6f166bf16fdee4e2c5e40f
```

固定 timestamp 已不是有效請求時間，僅用於驗證簽章演算法。

### C# 範例

```csharp
var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
var payload = $"trackinngNo={trackinngNo}&timestamp={timestamp.ToString(CultureInfo.InvariantCulture)}";
var hash = HMACSHA256.HashData(
    Encoding.UTF8.GetBytes(secretKey),
    Encoding.UTF8.GetBytes(payload));
var sign = Convert.ToHexString(hash).ToLowerInvariant();
var url = $"{baseUrl.TrimEnd('/')}/api/SeaShenzhen/get-amount"
    + $"?trackinngNo={Uri.EscapeDataString(trackinngNo)}"
    + $"&timestamp={timestamp.ToString(CultureInfo.InvariantCulture)}&sign={sign}";
```

需引用 System.Globalization、System.Security.Cryptography、System.Text。簽收單查詢將路徑改成 get-sign-receipts，簽名規則相同。

## 1. 查詢金額

```http
GET /api/SeaShenzhen/get-amount?trackinngNo=1056864804&timestamp={目前的Unix秒數}&sign={簽章}
```

金額規則依 SeaShenzhenOriginalQuery/Index；支付方式依此次需求回傳原始值。

| 回應 data 欄位 | 型別 | 來源與規則 |
| --- | --- | --- |
| `trackinngNo` | string | SeaShenzhenOriginal.JetfSerial |
| `cod` | number | 有 ShenzhenFeeMaster 資料取 Cod；否則直接取 SeaShenzhenOriginal.Cc 原值；Cc 未填則 0 |
| `tax` | integer | ShenzhenFeeMaster.Tax；沒有轉檔資料則 0 |
| `fee` | integer | ShenzhenFeeMaster.Fee；沒有轉檔資料則 0 |
| `taxPayment` | string / null | SeaShenzhenOriginal.TaxPayment 原始值，包含 XD、C 或 null，不轉中文、不修改原值 |

先以物流貨號查詢原始託運資料，再以 JetfSerial = ShenzhenFeeMaster.DlvInv 查詢金額。若同一貨號有多筆，兩個資料來源各依 DataDate、Id 由新至舊取第一筆。Cod 為 0 時仍使用 0，不回退至 Cc；到付款不加總稅金、手續費，也不取 ToDlvCod。未取得的金額回傳 0。

### 成功回應

```json
{
  "data": {
    "trackinngNo": "1056864804",
    "cod": 100,
    "tax": 200,
    "fee": 30,
    "taxPayment": "C"
  },
  "isSuccess": true,
  "code": 200,
  "message": "操作成功",
  "errorCode": "",
  "timestamp": "2026-10-24T10:00:00+08:00"
}
```

沒有稅金轉檔資料時，data 範例：

```json
{
  "trackinngNo": "1056864804",
  "cod": 123.456,
  "tax": 0,
  "fee": 0,
  "taxPayment": "XD"
}
```

## 2. 查詢簽收單

```http
GET /api/SeaShenzhen/get-sign-receipts?trackinngNo=1056864804&timestamp={目前的Unix秒數}&sign={簽章}
```

先確認 SeaShenzhenOriginal 有此物流貨號，才查詢 dbo.Cargo_Sign_Receipt，以 Jetf_Serial 比對貨號，依 Id 遞增讀取 FilePath 指定的 FTP 圖片。與 ShipmentInboundRecord/Index 使用相同的簽收單資料來源。

同一貨號可能有多張圖片，以 images 清單回傳。空白路徑、檔案不存在或內容為空的圖片會略過；全部皆不可讀取時回傳 404，FTP 連線或下載錯誤回傳 502。

| 回應 data 欄位 | 型別 | 說明 |
| --- | --- | --- |
| `trackinngNo` | string | 查詢的物流貨號 |
| `images` | array | 可讀取的簽收圖片清單 |
| `images[].fileName` | string | 圖片檔名；未填檔名時取路徑中的檔名 |
| `images[].contentType` | string | 依檔名判斷的 MIME，例如 image/jpeg；無法判斷時為 application/octet-stream |
| `images[].contentBase64` | string | 圖片 Base64 內容，不含 data URI 前綴 |

### 成功回應

```json
{
  "data": {
    "trackinngNo": "1056864804",
    "images": [
      {
        "fileName": "receipt.png",
        "contentType": "image/png",
        "contentBase64": "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+a9E8AAAAASUVORK5CYII="
      }
    ]
  },
  "isSuccess": true,
  "code": 200,
  "message": "操作成功",
  "errorCode": "",
  "timestamp": "2026-10-24T10:00:00+08:00"
}
```

若要在網頁顯示，可組成 `data:{contentType};base64,{contentBase64}`；下載檔案則直接解碼 Base64。上述圖片僅為 1 × 1 像素示意圖。

## 錯誤回應

```json
{
  "data": null,
  "isSuccess": false,
  "code": 401,
  "message": "Sign 驗證失敗",
  "errorCode": "",
  "timestamp": "2026-10-24T10:00:00+08:00"
}
```

新遞 API 以 HTTP 狀態碼與 message 說明錯誤，統一回應格式中的 errorCode 為空字串。

| HTTP Status | 說明 |
| --- | --- |
| 400 | 參數未填或格式錯誤 |
| 400 | 物流貨號格式錯誤 |
| 401 | Timestamp 超出有效時間 |
| 401 | Sign 驗證失敗 |
| 404 | 查無託運資料 |
| 404 | 查無簽收單圖片 |
| 502 | 簽收單圖片讀取失敗 |
| 503 | 簽名金鑰未設定 |
| 503 | 簽收單 FTP 未設定 |
| 500 | 系統發生未預期錯誤 |

## 伺服器設定

專案 appsettings.json 已設定本文件的 Secret Key。appsettings.SeaShenzhen.example.json 包含相同金鑰及 FTP 設定範例，範例檔不會自動載入。部署時請使用相同設定，或使用以下環境變數：

| 設定名稱 | 環境變數 | 說明 |
| --- | --- | --- |
| SeaShenzhenApi:SecretKey | SeaShenzhenApi__SecretKey | 本文件列出的 HMAC 金鑰 |
| CargoSignReceiptFtp:Host | CargoSignReceiptFtp__Host | 既有簽收單 FTP 主機 |
| CargoSignReceiptFtp:UserName | CargoSignReceiptFtp__UserName | 既有簽收單 FTP 帳號 |
| CargoSignReceiptFtp:Password | CargoSignReceiptFtp__Password | 既有簽收單 FTP 密碼 |

金額查詢不需要 FTP 設定。兩支 API 都需要有效 HMAC 金鑰；簽收圖片讀取另外需要 FTP 設定。延用既有 DefaultConnection 查詢 jetf 資料庫，無須新增資料表。

回應外層 timestamp 延用專案的伺服器本機時間；請求 timestamp 始終使用 UTC Unix 秒數。
