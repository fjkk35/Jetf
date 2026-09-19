SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

INSERT INTO [jetf].[dbo].[customer_special]
(
    [TRAN_TYPE],
    [CUST_NAME],
    [CUST_NAME2],
    [PHONE],
    [EMAIL],
    [REAMRK]
)
SELECT
    N'空運',
    source.[CUST_NAME],
    N'',
    source.[PHONE],
    N'',
    N''
FROM
(
    VALUES
        (N'27981667', N'康健生醫科技股份有限公司   鍾''s415'),
        (N'75914885', N'加高電子股份有限公司'),
        (N'86724715', N'宜軒科技有限公司'),
        (N'36204838', N'昱宣有限公司'),
        (N'22223510', N'正崴精密工业股份有限公司'),
        (N'27943474', N'英屬維京群島商曉龍電子股份有限公司台灣分公司')
) AS source ([PHONE], [CUST_NAME])
WHERE NOT EXISTS
(
    SELECT 1
    FROM [jetf].[dbo].[customer_special] AS target
    WHERE target.[TRAN_TYPE] = N'空運'
      AND target.[PHONE] = source.[PHONE]
);

COMMIT TRANSACTION;

SELECT
    [TRAN_TYPE],
    [CUST_NAME],
    [CUST_NAME2],
    [PHONE],
    [EMAIL],
    [REAMRK]
FROM [jetf].[dbo].[customer_special]
WHERE [TRAN_TYPE] = N'空運'
  AND [PHONE] IN
  (
      N'27981667',
      N'75914885',
      N'86724715',
      N'36204838',
      N'22223510',
      N'27943474'
  )
ORDER BY [PHONE];
