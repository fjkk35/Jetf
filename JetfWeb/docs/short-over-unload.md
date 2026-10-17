# 短溢卸

在「清關作業 → 短溢卸」選擇「台中-短卸」，下載範例、填寫 Excel 後上傳。上傳處理完成後會自動下載匯出的 Excel。支援 `.xlsx`、`.xls`，每個非空白頁籤的第一列皆須為「主提單號碼、分提單號碼、申報、實到」。件數使用既有 `ToInt()` 擴充方法轉換，空白或無法轉換為整數時使用 0；提單號碼前後空白會移除。

部署時先執行 `SQL/ShortOverUnloadAuthority.sql`，再於權限群組管理指派 `ShortOverUnload`（短溢卸）。使用者重新登入後生效。畫面、範例下載、上傳與報告下載均使用此權限。SQL 可重複執行，不會自動擴大既有群組的權限。

資料查詢、Excel 匯入與匯出均集中在 `ShortOverUnloadService`。匯出直接使用 NPOI 建立 Excel，不需要部署範本檔案，依使用者提供的「台中港-短卸格式.xlsx」建立字型、字級、欄寬、列高、框線、合併儲存格及列印設定，並加寬 C、G、H 欄，避免表頭文字拆行。依主提單分組，每張最多 14 筆；各主號及各頁內保留匯入順序。

資料查詢使用 EF LINQ，透過 `DataCenterDbContext`、`JetfDbContext` 及 `AsNoTracking()` 取得資料。每個主號讀取一次 `DATA_CENTER.dbo.CES_MAIN_ORDER`（`TYPE='O'`）的 `FIELD_A`，並取得 `jetf.dbo.SEA_MANIFEST_UPLOAD.MANIFEST`、`DATA_CENTER.dbo.SEA_ORDER_EDIT`（`ITEM_NO=1`）的貨物說明與櫃號。艙單與製單查詢同時以主提單及本次匯入的分提單清單篩選。相同船掛在單次匯入中共用 GB330、GB325 的查詢結果。船掛、艙單、製單或 GB330 符合船掛的明細查到多筆時取第一筆；查無必要資料或關港貿查詢失敗時會提示原因，不產生不完整報告。

GB330 使用 `ImoNo`、`VoyageFlightNo`、`VslName`；GB325 使用 `ProcessCustCd`、`EstArDate`。匯入日期於上傳時以台北時間取得，所有頁籤使用同一日期。預定到港日與匯入日寫入真正的 Excel 日期值，格式為 `yyyy/M/d`。

上線前需以具備權限的帳號驗證實際資料查詢。
