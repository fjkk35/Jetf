## Why

目前系統已提供「捷利托運資料上傳」功能，能將捷利托運資料寫入 jetf.dbo.SjlShippingData，但尚未提供依清關日期區間與派件公司匯出「捷利帳單」Excel 的能力。營運端目前需要手動整理大榮與捷通帳單資料，不僅作業成本高，也容易因最低收費與材積、重量計價規則而產生人工錯誤。

## What Changes

- 在 JETFTAX/Scripts/_Layout.js 的「捷穩通」選單下，於「捷利托運資料上傳」底下新增「捷利帳單」功能入口。
- 新增 Angular.js + TypeScript 頁面，提供日期起、日期迄、派件公司三個查詢條件。
- 新增後端查詢與 Excel 匯出流程，依派件公司輸出不同欄位格式的帳單檔案。
- 將大榮與捷通的材積、最低收費與重量計費規則明確化，避免各人以不同方式計算。

## Capabilities

### New Capabilities
- sjl-billing-export: 使用者可依日期區間與派件公司下載捷利帳單 Excel。

## Impact

- 影響選單設定、MVC 頁面、Angular.js Controller、後端 Controller 與 Service。
- 需新增查詢模型、Excel 匯出模型與 NPOI 匯出邏輯。
- 查詢資料來源包含 DATA_CENTER.dbo.CLEARANCE_INFO、DATA_CENTER.dbo.SEA_ORDER_ORIGINAL 與 jetf.dbo.SjlShippingData。
- 初版不涉及資料寫回，僅提供查詢與 Excel 匯出。