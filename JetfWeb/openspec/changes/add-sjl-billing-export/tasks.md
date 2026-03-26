## 1. 規格與頁面骨架

- [x] 1.1 在 JETFTAX/Scripts/_Layout.js 的「捷穩通」群組新增「捷利帳單」選單
- [x] 1.2 新增 SjlBillingController 與 Index 頁面，頁面提供日期起、日期迄、派件公司與下載按鈕
- [x] 1.3 新增 Angular.js + TypeScript controller，完成前端欄位驗證與下載流程

## 2. 後端查詢與模型

- [x] 2.1 新增 SjlBillingService 與 Domain Models，封裝查詢條件、查詢結果與匯出資料
- [x] 2.2 以 Dapper 實作清關資料、SEA_ORDER_ORIGINAL 與 SjlShippingData 的查詢
- [x] 2.3 將日期迄轉為加 1 天後的小於條件，並依有效派件公司過濾資料
- [x] 2.4 補上 OTransName、CreatedTime、ScanCargoTime 等查詢欄位與映射

## 3. 計價與匯出

- [x] 3.1 實作超才費、最低收費 300 元與同地址同日第一筆掛額邏輯
- [x] 3.2 實作捷通超重費、重量計費與應計價(擇大值)邏輯
- [x] 3.3 以 NPOI 產出大榮與捷通兩種不同欄位格式的 Excel，並調整代收與稅金欄位順序
- [x] 3.4 依清關日期、地址、運送編號排序輸出資料
- [x] 3.5 新增稅金頁籤與彙總頁籤

## 4. 驗證

- [ ] 4.1 驗證大榮匯出欄位與金額計算符合規格
- [ ] 4.2 驗證捷通匯出欄位、重量計費與應計價符合規格
- [ ] 4.3 驗證查無資料、缺少查詢條件與日期區間錯誤時的回應