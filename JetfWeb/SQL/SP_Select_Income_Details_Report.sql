USE [jetf];
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- 範例：
-- EXEC [dbo].[SP_Select_Income_Details_Report]
--     @ORIGINAL = N'SEA', @SDate = '2026-09-24T00:00:00', @EDate = '2026-09-24T23:59:59';
-- EXEC [dbo].[SP_Select_Income_Details_Report]
--     @ORIGINAL = N'ETL', @SDate = '2026-09-24T00:00:00', @EDate = '2026-09-24T23:59:59';
-- ETL2 保留既有的清關時間區間語意，供 LINE 報表使用。
ALTER PROCEDURE [dbo].[SP_Select_Income_Details_Report]
    @ORIGINAL nvarchar(8),
    @SDate datetime,
    @EDate datetime
AS
BEGIN
    SET NOCOUNT ON;

    IF @ORIGINAL = N'SEA'
    BEGIN
        -- 同主號、袋號的原單共用最新清關；先去重可避免逐張原單重查。
        SELECT DISTINCT c.MAIN_NUMBER, c.BAG_NUMBER
        INTO #SEA_CANDIDATE_KEYS
        FROM [DATA_CENTER].[dbo].[CLEARANCE_INFO] AS c
        WHERE c.SIGN_IN_TIME BETWEEN @SDate AND @EDate
          AND c.MAIN_NUMBER IS NOT NULL
          AND c.BAG_NUMBER IS NOT NULL
        OPTION (RECOMPILE);

        SELECT
            keys.MAIN_NUMBER,
            keys.BAG_NUMBER,
            c.I_DATA_TYPE,
            c.I_BAG_NUMBER,
            c.I_CARGO_PIECE,
            c.I_CLEARANCE_MODEL,
            c.I_SIGN_IN_TIME,
            c.I_SIGN_OUT_TIME
        INTO #SEA_LATEST_CLEARANCE
        FROM #SEA_CANDIDATE_KEYS AS keys
        CROSS APPLY
        (
            SELECT TOP (1)
                CONVERT(varchar(10), ci.DATA_TYPE) AS I_DATA_TYPE,
                CONVERT(varchar(20), ci.BAG_NUMBER) AS I_BAG_NUMBER,
                ci.CARGO_PIECE AS I_CARGO_PIECE,
                CONVERT(varchar(20), ci.CLEARANCE_MODEL) AS I_CLEARANCE_MODEL,
                ci.SIGN_IN_TIME AS I_SIGN_IN_TIME,
                ci.SIGN_OUT_TIME AS I_SIGN_OUT_TIME
            FROM [DATA_CENTER].[dbo].[CLEARANCE_INFO] AS ci
            WHERE ci.MAIN_NUMBER = keys.MAIN_NUMBER
              AND ci.BAG_NUMBER = keys.BAG_NUMBER
            ORDER BY ci.MODIFY_TIME DESC, ci.MODIFY_SEQ DESC, ci.ROW_ID DESC
        ) AS c
        WHERE c.I_SIGN_IN_TIME BETWEEN @SDate AND @EDate
        OPTION (RECOMPILE);

        SELECT
            s.MAINNUMBER,
            s.DESPATCH_NAME,
            s.TRANS_NAME,
            s.JETF_SERIAL,
            s.GW,
            s.PIECE,
            c.I_DATA_TYPE,
            c.I_BAG_NUMBER,
            c.I_CARGO_PIECE,
            c.I_CLEARANCE_MODEL,
            c.I_SIGN_IN_TIME,
            c.I_SIGN_OUT_TIME,
            f.FEE AS F_FEE,
            f.TAX1 AS F_TAX1,
            f.TAX2 AS F_TAX2,
            f.CCFEE AS F_CCFEE
        INTO #SEA_BASE
        FROM #SEA_LATEST_CLEARANCE AS c
        INNER JOIN [DATA_CENTER].[dbo].[SEA_ORDER_ORIGINAL] AS s
            ON s.MAINNUMBER = c.MAIN_NUMBER
           AND s.BL_NO = c.BAG_NUMBER
        OUTER APPLY
        (
            SELECT TOP (1) fm.FEE, fm.TAX1, fm.TAX2, fm.CCFEE
            FROM [jetf].[dbo].[FEE_MASTER] AS fm
            WHERE fm.DLV_INV = s.JETF_SERIAL
              AND
              (
                  fm.SOURCE_TYPE = N'2'
                  OR (s.GW > 0 AND fm.SOURCE_TYPE = N'1')
              )
            -- MERGE 轉檔先寫入 1 類費用，後寫入 2 類（G 類）；後者覆蓋前者。
            ORDER BY CASE WHEN fm.SOURCE_TYPE = N'2' THEN 0 ELSE 1 END,
                     fm.MODIFTYDATE DESC, fm.ID DESC
        ) AS f
        OPTION (RECOMPILE);

        -- GetIncomeDetailsTotalGwPiece('SEA', ...) 加總同主號所有原單的 GW/PIECE。
        SELECT
            keys.MAINNUMBER,
            CONVERT(nvarchar(50),
                CONVERT(nvarchar(20), SUM(all_orders.GW)) + N','
                    + CONVERT(nvarchar(20), SUM(all_orders.PIECE))) AS TOTAL_GW_PIECE_All
        INTO #SEA_MAIN_TOTAL
        FROM (SELECT DISTINCT MAINNUMBER FROM #SEA_BASE) AS keys
        OUTER APPLY
        (
            SELECT s.GW, s.PIECE
            FROM [DATA_CENTER].[dbo].[SEA_ORDER_ORIGINAL] AS s
            WHERE s.MAINNUMBER = keys.MAINNUMBER

            UNION ALL

            SELECT o.BAGWEIGHT AS GW, o.PIECES AS PIECE
            FROM [DATA_CENTER].[dbo].[ORIGINALLIST] AS o
            WHERE o.MAINNUMBER = keys.MAINNUMBER
        ) AS all_orders
        GROUP BY keys.MAINNUMBER;

        SELECT
            a.DATADATE,
            a.I_DATA_TYPE,
            a.DESPATCH_NO,
            a.DESPATCH_NAME,
            a.MAINNUMBER,
            CEILING(SUM(a.Total_TARIFF)) AS Total_TARIFF,
            CEILING(SUM(a.Total_CC)) AS Total_CC,
            SUM(a.F_FEE) AS TOTAL_FEE,
            SUM(a.TOTAL_TAX_N) AS TOTAL_TAX_N,
            SUM(a.TOTAL_TAX_C) AS TOTAL_TAX_C,
            SUM(a.TOTAL_TAX_Y) AS TOTAL_TAX_Y,
            SUM(a.F_CCFEE) AS TOTAL_CCFEE,
            SUM(a.PIECE) AS TOTAL_PIECE,
            SUM(a.OUT_PIECE) AS TOTAL_OUT_PIECE,
            SUM(a.GW) AS TOTAL_GW,
            COUNT(DISTINCT a.I_BAG_NUMBER) AS TOTAL_BAG_NUMBER,
            COUNT(DISTINCT a.JETF_SERIAL) AS TOTAL_COUNT,
            SUM(a.TOTAL_PIECE_C3) AS TOTAL_PIECE_C3,
            (
                SELECT t.TOTAL_GW_PIECE_All
                FROM #SEA_MAIN_TOTAL AS t
                WHERE t.MAINNUMBER = a.MAINNUMBER
            ) AS TOTAL_GW_PIECE_All
        FROM
        (
            SELECT
                CONVERT(nvarchar(20), a.I_SIGN_IN_TIME, 112) AS DATADATE,
                a.I_DATA_TYPE,
                a.DESPATCH_NAME AS DESPATCH_NO,
                [jetf].[dbo].[GetCUSTOMER](N'海運', a.DESPATCH_NAME) AS DESPATCH_NAME,
                b.TRANS_NO,
                a.TRANS_NAME,
                b.INCLUDE_TAX,
                a.I_BAG_NUMBER,
                a.JETF_SERIAL,
                a.MAINNUMBER,
                a.GW * c.TARIFF AS Total_TARIFF,
                a.GW * c.CC AS Total_CC,
                a.GW,
                a.F_FEE,
                a.F_TAX1,
                a.F_TAX2,
                a.F_CCFEE,
                a.PIECE,
                CASE WHEN b.INCLUDE_TAX IN (N'D', N'N') OR b.INCLUDE_TAX IS NULL
                     THEN a.F_TAX1 + a.F_TAX2 ELSE 0 END AS TOTAL_TAX_N,
                CASE WHEN b.INCLUDE_TAX = N'C'
                     THEN a.F_TAX1 + a.F_TAX2 ELSE 0 END AS TOTAL_TAX_C,
                CASE WHEN b.INCLUDE_TAX = N'Y'
                     THEN a.F_TAX1 + a.F_TAX2 ELSE 0 END AS TOTAL_TAX_Y,
                CASE WHEN a.I_CLEARANCE_MODEL = 'C3' AND a.GW > 0
                     THEN a.I_CARGO_PIECE ELSE 0 END AS TOTAL_PIECE_C3,
                CASE WHEN a.I_SIGN_OUT_TIME > '' THEN a.PIECE ELSE 0 END AS OUT_PIECE,
                CASE WHEN a.I_SIGN_OUT_TIME > '' THEN a.I_BAG_NUMBER ELSE NULL END AS OUT_BAG_NUMBER
            FROM #SEA_BASE AS a
            LEFT JOIN [jetf].[dbo].[customer_master] AS b
                ON b.CUST_ID = a.DESPATCH_NAME
               AND b.TRANS_NAME = a.TRANS_NAME
               AND b.TRAN_TYPE = N'海運'
            LEFT JOIN [jetf].[dbo].[customer_price] AS c
                ON c.CUST_ID = a.DESPATCH_NAME
               AND c.TRAN_TYPE = N'進口海快'
               AND c.INCLUDE_TAX = b.INCLUDE_TAX
        ) AS a
        GROUP BY
            a.DATADATE, a.I_DATA_TYPE, a.DESPATCH_NO, a.DESPATCH_NAME,
            a.MAINNUMBER;

        DROP TABLE #SEA_MAIN_TOTAL;
        DROP TABLE #SEA_BASE;
        DROP TABLE #SEA_LATEST_CLEARANCE;
        DROP TABLE #SEA_CANDIDATE_KEYS;
        RETURN;
    END;

    IF @ORIGINAL = N'ETL'
    BEGIN
        -- ETL 將日期轉為隔日 09:00 前的清關時間；ETL2 直接使用傳入時間。
        DECLARE @FilterStart datetime = DATEADD(HOUR, 9, @SDate);
        DECLARE @FilterEnd datetime = DATEADD(HOUR, 9, @EDate);

        -- TOTAL_COUNT 原本會重新計算每個 DATADATE 的完整 09:00 至隔日 09:00。
        DECLARE @CountStart datetime = DATEADD
        (
            HOUR, 9, CONVERT(datetime, CONVERT(date, DATEADD(HOUR, -9, @FilterStart)))
        );
        DECLARE @CountEnd datetime = DATEADD
        (
            DAY, 1, DATEADD(HOUR, 9,
                CONVERT(datetime, CONVERT(date, DATEADD(HOUR, -9, @FilterEnd))))
        );

        -- 先以清關日期索引找出候選訂單；三種配對各自使用原單索引。
        SELECT o.ID AS ORDER_ID
        INTO #AIR_CANDIDATES
        FROM [DATA_CENTER].[dbo].[CLEARANCE_INFO] AS c
        INNER JOIN [DATA_CENTER].[dbo].[ORIGINALLIST] AS o
            ON o.MAINNUMBER = c.MAIN_NUMBER
           AND o.TRACKINGUB = c.MERGE_NUMBER
        WHERE c.SIGN_IN_TIME BETWEEN @CountStart AND @CountEnd
          AND c.MERGE_NUMBER > ''

        UNION

        SELECT o.ID
        FROM [DATA_CENTER].[dbo].[CLEARANCE_INFO] AS c
        INNER JOIN [DATA_CENTER].[dbo].[ORIGINALLIST] AS o
            ON o.MAINNUMBER = c.MAIN_NUMBER
           AND o.BAGNO = c.BAG_NUMBER
        WHERE c.SIGN_IN_TIME BETWEEN @CountStart AND @CountEnd
          AND c.MERGE_NUMBER = ''

        UNION

        SELECT o.ID
        FROM [DATA_CENTER].[dbo].[CLEARANCE_INFO] AS c
        INNER JOIN [DATA_CENTER].[dbo].[ORIGINALLIST] AS o
            ON o.MAINNUMBER = c.MAIN_NUMBER
           AND o.BAGNO = c.MERGE_NUMBER
        WHERE c.SIGN_IN_TIME BETWEEN @CountStart AND @CountEnd
          AND c.DATA_TYPE = N'FTZ'
        OPTION (RECOMPILE);

        ;WITH AIR_LATEST AS
        (
            SELECT
                o.ID AS ORDER_ID,
                o.MAINNUMBER,
                o.BAGNO,
                o.DESPATCHNO AS DESPATCH_NO,
                o.CLEARANCEWAREHOUSING,
                o.TRANS_TAXPAYMENT,
                o.TRACKINGUB AS JETF_SERIAL,
                o.DELIVERYNO,
                c.I_DATA_TYPE,
                c.I_BAG_NUMBER,
                c.I_MERGE_NUMBER,
                c.I_CARGO_PIECE,
                c.CARGO_WEIGHT,
                c.I_SIGN_IN_TIME,
                c.I_SIGN_OUT_TIME,
                c.MATCH_PRIORITY
            FROM #AIR_CANDIDATES AS candidate
            INNER JOIN [DATA_CENTER].[dbo].[ORIGINALLIST] AS o
                ON o.ID = candidate.ORDER_ID
            CROSS APPLY
            (
                SELECT TOP (1)
                    CONVERT(varchar(10), ci.DATA_TYPE) AS I_DATA_TYPE,
                    CONVERT(varchar(20), ci.BAG_NUMBER) AS I_BAG_NUMBER,
                    CONVERT(varchar(30), ci.MERGE_NUMBER) AS I_MERGE_NUMBER,
                    ci.CARGO_PIECE AS I_CARGO_PIECE,
                    ci.CARGO_WEIGHT,
                    ci.SIGN_IN_TIME AS I_SIGN_IN_TIME,
                    ci.SIGN_OUT_TIME AS I_SIGN_OUT_TIME,
                    CASE
                        WHEN ci.MERGE_NUMBER > '' AND o.TRACKINGUB = ci.MERGE_NUMBER THEN 1
                        WHEN ci.MERGE_NUMBER = '' AND o.BAGNO = ci.BAG_NUMBER THEN 2
                        ELSE 3
                    END AS MATCH_PRIORITY
                FROM [DATA_CENTER].[dbo].[CLEARANCE_INFO] AS ci
                WHERE ci.MAIN_NUMBER = o.MAINNUMBER
                  AND
                  (
                      (ci.MERGE_NUMBER > '' AND o.TRACKINGUB = ci.MERGE_NUMBER)
                      OR (ci.MERGE_NUMBER = '' AND o.BAGNO = ci.BAG_NUMBER)
                      OR (ci.DATA_TYPE = N'FTZ' AND o.BAGNO = ci.MERGE_NUMBER)
                  )
                ORDER BY ci.MODIFY_TIME DESC, ci.MODIFY_SEQ DESC,
                    CASE
                        WHEN ci.MERGE_NUMBER > '' AND o.TRACKINGUB = ci.MERGE_NUMBER THEN 1
                        WHEN ci.MERGE_NUMBER = '' AND o.BAGNO = ci.BAG_NUMBER THEN 2
                        ELSE 3
                    END,
                    ci.ROW_ID DESC
            ) AS c
        )
        SELECT
            m.ORDER_ID,
            m.MAINNUMBER,
            m.BAGNO,
            m.DESPATCH_NO AS DESPATCH_NAME,
            [jetf].[dbo].[GetTRANS_NAME](m.CLEARANCEWAREHOUSING) AS TRANS_NAME,
            m.TRANS_TAXPAYMENT,
            m.JETF_SERIAL,
            m.DELIVERYNO,
            m.I_DATA_TYPE,
            m.I_BAG_NUMBER,
            m.I_MERGE_NUMBER,
            m.I_CARGO_PIECE,
            CASE
                WHEN m.MATCH_PRIORITY = 1
                  OR NOT EXISTS
                  (
                      SELECT 1
                      FROM [DATA_CENTER].[dbo].[ORIGINALLIST] AS prior_order
                      WHERE prior_order.MAINNUMBER = m.MAINNUMBER
                        AND prior_order.BAGNO = m.BAGNO
                        AND prior_order.ID < m.ORDER_ID
                  ) THEN CONVERT(float, m.CARGO_WEIGHT)
                ELSE CONVERT(float, 0)
            END AS I_CARGO_WEIGHT,
            m.I_SIGN_IN_TIME,
            m.I_SIGN_OUT_TIME
        INTO #AIR_SCOPE
        FROM AIR_LATEST AS m
        WHERE m.I_SIGN_IN_TIME BETWEEN @CountStart AND @CountEnd
        OPTION (RECOMPILE);

        SELECT
            a.*,
            f.FEE AS F_FEE,
            f.TAX1 AS F_TAX1,
            f.TAX2 AS F_TAX2,
            f.CCFEE AS F_CCFEE
        INTO #AIR_BASE
        FROM #AIR_SCOPE AS a
        OUTER APPLY
        (
            SELECT TOP (1) fm.FEE, fm.TAX1, fm.TAX2, fm.CCFEE
            FROM [jetf].[dbo].[FEE_MASTER] AS fm
            WHERE fm.DLV_INV = a.DELIVERYNO
              AND fm.SOURCE_TYPE = N'3'
            ORDER BY fm.MODIFTYDATE DESC, fm.ID DESC
        ) AS f
        WHERE a.I_SIGN_IN_TIME BETWEEN @FilterStart AND @FilterEnd;

        -- 舊函式以整個主號為範圍，且沒有來源與日期條件；從兩種原始訂單重建。
        SELECT
            keys.MAINNUMBER,
            CONVERT(nvarchar(50),
                CONVERT(nvarchar(20), SUM(all_clearance.I_CARGO_WEIGHT)) + N','
                    + CONVERT(nvarchar(20), SUM(all_clearance.I_CARGO_PIECE))) AS TOTAL_GW_PIECE_All
        INTO #AIR_MAIN_TOTAL
        FROM (SELECT DISTINCT MAINNUMBER FROM #AIR_BASE) AS keys
        OUTER APPLY
        (
            SELECT
                CASE
                    WHEN c.ROW_ID IS NULL THEN CONVERT(float, NULL)
                    WHEN c.MERGE_NUMBER > '' AND o.TRACKINGUB = c.MERGE_NUMBER
                        THEN CONVERT(float, c.CARGO_WEIGHT)
                    WHEN NOT EXISTS
                    (
                        SELECT 1
                        FROM [DATA_CENTER].[dbo].[ORIGINALLIST] AS prior_order
                        WHERE prior_order.MAINNUMBER = o.MAINNUMBER
                          AND prior_order.BAGNO = o.BAGNO
                          AND prior_order.ID < o.ID
                    ) THEN CONVERT(float, c.CARGO_WEIGHT)
                    ELSE CONVERT(float, 0)
                END AS I_CARGO_WEIGHT,
                c.CARGO_PIECE AS I_CARGO_PIECE
            FROM [DATA_CENTER].[dbo].[ORIGINALLIST] AS o
            OUTER APPLY
            (
                SELECT TOP (1) ci.ROW_ID, ci.MERGE_NUMBER,
                               ci.CARGO_WEIGHT, ci.CARGO_PIECE
                FROM [DATA_CENTER].[dbo].[CLEARANCE_INFO] AS ci
                WHERE ci.MAIN_NUMBER = o.MAINNUMBER
                  AND
                  (
                      (ci.MERGE_NUMBER > '' AND ci.MERGE_NUMBER = o.TRACKINGUB)
                      OR (ci.MERGE_NUMBER = '' AND ci.BAG_NUMBER = o.BAGNO)
                      OR (ci.DATA_TYPE = N'FTZ' AND ci.MERGE_NUMBER = o.BAGNO)
                  )
                ORDER BY ci.MODIFY_TIME DESC, ci.MODIFY_SEQ DESC, ci.ROW_ID DESC
            ) AS c
            WHERE o.MAINNUMBER = keys.MAINNUMBER

            UNION ALL

            SELECT
                CONVERT(float, c.CARGO_WEIGHT) AS I_CARGO_WEIGHT,
                c.CARGO_PIECE AS I_CARGO_PIECE
            FROM [DATA_CENTER].[dbo].[SEA_ORDER_ORIGINAL] AS s
            OUTER APPLY
            (
                SELECT TOP (1) ci.CARGO_WEIGHT, ci.CARGO_PIECE
                FROM [DATA_CENTER].[dbo].[CLEARANCE_INFO] AS ci
                WHERE ci.MAIN_NUMBER = s.MAINNUMBER
                  AND ci.BAG_NUMBER = s.BL_NO
                ORDER BY ci.MODIFY_TIME DESC, ci.MODIFY_SEQ DESC, ci.ROW_ID DESC
            ) AS c
            WHERE s.MAINNUMBER = keys.MAINNUMBER
        ) AS all_clearance
        GROUP BY keys.MAINNUMBER
        OPTION (RECOMPILE);

        SELECT
            a.DATADATE,
            a.I_DATA_TYPE,
            a.DESPATCH_NO,
            a.DESPATCH_NAME,
            a.MAINNUMBER,
            CONVERT(int, CEILING(SUM(a.Total_TARIFF))) AS Total_TARIFF,
            CONVERT(int, CEILING(SUM(a.Total_CC))) AS Total_CC,
            SUM(a.F_FEE) AS TOTAL_FEE,
            SUM(a.TOTAL_TAX_N) AS TOTAL_TAX_N,
            SUM(a.TOTAL_TAX_C) AS TOTAL_TAX_C,
            SUM(a.TOTAL_TAX_Y) AS TOTAL_TAX_Y,
            SUM(a.F_CCFEE) AS TOTAL_CCFEE,
            SUM(a.PIECE) AS TOTAL_PIECE,
            SUM(a.OUT_PIECE) AS TOTAL_OUT_PIECE,
            CAST(SUM(a.GW) AS numeric(8, 2)) AS TOTAL_GW,
            COUNT(DISTINCT a.I_BAG_NUMBER) AS TOTAL_BAG_NUMBER,
            COUNT(DISTINCT a.OUT_BAG_NUMBER) AS TOTAL_OUT_BAG_NUMBER,
            (
                SELECT COUNT(1)
                FROM
                (
                    SELECT DISTINCT scope.I_BAG_NUMBER, scope.I_MERGE_NUMBER
                    FROM #AIR_SCOPE AS scope
                    WHERE scope.MAINNUMBER = a.MAINNUMBER
                      AND scope.I_DATA_TYPE = a.I_DATA_TYPE
                      AND scope.DESPATCH_NAME = a.DESPATCH_NO
                      AND scope.I_SIGN_IN_TIME BETWEEN
                          DATEADD(HOUR, 9, CONVERT(datetime, a.DATADATE, 112))
                          AND DATEADD(DAY, 1,
                              DATEADD(HOUR, 9, CONVERT(datetime, a.DATADATE, 112)))
                ) AS distinct_bags
            ) AS TOTAL_COUNT,
            SUM(a.TOTAL_PIECE_C3) AS TOTAL_PIECE_C3,
            (
                SELECT t.TOTAL_GW_PIECE_All
                FROM #AIR_MAIN_TOTAL AS t
                WHERE t.MAINNUMBER = a.MAINNUMBER
            ) AS TOTAL_GW_PIECE_All
        FROM
        (
            SELECT
                CONVERT(nvarchar(20), DATEADD(HH, -9, a.I_SIGN_IN_TIME), 112) AS DATADATE,
                a.I_DATA_TYPE,
                a.DESPATCH_NAME AS DESPATCH_NO,
                [jetf].[dbo].[GetCUSTOMER](N'空運', a.DESPATCH_NAME) AS DESPATCH_NAME,
                b.TRANS_NO,
                a.TRANS_NAME,
                b.INCLUDE_TAX,
                a.I_BAG_NUMBER,
                a.I_MERGE_NUMBER,
                a.MAINNUMBER,
                a.I_CARGO_WEIGHT * c.TARIFF AS Total_TARIFF,
                a.I_CARGO_WEIGHT * c.CC AS Total_CC,
                a.I_CARGO_WEIGHT AS GW,
                a.F_FEE,
                a.F_TAX1,
                a.F_TAX2,
                a.F_CCFEE,
                a.I_CARGO_PIECE AS PIECE,
                CASE WHEN b.INCLUDE_TAX IN (N'D', N'N') OR b.INCLUDE_TAX IS NULL
                     THEN a.F_TAX1 + a.F_TAX2 ELSE 0 END AS TOTAL_TAX_N,
                CASE WHEN b.INCLUDE_TAX = N'C'
                     THEN a.F_TAX1 + a.F_TAX2 ELSE 0 END AS TOTAL_TAX_C,
                CASE WHEN b.INCLUDE_TAX = N'Y'
                     THEN a.F_TAX1 + a.F_TAX2 ELSE 0 END AS TOTAL_TAX_Y,
                CONVERT(int, 0) AS TOTAL_PIECE_C3,
                CASE WHEN a.I_SIGN_OUT_TIME > '' THEN a.I_CARGO_PIECE ELSE 0 END AS OUT_PIECE,
                CASE WHEN a.I_SIGN_OUT_TIME > '' THEN a.I_BAG_NUMBER ELSE NULL END AS OUT_BAG_NUMBER
            FROM #AIR_BASE AS a
            LEFT JOIN [jetf].[dbo].[customer_master] AS b
                ON b.CUST_ID = a.DESPATCH_NAME
               AND b.TRANS_NO = a.TRANS_TAXPAYMENT
               AND b.TRAN_TYPE = N'空運'
            LEFT JOIN [jetf].[dbo].[customer_price] AS c
                ON c.CUST_ID = a.DESPATCH_NAME
               AND c.TRAN_TYPE = N'進口空快'
               AND c.INCLUDE_TAX = b.INCLUDE_TAX
        ) AS a
        GROUP BY
            a.DATADATE, a.I_DATA_TYPE, a.DESPATCH_NO, a.DESPATCH_NAME,
            a.MAINNUMBER;

        DROP TABLE #AIR_MAIN_TOTAL;
        DROP TABLE #AIR_BASE;
        DROP TABLE #AIR_SCOPE;
        DROP TABLE #AIR_CANDIDATES;
    END;

    IF @ORIGINAL = N'ETL2'
    BEGIN
        -- ETL 將日期轉為隔日 09:00 前的清關時間；ETL2 直接使用傳入時間。
        DECLARE @FilterStartETL2 datetime = @SDate;
        DECLARE @FilterEndETL2 datetime = @EDate;

        -- TOTAL_COUNT 原本會重新計算每個 DATADATE 的完整 09:00 至隔日 09:00。
        DECLARE @CountStartETL2 datetime = DATEADD
        (
            HOUR, 9, CONVERT(datetime, CONVERT(date, DATEADD(HOUR, -9, @FilterStartETL2)))
        );
        DECLARE @CountEndETL2 datetime = DATEADD
        (
            DAY, 1, DATEADD(HOUR, 9,
                CONVERT(datetime, CONVERT(date, DATEADD(HOUR, -9, @FilterEndETL2))))
        );

        -- 先以清關日期索引找出候選訂單；三種配對各自使用原單索引。
        SELECT o.ID AS ORDER_ID
        INTO #AIR_CANDIDATES_ETL2
        FROM [DATA_CENTER].[dbo].[CLEARANCE_INFO] AS c
        INNER JOIN [DATA_CENTER].[dbo].[ORIGINALLIST] AS o
            ON o.MAINNUMBER = c.MAIN_NUMBER
           AND o.TRACKINGUB = c.MERGE_NUMBER
        WHERE c.SIGN_IN_TIME BETWEEN @CountStartETL2 AND @CountEndETL2
          AND c.MERGE_NUMBER > ''

        UNION

        SELECT o.ID
        FROM [DATA_CENTER].[dbo].[CLEARANCE_INFO] AS c
        INNER JOIN [DATA_CENTER].[dbo].[ORIGINALLIST] AS o
            ON o.MAINNUMBER = c.MAIN_NUMBER
           AND o.BAGNO = c.BAG_NUMBER
        WHERE c.SIGN_IN_TIME BETWEEN @CountStartETL2 AND @CountEndETL2
          AND c.MERGE_NUMBER = ''

        UNION

        SELECT o.ID
        FROM [DATA_CENTER].[dbo].[CLEARANCE_INFO] AS c
        INNER JOIN [DATA_CENTER].[dbo].[ORIGINALLIST] AS o
            ON o.MAINNUMBER = c.MAIN_NUMBER
           AND o.BAGNO = c.MERGE_NUMBER
        WHERE c.SIGN_IN_TIME BETWEEN @CountStartETL2 AND @CountEndETL2
          AND c.DATA_TYPE = N'FTZ'
        OPTION (RECOMPILE);

        ;WITH AIR_LATEST AS
        (
            SELECT
                o.ID AS ORDER_ID,
                o.MAINNUMBER,
                o.BAGNO,
                o.DESPATCHNO AS DESPATCH_NO,
                o.CLEARANCEWAREHOUSING,
                o.TRANS_TAXPAYMENT,
                o.TRACKINGUB AS JETF_SERIAL,
                o.DELIVERYNO,
                c.I_DATA_TYPE,
                c.I_BAG_NUMBER,
                c.I_MERGE_NUMBER,
                c.I_CARGO_PIECE,
                c.CARGO_WEIGHT,
                c.I_SIGN_IN_TIME,
                c.I_SIGN_OUT_TIME,
                c.MATCH_PRIORITY
            FROM #AIR_CANDIDATES_ETL2 AS candidate
            INNER JOIN [DATA_CENTER].[dbo].[ORIGINALLIST] AS o
                ON o.ID = candidate.ORDER_ID
            CROSS APPLY
            (
                SELECT TOP (1)
                    CONVERT(varchar(10), ci.DATA_TYPE) AS I_DATA_TYPE,
                    CONVERT(varchar(20), ci.BAG_NUMBER) AS I_BAG_NUMBER,
                    CONVERT(varchar(30), ci.MERGE_NUMBER) AS I_MERGE_NUMBER,
                    ci.CARGO_PIECE AS I_CARGO_PIECE,
                    ci.CARGO_WEIGHT,
                    ci.SIGN_IN_TIME AS I_SIGN_IN_TIME,
                    ci.SIGN_OUT_TIME AS I_SIGN_OUT_TIME,
                    CASE
                        WHEN ci.MERGE_NUMBER > '' AND o.TRACKINGUB = ci.MERGE_NUMBER THEN 1
                        WHEN ci.MERGE_NUMBER = '' AND o.BAGNO = ci.BAG_NUMBER THEN 2
                        ELSE 3
                    END AS MATCH_PRIORITY
                FROM [DATA_CENTER].[dbo].[CLEARANCE_INFO] AS ci
                WHERE ci.MAIN_NUMBER = o.MAINNUMBER
                  AND
                  (
                      (ci.MERGE_NUMBER > '' AND o.TRACKINGUB = ci.MERGE_NUMBER)
                      OR (ci.MERGE_NUMBER = '' AND o.BAGNO = ci.BAG_NUMBER)
                      OR (ci.DATA_TYPE = N'FTZ' AND o.BAGNO = ci.MERGE_NUMBER)
                  )
                ORDER BY ci.MODIFY_TIME DESC, ci.MODIFY_SEQ DESC,
                    CASE
                        WHEN ci.MERGE_NUMBER > '' AND o.TRACKINGUB = ci.MERGE_NUMBER THEN 1
                        WHEN ci.MERGE_NUMBER = '' AND o.BAGNO = ci.BAG_NUMBER THEN 2
                        ELSE 3
                    END,
                    ci.ROW_ID DESC
            ) AS c
        )
        SELECT
            m.ORDER_ID,
            m.MAINNUMBER,
            m.BAGNO,
            m.DESPATCH_NO AS DESPATCH_NAME,
            [jetf].[dbo].[GetTRANS_NAME](m.CLEARANCEWAREHOUSING) AS TRANS_NAME,
            m.TRANS_TAXPAYMENT,
            m.JETF_SERIAL,
            m.DELIVERYNO,
            m.I_DATA_TYPE,
            m.I_BAG_NUMBER,
            m.I_MERGE_NUMBER,
            m.I_CARGO_PIECE,
            CASE
                WHEN m.MATCH_PRIORITY = 1
                  OR NOT EXISTS
                  (
                      SELECT 1
                      FROM [DATA_CENTER].[dbo].[ORIGINALLIST] AS prior_order
                      WHERE prior_order.MAINNUMBER = m.MAINNUMBER
                        AND prior_order.BAGNO = m.BAGNO
                        AND prior_order.ID < m.ORDER_ID
                  ) THEN CONVERT(float, m.CARGO_WEIGHT)
                ELSE CONVERT(float, 0)
            END AS I_CARGO_WEIGHT,
            m.I_SIGN_IN_TIME,
            m.I_SIGN_OUT_TIME
        INTO #AIR_SCOPE_ETL2
        FROM AIR_LATEST AS m
        WHERE m.I_SIGN_IN_TIME BETWEEN @CountStartETL2 AND @CountEndETL2
        OPTION (RECOMPILE);

        SELECT
            a.*,
            f.FEE AS F_FEE,
            f.TAX1 AS F_TAX1,
            f.TAX2 AS F_TAX2,
            f.CCFEE AS F_CCFEE
        INTO #AIR_BASE_ETL2
        FROM #AIR_SCOPE_ETL2 AS a
        OUTER APPLY
        (
            SELECT TOP (1) fm.FEE, fm.TAX1, fm.TAX2, fm.CCFEE
            FROM [jetf].[dbo].[FEE_MASTER] AS fm
            WHERE fm.DLV_INV = a.DELIVERYNO
              AND fm.SOURCE_TYPE = N'3'
            ORDER BY fm.MODIFTYDATE DESC, fm.ID DESC
        ) AS f
        WHERE a.I_SIGN_IN_TIME BETWEEN @FilterStartETL2 AND @FilterEndETL2;

        -- 舊函式以整個主號為範圍，且沒有來源與日期條件；從兩種原始訂單重建。
        SELECT
            keys.MAINNUMBER,
            CONVERT(nvarchar(50),
                CONVERT(nvarchar(20), SUM(all_clearance.I_CARGO_WEIGHT)) + N','
                    + CONVERT(nvarchar(20), SUM(all_clearance.I_CARGO_PIECE))) AS TOTAL_GW_PIECE_All
        INTO #AIR_MAIN_TOTAL_ETL2
        FROM (SELECT DISTINCT MAINNUMBER FROM #AIR_BASE_ETL2) AS keys
        OUTER APPLY
        (
            SELECT
                CASE
                    WHEN c.ROW_ID IS NULL THEN CONVERT(float, NULL)
                    WHEN c.MERGE_NUMBER > '' AND o.TRACKINGUB = c.MERGE_NUMBER
                        THEN CONVERT(float, c.CARGO_WEIGHT)
                    WHEN NOT EXISTS
                    (
                        SELECT 1
                        FROM [DATA_CENTER].[dbo].[ORIGINALLIST] AS prior_order
                        WHERE prior_order.MAINNUMBER = o.MAINNUMBER
                          AND prior_order.BAGNO = o.BAGNO
                          AND prior_order.ID < o.ID
                    ) THEN CONVERT(float, c.CARGO_WEIGHT)
                    ELSE CONVERT(float, 0)
                END AS I_CARGO_WEIGHT,
                c.CARGO_PIECE AS I_CARGO_PIECE
            FROM [DATA_CENTER].[dbo].[ORIGINALLIST] AS o
            OUTER APPLY
            (
                SELECT TOP (1) ci.ROW_ID, ci.MERGE_NUMBER,
                               ci.CARGO_WEIGHT, ci.CARGO_PIECE
                FROM [DATA_CENTER].[dbo].[CLEARANCE_INFO] AS ci
                WHERE ci.MAIN_NUMBER = o.MAINNUMBER
                  AND
                  (
                      (ci.MERGE_NUMBER > '' AND ci.MERGE_NUMBER = o.TRACKINGUB)
                      OR (ci.MERGE_NUMBER = '' AND ci.BAG_NUMBER = o.BAGNO)
                      OR (ci.DATA_TYPE = N'FTZ' AND ci.MERGE_NUMBER = o.BAGNO)
                  )
                ORDER BY ci.MODIFY_TIME DESC, ci.MODIFY_SEQ DESC, ci.ROW_ID DESC
            ) AS c
            WHERE o.MAINNUMBER = keys.MAINNUMBER

            UNION ALL

            SELECT
                CONVERT(float, c.CARGO_WEIGHT) AS I_CARGO_WEIGHT,
                c.CARGO_PIECE AS I_CARGO_PIECE
            FROM [DATA_CENTER].[dbo].[SEA_ORDER_ORIGINAL] AS s
            OUTER APPLY
            (
                SELECT TOP (1) ci.CARGO_WEIGHT, ci.CARGO_PIECE
                FROM [DATA_CENTER].[dbo].[CLEARANCE_INFO] AS ci
                WHERE ci.MAIN_NUMBER = s.MAINNUMBER
                  AND ci.BAG_NUMBER = s.BL_NO
                ORDER BY ci.MODIFY_TIME DESC, ci.MODIFY_SEQ DESC, ci.ROW_ID DESC
            ) AS c
            WHERE s.MAINNUMBER = keys.MAINNUMBER
        ) AS all_clearance
        GROUP BY keys.MAINNUMBER
        OPTION (RECOMPILE);

        SELECT
            a.DATADATE,
            a.I_DATA_TYPE,
            a.DESPATCH_NO,
            a.DESPATCH_NAME,
            a.MAINNUMBER,
            CONVERT(int, CEILING(SUM(a.Total_TARIFF))) AS Total_TARIFF,
            CONVERT(int, CEILING(SUM(a.Total_CC))) AS Total_CC,
            SUM(a.F_FEE) AS TOTAL_FEE,
            SUM(a.TOTAL_TAX_N) AS TOTAL_TAX_N,
            SUM(a.TOTAL_TAX_C) AS TOTAL_TAX_C,
            SUM(a.TOTAL_TAX_Y) AS TOTAL_TAX_Y,
            SUM(a.F_CCFEE) AS TOTAL_CCFEE,
            SUM(a.PIECE) AS TOTAL_PIECE,
            SUM(a.OUT_PIECE) AS TOTAL_OUT_PIECE,
            CAST(SUM(a.GW) AS numeric(8, 2)) AS TOTAL_GW,
            COUNT(DISTINCT a.I_BAG_NUMBER) AS TOTAL_BAG_NUMBER,
            COUNT(DISTINCT a.OUT_BAG_NUMBER) AS TOTAL_OUT_BAG_NUMBER,
            (
                SELECT COUNT(1)
                FROM
                (
                    SELECT DISTINCT scope.I_BAG_NUMBER, scope.I_MERGE_NUMBER
                    FROM #AIR_SCOPE_ETL2 AS scope
                    WHERE scope.MAINNUMBER = a.MAINNUMBER
                      AND scope.I_DATA_TYPE = a.I_DATA_TYPE
                      AND scope.DESPATCH_NAME = a.DESPATCH_NO
                      AND scope.I_SIGN_IN_TIME BETWEEN
                          DATEADD(HOUR, 9, CONVERT(datetime, a.DATADATE, 112))
                          AND DATEADD(DAY, 1,
                              DATEADD(HOUR, 9, CONVERT(datetime, a.DATADATE, 112)))
                ) AS distinct_bags
            ) AS TOTAL_COUNT,
            SUM(a.TOTAL_PIECE_C3) AS TOTAL_PIECE_C3,
            (
                SELECT t.TOTAL_GW_PIECE_All
                FROM #AIR_MAIN_TOTAL_ETL2 AS t
                WHERE t.MAINNUMBER = a.MAINNUMBER
            ) AS TOTAL_GW_PIECE_All
        FROM
        (
            SELECT
                CONVERT(nvarchar(20), DATEADD(HH, -9, a.I_SIGN_IN_TIME), 112) AS DATADATE,
                a.I_DATA_TYPE,
                a.DESPATCH_NAME AS DESPATCH_NO,
                [jetf].[dbo].[GetCUSTOMER](N'空運', a.DESPATCH_NAME) AS DESPATCH_NAME,
                b.TRANS_NO,
                a.TRANS_NAME,
                b.INCLUDE_TAX,
                a.I_BAG_NUMBER,
                a.I_MERGE_NUMBER,
                a.MAINNUMBER,
                a.I_CARGO_WEIGHT * c.TARIFF AS Total_TARIFF,
                a.I_CARGO_WEIGHT * c.CC AS Total_CC,
                a.I_CARGO_WEIGHT AS GW,
                a.F_FEE,
                a.F_TAX1,
                a.F_TAX2,
                a.F_CCFEE,
                a.I_CARGO_PIECE AS PIECE,
                CASE WHEN b.INCLUDE_TAX IN (N'D', N'N') OR b.INCLUDE_TAX IS NULL
                     THEN a.F_TAX1 + a.F_TAX2 ELSE 0 END AS TOTAL_TAX_N,
                CASE WHEN b.INCLUDE_TAX = N'C'
                     THEN a.F_TAX1 + a.F_TAX2 ELSE 0 END AS TOTAL_TAX_C,
                CASE WHEN b.INCLUDE_TAX = N'Y'
                     THEN a.F_TAX1 + a.F_TAX2 ELSE 0 END AS TOTAL_TAX_Y,
                CONVERT(int, 0) AS TOTAL_PIECE_C3,
                CASE WHEN a.I_SIGN_OUT_TIME > '' THEN a.I_CARGO_PIECE ELSE 0 END AS OUT_PIECE,
                CASE WHEN a.I_SIGN_OUT_TIME > '' THEN a.I_BAG_NUMBER ELSE NULL END AS OUT_BAG_NUMBER
            FROM #AIR_BASE_ETL2 AS a
            LEFT JOIN [jetf].[dbo].[customer_master] AS b
                ON b.CUST_ID = a.DESPATCH_NAME
               AND b.TRANS_NO = a.TRANS_TAXPAYMENT
               AND b.TRAN_TYPE = N'空運'
            LEFT JOIN [jetf].[dbo].[customer_price] AS c
                ON c.CUST_ID = a.DESPATCH_NAME
               AND c.TRAN_TYPE = N'進口空快'
               AND c.INCLUDE_TAX = b.INCLUDE_TAX
        ) AS a
        GROUP BY
            a.DATADATE, a.I_DATA_TYPE, a.DESPATCH_NO, a.DESPATCH_NAME,
            a.MAINNUMBER;

        DROP TABLE #AIR_MAIN_TOTAL_ETL2;
        DROP TABLE #AIR_BASE_ETL2;
        DROP TABLE #AIR_SCOPE_ETL2;
        DROP TABLE #AIR_CANDIDATES_ETL2;
    END;
END;
GO
