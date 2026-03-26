## 1. 規格與頁面骨架

- [x] 1.1 在捷穩通選單新增「捷利托運資料查詢」入口
- [x] 1.2 新增 SjlBatchImport/Search 頁面與查詢表單
- [x] 1.3 新增查詢結果表格、分頁與派件公司修改 modal

## 2. 後端查詢與模型

- [x] 2.1 新增 SjlBatchImportSearch request/response/domain model
- [x] 2.2 實作 SjlShippingData 查詢與分頁
- [x] 2.3 新增 SjlBatchImportController 的 SearchData API

## 3. 派件公司修改

- [x] 3.1 新增 UpdateTransName API 與 request model
- [x] 3.2 以 transaction 更新 SjlShippingData.TransName
- [x] 3.3 寫入 SjlShippingDataTransNameHistory 歷史資料

## 4. 驗證

- [ ] 4.1 驗證查詢條件與分頁回傳正確
- [ ] 4.2 驗證相同派件公司不會送出修改
- [ ] 4.3 驗證修改後主表與歷史表資料正確