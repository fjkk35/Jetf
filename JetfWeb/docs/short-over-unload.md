# 短溢卸

在「清關作業 → 短溢卸」選擇「台中-短卸」或「台中-溢卸」，下載範例、填寫 Excel 後上傳。資料來源選項由 `ShortOverUnload/GetDataSourceOptions` 依 `ShortOverUnloadDataSource` 列舉回傳，畫面預設選取第一項；選項尚未載入或載入失敗時不能匯出。兩種格式共用上傳流程及四欄範例，上傳處理完成後會自動下載所選格式的 Excel。支援 `.xlsx`、`.xls`，每個非空白頁籤的第一列皆須為「主提單號碼、分提單號碼、申報、實到」。件數使用既有 `ToInt()` 擴充方法轉換，空白或無法轉換為整數時使用 0；提單號碼前後空白會移除。

查無資料時照常下載 Excel，對應欄位留白，畫面以「未取得資料」表格列出主提單號碼、分提單號碼及未取得欄位。同一主分號只顯示一次，缺少欄位合併去重；重新上傳時清除前一次清單。服務直接從補值結果檢查欄位並完成分組，不增加查詢次數；清單僅保留畫面使用的三個欄位，Controller 直接回傳，不再記錄查詢來源、來源頁籤或列號。

部署時先執行 `SQL/ShortOverUnloadAuthority.sql`，再於權限群組管理指派 `ShortOverUnload`（短溢卸）。使用者重新登入後生效。畫面、範例下載與上傳使用此權限。報告沿用共用下載流程：上傳成功以 GUID 作為 `TempData` 的鍵儲存檔案內容，回傳 `fileGuid` 與 `fileName`，前端呼叫 `Download/DownloadFile` 下載；共用下載端點使用既有登入驗證。SQL 可重複執行，不會自動擴大既有群組的權限。

資料查詢、Excel 匯入與匯出均集中在 `ShortOverUnloadService`。匯出直接使用 NPOI 建立 Excel，不需要部署範本檔案，依使用者提供的「台中港-短卸格式.xlsx」建立字型、字級、欄寬、列高、框線、合併儲存格及列印設定，並加寬 C、G、H 欄，避免表頭文字拆行。依主提單分組，每張最多 14 筆；各主號及各頁內保留匯入順序。

資料查詢使用 EF LINQ，透過 `DataCenterDbContext`、`JetfDbContext` 及 `AsNoTracking()` 取得資料。每個主號讀取一次 `DATA_CENTER.dbo.CES_MAIN_ORDER`（`TYPE='O'`）的 `FIELD_A`，並取得 `jetf.dbo.SEA_MANIFEST_UPLOAD.MANIFEST`、`DATA_CENTER.dbo.SEA_ORDER_EDIT`（`ITEM_NO=1`）的貨物說明與櫃號。艙單與製單查詢同時以主提單及本次匯入的分提單清單篩選。相同船掛在單次匯入中共用 GB330、GB325 的查詢結果。船掛、艙單、製單或 GB330 符合船掛的明細查到多筆時取第一筆。查無船掛、裝貨型態、製單或 GB 欄位資料時，對應欄位留白並繼續匯出，其他查得到的欄位仍正常填入；沒有船掛時略過 GB 查詢。上傳格式不符、資料庫連線失敗或關港貿查詢執行失敗仍會提示原因。

GB330 使用 `ImoNo`、`VoyageFlightNo`、`VslName`；GB325 使用 `ProcessCustCd`、`EstArDate`。匯入日期於上傳時以台北時間取得，所有頁籤使用同一日期。預定到港日與匯入日寫入真正的 Excel 日期值，格式為 `yyyy/M/d`；查無或無法解析的預定到港日留白。

上線前需以具備權限的帳號驗證實際資料查詢。

## 台中-溢卸

實作前已使用參考檔的船掛 `15PANF`、主號 `IPC260818711EX` 及分號 `LP00834954832277` 實際核對三支 API，必要欄位皆有回傳：

| API | 回傳欄位 | 用途 |
| --- | --- | --- |
| GB330 | `ImoNo`、`VoyageFlightNo`、`GridModel.VslName`（表頭為 `ShipName`）、`VslSign`、`ShipCoCd` | 船機代碼、航次、船名、呼號、船公司 |
| GB325 | `ProcessCustCd`、`EstArDate`、`LastPorCd`、`OfficerName` | 受理關別、到港日、前一國外港、船長 |
| GB326 query1 | `MftNo` | 主號查詢取得主艙號 `JF06`；帶入分號取得分艙號 `HI50` |

溢卸沿用短卸的船掛、裝貨型態及 `ITEM_NO=1` 櫃號查詢，新增每主號一次的 `SEA_ORDER_EDIT` 製單查詢，以主分號與 `GW > 0` 篩選。同分號有多筆有效毛重資料時優先選 `MERGE_OVER_FLAG='O'`；若沒有符合者則依 `ROW_ID` 順序取第一筆。各欄位採用該筆的 `JETF_ID`、`GW`、`PIECE`、`PIECE_UNIT`、`MARKS`、`ITEM_NAME`、`QUANTITY`、`UNIT_PRICE`、`IMPORTER`、`IM_ADD`、`CONT_TYPE`、`CONT_NO`、`SEALNO`。整筆製單查無資料時，這些欄位全部留白，數值欄位也不補零。

