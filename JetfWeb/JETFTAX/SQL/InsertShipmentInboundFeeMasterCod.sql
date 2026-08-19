/*
    將既有「開新單號重出」資料新增至 FEE_MASTER_COD。

    條件：
    1. ProcessType = 1（開新單號重出）
    2. FreightFee > 0
    3. Fee > 0
    4. Tax、Ccfee、Cod 皆為 0
    5. 已有出庫日期及出庫單號

    DLV_INV 使用 ShipmentInbound.OutboundTrackingNo。
    同一物流貨號若已存在 FEE_MASTER_COD，則不重複新增。
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

DECLARE @CreatedTime DATETIME = GETDATE();
DECLARE @InsertedCount INT;

;WITH ShipmentInboundSource AS
(
    SELECT
        s.*,
        ROW_NUMBER() OVER
        (
            PARTITION BY s.OutboundTrackingNo
            ORDER BY s.Id
        ) AS RowNo
    FROM dbo.ShipmentInbound AS s
    WHERE s.ProcessType = 1
      AND s.FreightFee > 0
      AND s.Fee > 0
      AND s.Tax = 0
      AND s.Ccfee = 0
      AND s.Cod = 0
      AND s.OutboundDate IS NOT NULL
      AND s.OutboundTrackingNo IS NOT NULL
      AND s.OutboundTrackingNo <> N''
)
INSERT INTO dbo.FEE_MASTER_COD
(
    DATA_TYPE,
    MAINNUMBER,
    CUSTOMER,
    BAG_NUMBER,
    TRACKINGNO,
    DLV_INV,
    CC,
    FreightFee,
    Fee,
    ToDlvCod,
    IsShipmentInbound,
    SIGN_OUT_TIME,
    CREATED_TIME
)
SELECT
    ISNULL(s.DataType, N''),
    ISNULL(s.MainNumber, N''),
    s.CustCode,
    N'',
    s.OriginalTrackingNo,
    s.OutboundTrackingNo,
    0,
    s.FreightFee,
    s.Fee,
    ISNULL(s.FreightFee, 0) + ISNULL(s.Fee, 0),
    1,
    s.OutboundDate,
    @CreatedTime
FROM ShipmentInboundSource AS s
WHERE s.RowNo = 1
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.FEE_MASTER_COD AS c
      WHERE c.DLV_INV = s.OutboundTrackingNo
  );

SET @InsertedCount = @@ROWCOUNT;

COMMIT TRANSACTION;

SELECT @InsertedCount AS InsertedCount;
