## Overview

此功能為捷穩通模組下的帳單匯出頁面，與既有「捷利托運資料上傳」同屬一組作業。畫面提供查詢條件後，後端依指定 SQL 取得符合條件的清關與托運資料，套用派件公司對應欄位與計價規則，再以 NPOI 產出 Excel 檔供使用者下載。

初版以單一下載動作為主，不在畫面呈現查詢結果表格，避免與既有匯出型頁面風格不一致，也可減少前端維護成本。

## Menu And Permission

- 選單位置：JETFTAX/Scripts/_Layout.js 的「捷穩通」群組。
- 顯示方式：在現有「捷利托運資料上傳」項目後新增「捷利帳單」。
- URL：~/SjlBilling/Index
- 權限：初版沿用 Authority.SjlBatchImport，避免為單一匯出頁面額外拆新權限；若後續要分權，再拆成獨立 Authority.SjlBilling。

## UI Design

頁面使用 Angular.js 與 TypeScript，包含以下欄位與操作：

- 日期起：必填，日期選擇器。
- 日期迄：必填，日期選擇器。
- 派件公司：必填，下拉選單固定兩個選項。
  - 大榮
  - 捷通
- 下載 Excel：送出查詢並直接下載檔案。

前端驗證規則：

- 日期起、日期迄、派件公司皆不可空白。
- 日期起不得大於日期迄。

## Data Query

### Query Parameters

- @StartDate = request.日期起
- @EndDate = request.日期迄加 1 天，作為小於上限

### SQL

```sql
with info as (
    select distinct MAIN_NUMBER, BAG_NUMBER, SIGN_OUT_TIME
    from DATA_CENTER.dbo.CLEARANCE_INFO
    where SIGN_OUT_TIME >= @StartDate
      and SIGN_OUT_TIME < @EndDate
      and DATA_TYPE not in ('FTZ', 'TACT')
),
Original as (
  select MAINNUMBER, BL_NO, JETF_SERIAL, TRANS_NAME as OTransName
    from DATA_CENTER.dbo.SEA_ORDER_ORIGINAL a
  where TRANS_NAME in (N'捷通', N'大榮')
)
select
    b.SIGN_OUT_TIME,
    MAINNUMBER,
    BL_NO,
    JETF_SERIAL,
  OTransName,
  c.TransName,
  c.CreatedTime,
    c.BagNumber,
    c.Importer,
    c.OtherFee,
    c.Cod,
    c.ImporterAddr,
    c.ItemName,
    c.Qty,
    c.Volume,
    c.Gw,
    c.ImporterPhone,
    e.UploadTime as ScanCargoTime
from Original a
join info b on a.MAINNUMBER = b.MAIN_NUMBER and a.BL_NO = b.BAG_NUMBER
left join jetf.dbo.SjlShippingData c on a.JETF_SERIAL = c.JetfSerial
  left join jetf.dbo.PdtScanCargoUpload e on a.JETF_SERIAL = e.Data and e.TransNo in (11, 98)
```

### Query Notes

- 查詢範圍以清關日 SIGN_OUT_TIME 為準。
- 日期迄需轉為隔日零點，避免漏掉日期迄當天資料。
- 排除 DATA_TYPE 為 FTZ 與 TACT 的資料。
- SQL 先一次查出大榮與捷通兩種派件資料。
- 實際匯出前需先計算有效派件公司：若 SjlShippingData.TransName 有值則使用該值；若為空則回退使用 SEA_ORDER_ORIGINAL.OTransName。
- 匯出時再以有效派件公司比對 request.派件公司。
- 以 SjlShippingData 補齊收件人、費用、地址、品名、件數、材積、重量與電話等欄位。
- CreatedTime 供捷通主表「資料日期」欄位使用。
- ScanCargoTime 為字串欄位，供捷通主表「派送日」欄位使用；若可解析為日期則統一輸出 yyyy/MM/dd。

## Calculation Design

### Shared Rules

- 基本運費固定為 55。
- 超才費規則：當 Volume > 4 時，每超過 1 才加收 20 元，不足 1 才無條件進位。
  - 計算式：ceiling(Volume - 4) * 20
  - 例如：Volume = 4.1，超才費 = 20。
- 地址最低收費規則：以同一天清關且同地址為一組，先計算該組每筆的基本運費加超才費後加總。
  - 若分組合計小於 300，則第一筆總額調整為 300，其餘同組資料總額為 0。
  - 若分組合計大於或等於 300，則每筆總額維持各自的基本運費加超才費。
