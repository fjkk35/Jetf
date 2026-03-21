## Context

此需求的資料來源橫跨兩個 SQL Server 資料庫與一個 FTP 檔案伺服器。API 必須先依 TaxNumber 從 DATA_CENTER.dbo.CLEARANCE_TAX 找到資料類型與合併編號，再依資料類型切換查詢來源取得 CustCode，之後回到 jetf 資料庫確認目前登入使用者是否有該 CustCode 的存取關聯，最後查詢 jetf.dbo.Clearance_Tax_Pdf 並下載 FTP 上的 PDF。由於流程同時包含授權、跨系統查詢與檔案下載，必須先固定技術決策與錯誤邊界。

## Goals / Non-Goals

**Goals:**
- 提供受 JWT 保護的稅金單下載 API，請求只需傳入 TaxNumber。
- 使用 EF Core 查詢 jetf 與 DATA_CENTER，避免混用原始 SQL。
- JWT 必須攜帶 UserId claim，讓服務層可直接取得目前登入者識別。
- 成功回應直接輸出 application/pdf，供瀏覽器或前端直接下載與預覽。
- 查無資料、無綁定關聯、找不到 PDF 或 FTP 檔案缺失時，回傳一致的錯誤格式。

**Non-Goals:**
- 不在本次變更中實作 PDF 快取、批次下載或多檔壓縮。
- 不處理 FTP 憑證加密儲存；先沿用應用程式設定，後續再視部署流程移至 Secret Store。
- 不在此次變更中建立完整客戶維護後台。

## Decisions

### 成功回應直接回傳 PDF 檔案，而非 byte/base64 包裝 JSON
這個 API 的核心用途是下載或預覽稅金單。若回傳 byte 陣列或 base64，會額外膨脹 payload、增加前端轉換成本，也不利於瀏覽器直接處理。因此成功路徑直接回傳 application/pdf 檔案內容；只有失敗時才使用 ApiResponse 錯誤格式。

### 使用雙 DbContext 分別對應 jetf 與 DATA_CENTER
jetf 與 DATA_CENTER 具有不同連線字串與責任範圍。以兩個 DbContext 分離可讓查詢語意更清楚，也能避免將不同資料庫實體混入同一個 context。服務層只負責跨 context 協調流程。

### 以 ClaimTypes.NameIdentifier 作為 UserId claim 的主要來源
JWT 會同時保留使用者名稱與使用者 Id，其中 UserId 使用 ClaimTypes.NameIdentifier 儲存，受保護 API 可直接從 ClaimsPrincipal 讀取，不必再次查詢 TaxPortalUser。這符合需求中避免重查使用者資料的要求。

### 以 TaxPortalCustomer 查無資料視為不可取得該稅單
當登入者與 CustCode 沒有對應關聯時，API 回傳 404，而不是回傳是否存在該稅單的更多資訊。這樣可以降低未授權使用者透過回應差異推測資料是否存在的風險。

### FluentFTP 使用設定化連線並在請求期間即時下載
FTP 連線資訊放在設定檔並透過 Options 綁定。服務層在每次請求時建立連線、驗證檔案存在後下載 PDF 內容，避免長駐連線管理複雜度。若後續效能有壓力，再評估連線池或快取。

## Risks / Trade-offs

- TaxPortalUser.Id 為 bigint，但 TaxPortalCustomer.TaxPortalUserId 為 int：服務層需進行安全轉型，若超出範圍需明確失敗。
- CLEARANCE_TAX、ORIGINALLIST 與 SEA_ORDER_ORIGINAL 都可能有重複資料：本次先取第一筆符合條件資料，若未來需要更精準排序，再擴充規則。
- FTP 為外部相依，若網路異常或檔案不存在，API 將回傳 404 或 502；這會直接影響下載成功率。
- 成功回應不是 ApiResponse，而是檔案串流：前端需要依 Content-Type 判斷成功路徑，但這比 JSON 包裝更符合檔案下載語意。

## Migration Plan

1. 在設定檔新增 DATA_CENTER 連線字串與 FTP 設定。
2. 部署 FluentFTP 套件與新服務後，先以已知可用的 TaxNumber 驗證完整查詢鏈路。
3. 使用登入 API 取得 JWT，確認 Token 內含 UserId claim，再測試下載端點。
4. 若發現 FTP 無法連線，可先停用新端點或回滾本次變更，不影響既有登入流程。

## Open Questions

- CLEARANCE_TAX 與 PDF 資料若存在多筆相同 TaxNumber，目前採第一筆與最新建立時間 PDF；若業務需要特定排序規則，需再補充。
- FTP 憑證目前依需求寫在設定檔，正式環境建議後續改由安全憑證管理機制提供。