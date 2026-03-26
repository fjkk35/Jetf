## Overview

此功能延伸既有「捷利托運資料上傳」模組，新增一個查詢頁供營運人員檢視 SjlShippingData 內容，並於同頁直接修正派件公司。頁面使用既有 Angular.js + MVC 模式，查詢結果採分頁載入，避免一次載入過多資料。

## Menu And Permission

- 選單位置：JETFTAX/Scripts/_Layout.js 的「捷穩通」群組。
- 顯示方式：在「捷利托運資料上傳」下新增「捷利托運資料查詢」。
- URL：~/SjlBatchImport/Search
- 權限：沿用 Authority.SjlBatchImport。

## UI Design

頁面欄位：

- 日期起：非必填，查詢 jetf.dbo.SjlShippingData.CreatedTime 的起始日。
- 日期迄：非必填，查詢 jetf.dbo.SjlShippingData.CreatedTime 的結束日。
- 運送編號：非必填，模糊查詢 JetfSerial。
- 查詢按鈕。
- 清除按鈕。

結果欄位：

1. 運送編號
2. 單據編號
3. 編號
4. 收件人
5. 派送日
6. 其他費用
7. 代收
8. 地址
9. 品名
10. 件數
11. 材積
12. 重量
13. 收件人電話
14. 派件公司
15. 操作

操作規則：

- 每列提供「修改」按鈕。
- 點擊後開啟 bootstrap modal。
- modal 內提供派件公司下拉選單，選項只有「大榮」與「捷通」。
- 若使用者選到與目前相同的派件公司，前端直接提示並不送出修改。

## Data Query

### Query Parameters

- @StartDate = request.日期起，對應 CreatedTime >= @StartDate
- @EndDate = request.日期迄加 1 天，對應 CreatedTime < @EndDate
- @JetfSerial = request.運送編號，使用精準查詢
- @Page = request.Page
- @PageSize = request.PageSize

### SQL

```sql
select count(1)
from jetf.dbo.SjlShippingData
where (@StartDate is null or CreatedTime >= @StartDate)
  and (@EndDate is null or CreatedTime < @EndDate)
  and (@JetfSerial = '' or JetfSerial = @JetfSerial);

select
    Id,
    JetfSerial,
    BagNumber,
    Seq,
    Importer,
    DeliveryDate,
    OtherFee,
    Cod,
    ImporterAddr,
    ItemName,
    Qty,
    Volume,
    Gw,
    ImporterPhone,
    TransName,
    CreatedTime
from jetf.dbo.SjlShippingData
where (@StartDate is null or CreatedTime >= @StartDate)
  and (@EndDate is null or CreatedTime < @EndDate)
  and (@JetfSerial = '' or JetfSerial = @JetfSerial)
order by CreatedTime desc, Id desc
offset @Offset rows fetch next @PageSize rows only;
```

### Query Notes

- 日期起訖皆為 CreatedTime 條件。
- 日期迄需轉為隔日零點，才能包含整個日期迄當天。
- 運送編號使用 JetfSerial 精準查詢。
- 派件公司 TransName 可能為空，畫面可顯示空白。

## Update TransName Design

### Update Flow

1. 依 Id 取得 SjlShippingData 目前資料。
2. 若查無資料，回傳錯誤。
3. 若新派件公司與目前 TransName 相同，回傳不需修改訊息。
4. 以 transaction 執行：
   - 更新 jetf.dbo.SjlShippingData.TransName、UpdatedOpe、UpdatedTime
   - 寫入 jetf.dbo.SjlShippingDataTransNameHistory
5. 回傳成功訊息與最新派件公司。

### History Table

```sql
CREATE TABLE [jetf].[dbo].[SjlShippingDataTransNameHistory] (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SjlShippingDataId INT NOT NULL,
    OldTransName NVARCHAR(50) NULL,
    NewTransName NVARCHAR(50) NULL,
    CreatedOpe NVARCHAR(10) NULL,
    CreatedTime datetime2(0) NULL
);
```

## Backend Design

- Controller
  - SjlBatchImportController 新增 Search action。
  - 新增 SearchData API 回傳分頁資料。
  - 新增 UpdateTransName API 處理派件公司修改。
- Service
  - SjlBatchImportService 新增查詢與修改派件公司方法。
  - 修改派件公司需使用 transaction 同步更新主表與歷史表。
- Domain Model
  - SearchRequest: 日期起、日期迄、運送編號、Page、PageSize。
  - SearchResponse: TotalCount、Data。
  - RowModel: 對應畫面顯示欄位與 Id。
  - UpdateRequest: SjlShippingDataId、TransName。

## Error Handling

- 查詢失敗時回傳錯誤訊息。
- 修改派件公司若查無資料、派件公司不合法或與原值相同，皆回傳對應訊息。
- 若歷史表寫入失敗，整筆修改需 rollback。

## Non-goals

- 不在本次功能中提供歷史查詢畫面。
- 不提供批次修改派件公司。