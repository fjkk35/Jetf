## Why

目前系統已提供「捷利托運資料上傳」功能，能將捷利托運資料寫入 jetf.dbo.SjlShippingData，但尚未提供依清關日期區間與派件公司匯出「捷利帳單」Excel 的能力。營運端目前需要手動整理大榮與捷通帳單資料，不僅作業成本高，也容易因最低收費與材積、重量計價規則而產生人工錯誤。近期 SQL 已調整為一次查出「大榮」與「捷通」兩種派件來源，因此匯出邏輯也需要同步改為以實際有效派件公司做後置篩選，而非只依單一來源欄位查詢。

## What Changes

- 在 JETFTAX/Scripts/_Layout.js 的「捷穩通」選單下，於「捷利托運資料上傳」底下新增「捷利帳單」功能入口。
- 新增 Angular.js + TypeScript 頁面，提供日期起、日期迄、派件公司三個查詢條件。
- 新增後端查詢與 Excel 匯出流程，一次查出大榮與捷通資料後，再依有效派件公司輸出指定帳單。
- 將大榮與捷通的材積、最低收費與重量計費規則明確化，避免各人以不同方式計算。
- 新增「稅金」與「彙總」兩個頁籤，讓營運可直接查看稅金明細與每日運費加總。

## Capabilities

### New Capabilities
- sjl-billing-export: 使用者可依日期區間與派件公司下載捷利帳單 Excel，且 Excel 內含主表、稅金、彙總三個頁籤。

## Impact

- 影響選單設定、MVC 頁面、Angular.js Controller、後端 Controller 與 Service。
- 需新增查詢模型、Excel 匯出模型與 NPOI 多工作表匯出邏輯。
- 查詢資料來源包含 DATA_CENTER.dbo.CLEARANCE_INFO、DATA_CENTER.dbo.SEA_ORDER_ORIGINAL 與 jetf.dbo.SjlShippingData。
- 查詢需同時處理 SEA_ORDER_ORIGINAL.TRANS_NAME 與 SjlShippingData.TransName 的派件判定。
- 初版不涉及資料寫回，僅提供查詢與 Excel 匯出。