GB326 使用既有 `CptPortalApi.GetGb326Async`，參數沿用 `tab1.vslRegNo`、`tab1.mawb`、`tab1.hawb` 與 `choice=B`。同主號的主艙號只查一次，同主號的相同分號也只查一次。查無艙號或回傳船掛、主分號不符時，該艙號留白並繼續匯出。

每個主號產生六類報表：第一類頁籤名稱為「(一)短溢卸報告」，沿用短卸版面並將短溢卸註記改為「溢卸」；新增溢卸申請書（14 筆）、裝機明細表（46 筆）、艙單更正申請書（25 筆）、BL、准單申請書（14 筆）。所有頁籤一律使用「頁籤名稱_主號_頁碼」，單一主號也會加上主號，例如 `(一)短溢卸報告_IPC260818711EX_1`。依主號與筆數建立分頁，明細保留上傳順序，共計艙單數使用同主號的上傳分號筆數。BL 每個主號一張，標記與櫃號／封條組合去重後換行顯示，箱型固定 `40HC`。

BL 的 C16、F16 每個主號一次查詢 `CES_MAIN_ORDER` 的 `TYPE='ER'` 與 `TYPE='SEO'`，同類型多筆時依 `ROW_ID` 取第一筆。C16 將兩者的 `COUNT_RECORD` 相加後加上 `PACKAGES`，例如 `1260PACKAGES`。F16 先對 ER 的 `SUM_WEIGHT` 無條件捨去小數，再加上 SEO 的完整 `SUM_WEIGHT`，加總後四捨五入至小數點後兩位（`MidpointRounding.AwayFromZero`），固定顯示兩位小數並加上 `KGS`，例如 `8048.619` 顯示為 `8048.62KGS`、`10.5` 顯示為 `10.50KGS`。件數與毛重各自判斷，ER、SEO 其中一方有值就加總，缺少的一方以零計算；只有兩方該欄位都沒有值時才保留空白，並在畫面清單列出「BL總件數」或「BL總毛重」。數值零視為有效資料，已知的另一個合計仍正常顯示。

`TaichungOverUnloadLayout` 直接以 NPOI 定義參考檔的字型、字級、欄寬、列高、框線、合併範圍及固定文字；所有背景皆無填色，部署及執行時不讀取參考檔。頁籤二 B26「財政部關務署台中關」的列高調為 30 點，避免原本 20.25 點列高裁切 22 點文字。頁籤四補齊原申報區的左右邊界、各列橫線及表格底部缺線，保留原申報／更正內容合併欄的內部留白；合併完成後，明確將 A5:A38 右框線與 B5:B38 左框線設為不透明黑色（ARGB `FF000000`），並使用與外框相同的 `Medium` 線寬，提高整頁縮小列印預覽時的可見度。新增工作表各自設定單頁列印範圍。日期使用台北匯入日期並以真正 Excel 日期值輸出 `yyyy/M/d`。

BL 同時重建範例中的 10 條水平底線、5 條虛線分隔及 14 個方框。原印章 PNG 以 `Resources/ShortOverUnload/BillOfLadingSeal.png` 內嵌於 Service 組件，沿用範例的錨點與尺寸，同一工作簿的多個 BL 頁籤共用圖片資料。水平自訂路徑改用 NPOI 可完整輸出的水平線；混合字型文字逐段建立，避免後一次字型設定覆蓋前面的段落。

資料查詢、未取得資料清單、頁籤安排及各頁籤填值方法集中在 `ShortOverUnloadService.cs`，不使用 partial 分檔。各類頁籤皆有獨立方法及頁籤名稱註解；頁籤一由 `AddShortOverUnloadReportSheets` 產生，溢卸申請書與准單申請書分別由 `AddOverUnloadApplicationSheets`、`AddPermitApplicationSheets` 產生。`Export` 負責建立工作簿、安排報告與附件及輸出檔案。

先編譯方案，再執行回歸測試：

```powershell
.\tests\Run-ShortOverUnloadRegression.ps1 -ReferenceWorkbook 'C:\Users\user\Desktop\溢卸格式.xlsx'
node .\tests\ShortOverUnloadUiRegression.js
```

不指定參考檔時仍會驗證分頁邊界、各類明細順序、主分艙號、數值與日期型別、BL 保留欄位與繪圖物件、跨主號頁籤撞名、溢卸資料選取、API 對應及快取，以及既有短卸輸出。另以記憶體資料庫查詢及 API 回應驗證所有來源查無資料、部分來源缺少資料、日期無法解析、回傳船掛不符等情況，確認匯出欄位留白且保留其他已知值，並驗證未取得資料清單保留所有缺少欄位、依主分號分組及合併去重。畫面測試驗證清單呈現、自動下載、重新上傳清除清單，以及成功與失敗狀態。指定參考檔時另逐格比對五張新增報表的尺寸、字型、對齊、框線及合併範圍，並逐字比對混合字型、驗證 BL 繪圖錨點及原印章圖片內容。輸出的測試檔位於系統暫存目錄，測試不連線正式資料庫。