- 第一筆定義：依資料排序後，同一天清關且同地址分組中的第一筆資料。
- 排序規則：先依清關日期升冪，再依地址升冪；同組內若仍需穩定排序，依運送編號升冪。

### 大榮計價

- 總額 = 基本運費 + 超才費，再套用地址最低收費規則。
- 大榮換單號初版保留空白欄位，不回填任何值。

### 捷通計價

- 總額 = 基本運費 + 超才費，再套用地址最低收費規則。
- 超重費規則：當 Gw > 20 時，每超過 1 公斤加收 5 元，不足 1 公斤無條件進位。
  - 計算式：ceiling(Gw - 20) * 5
  - 例如：Gw = 20.2，超重費 = 5。
- 重量計費 = 基本運費 + 超重費。
- 應計價(擇大值) = max(總額, 重量計費)。
- 捷通欄位中有兩個「基本運費」欄位，皆固定輸出 55：
  - 第一個「基本運費」用於材積計價區塊。
  - 第二個「基本運費」用於重量計價區塊。

## Excel Output

### 大榮欄位

1. 清關日 = SIGN_OUT_TIME
2. 運送編號 = JETF_SERIAL
3. 單據編號 = BL_NO
4. 大榮換單號 = 空白
5. 收件人 = Importer
6. 其他費用(稅金) = OtherFee
7. 代收 = Cod
8. 地址 = ImporterAddr
9. 品名 = ItemName
10. 件數 = Qty
11. 材積 = Volume
12. 重量 = Gw
13. 收件人電話 = ImporterPhone
14. 基本運費 = 55
15. 超才費 = 依超才費規則計算
16. 總額 = 依最低收費規則後之結果

### 捷通欄位

1. 資料日期 = CreatedTime，格式 yyyy/MM/dd
2. 清關日期 = SIGN_OUT_TIME
3. 運送編號 = JETF_SERIAL
4. 單據編號0H4 = BL_NO
5. 派送日 = ScanCargoTime，若可解析日期則格式 yyyy/MM/dd
6. 收件人 = Importer
7. 稅金 = OtherFee
8. 代收 = Cod
9. 電話 = ImporterPhone
10. 地址 = ImporterAddr
11. 品名 = ItemName
12. 件數 = Qty
13. 材積 = Volume
14. 重量（kg） = Gw
15. 基本運費 = 55
16. 超才費 = 依超才費規則計算
17. 總額 = 依最低收費規則後之結果
18. 基本運費 = 55
19. 超重費 = 依超重費規則計算
20. 重量計費 = 基本運費 + 超重費
21. 應計價(擇大值) = max(總額, 重量計費)

### 稅金頁籤欄位

1. 運單號 = JETF_SERIAL
2. 稅金 = OtherFee
3. 日期 = SIGN_OUT_TIME

補充規則：

- 僅輸出 OtherFee > 0 的資料。

### 彙總頁籤欄位

1. 日期 = SIGN_OUT_TIME
2. 運費 = ChargeAmount

補充規則：

- 以日期分組加總 ChargeAmount。
- 最後一列需輸出合計。

## Backend Design

- Controller
  - 新增 SjlBillingController。
  - Index 提供頁面。
  - Download 接收查詢條件並回傳 Excel 檔案。
- Service
  - 新增 SjlBillingService。
  - 以 Dapper 一次查出大榮與捷通資料。
  - 將查詢結果轉為統一中介模型，先判定有效派件公司，再依 request.派件公司過濾。
  - 以 NPOI 建立主表、稅金、彙總三個工作表。
- Domain Model
  - Request Model：開始日期、結束日期、派件公司。
  - Query Row Model：對應 SQL 原始欄位。
  - Export Row Model：包含共用計價欄位與匯出欄位。

## Error Handling

- 查詢條件不完整或日期區間錯誤時，回傳錯誤訊息，不產生 Excel。
- 查無資料時，回傳「查無資料」訊息，不下載空白 Excel。
- 若 SjlShippingData 缺少補充欄位，仍可輸出資料，但缺值欄位維持空白；計價所需欄位若為空，視為 0。

## Non-goals

- 不新增資料編修功能。
- 不在畫面顯示查詢結果清單。
- 不處理大榮換單號回填邏輯。