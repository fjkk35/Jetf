## Why

目前捷利托運資料僅提供上傳功能，缺少可直接檢視既有資料與修正派件公司的查詢頁。營運人員若要確認資料內容或補正派件公司，必須直接查資料庫或重新上傳，流程不夠直覺，也無法留下派件公司修改歷程。

## What Changes

- 在捷穩通模組新增「捷利托運資料查詢」功能入口。
- 新增查詢頁面，提供 CreatedTime 日期區間與運送編號查詢條件。
- 查詢結果支援分頁，顯示捷利托運資料主要欄位與派件公司。
- 提供修改派件公司的操作，使用彈窗下拉選單切換「大榮」與「捷通」。
- 修改派件公司時寫入 SjlShippingDataTransNameHistory 歷史資料表。

## Capabilities

### New Capabilities
- sjl-batch-import-search: 使用者可查詢捷利托運資料，並針對單筆資料修改派件公司與保留歷史紀錄。

## Impact

- 影響捷穩通選單、SjlBatchImport MVC controller/view/script 與後端 service/domain model。
- 查詢資料來源為 jetf.dbo.SjlShippingData。
- 派件公司修改會寫入 jetf.dbo.SjlShippingData 與 jetf.dbo.SjlShippingDataTransNameHistory。