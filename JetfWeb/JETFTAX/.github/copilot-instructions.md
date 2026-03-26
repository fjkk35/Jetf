1.回復一律使用中文
2.前端使用Angular.ts，檔案Scripts/ng-controllers/{controllers}/{controllers}.ts
3. cshtml不用再引用Angular.js檔案
4.Service的Model寫在，Services/{controller}Service/Domain/{名稱}Model.cs
5.匯出 Excel 使用 NPOI 套件，相關擴充方法已置於 Service/Extensions/NPOIExtensions.cs。請優先使用既有的擴充方法，如有不足再於此檔案中新增。」
6. 查詢資料庫採用 Dapper 套件進行操作。
7. Domain/Model 每個欄位都需要加上註解
8.匯出Excel使用NPOI，JETFTAX\Service\Extensions\NPOIExtensions.cs有擴充方法可以使用
9.Enum轉換使用擴充方法JETFTAX\Service\Extensions\EnumerableExtensions.cs
10.### Excel 批量上傳處理規範
* 系統在執行 Excel 批量上傳前，**必須先驗證所有資料列**。
* 若任一資料列驗證失敗，則：

  * 視為整批上傳失敗
  * **不得更新任何資料庫（DB）資料**
* 上傳失敗時，必須回傳驗證結果並於畫面顯示錯誤清單，錯誤清單需包含：

  * 上傳欄位名稱
  * 對應的失敗原因
11.檔案編碼一律使用 UTF-8 有簽章，避免因編碼問題導致資料錯亂。