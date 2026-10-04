USE [jetf];
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- 保留原 SP 的分組、費用、日期及輸出規則；從原始資料取代 MERGE_ORIGINALLIST，並縮小 ETL 明細處理範圍。
ALTER PROCEDURE [dbo].[SP_Select_Income_Details]
    @ORIGINAL nvarchar(8),
    @SDate datetime,
    @EDate datetime
AS
BEGIN
    SET NOCOUNT ON;

    -- 海運查詢達 8 小時時先以清關日期縮小候選；短區間沿用原單起點。
    DECLARE @UseSeaDateFirst bit = CASE WHEN @ORIGINAL = N'SEA'
        AND DATEDIFF(MINUTE, @SDate, @EDate) >= 480 THEN 1 ELSE 0 END;

    -- 空快與短區間海運先取得查詢時段內的主號。
    SELECT DISTINCT c.MAIN_NUMBER
    INTO #MAIN_NUMBERS
    FROM [DATA_CENTER].[dbo].[CLEARANCE_INFO] AS c
    WHERE (@ORIGINAL = N'SEA' AND @UseSeaDateFirst = 0 AND c.SIGN_IN_TIME BETWEEN @SDate AND @EDate)
       OR (@ORIGINAL = N'ETL' AND c.SIGN_IN_TIME BETWEEN DATEADD(HH, 9, @SDate)
                                                       AND DATEADD(HH, 9, @EDate))
    OPTION (RECOMPILE);

    CREATE UNIQUE CLUSTERED INDEX IX_MAIN_NUMBERS
        ON #MAIN_NUMBERS (MAIN_NUMBER);

    -- 兩條海運路徑共用同一個明細暫存表與欄位型別。
    SELECT TOP (0)
        o.DESPATCH_NAME, o.TRANS_NAME, o.JETF_SERIAL,
        o.MAINNUMBER, o.GW, o.PIECE,
        c.DATA_TYPE, c.BAG_NUMBER, c.CLEARANCE_MODEL,
        c.CARGO_PIECE, c.CARGO_WEIGHT, c.SIGN_IN_TIME, c.SIGN_OUT_TIME
    INTO #SEA_SCOPE
    FROM [DATA_CENTER].[dbo].[SEA_ORDER_ORIGINAL] AS o
    INNER JOIN #MAIN_NUMBERS AS m
        ON m.MAIN_NUMBER = o.MAINNUMBER
    OUTER APPLY
    (
        SELECT TOP (1)
            ci.DATA_TYPE, ci.BAG_NUMBER, ci.CLEARANCE_MODEL,
            ci.CARGO_PIECE, ci.CARGO_WEIGHT, ci.SIGN_IN_TIME, ci.SIGN_OUT_TIME
        FROM [DATA_CENTER].[dbo].[CLEARANCE_INFO] AS ci
        WHERE ci.MAIN_NUMBER = o.MAINNUMBER
          AND ci.BAG_NUMBER = o.BL_NO
        ORDER BY ci.MODIFY_TIME DESC, ci.MODIFY_SEQ DESC, ci.ROW_ID DESC
    ) AS c
    WHERE @ORIGINAL = N'SEA'
      AND c.SIGN_IN_TIME BETWEEN @SDate AND @EDate
    OPTION (RECOMPILE);

    IF @UseSeaDateFirst = 1
    BEGIN
        -- 長區間先找日期內的主號與袋號，再找各鍵跨所有日期的最新清關。
        SELECT DISTINCT c.MAIN_NUMBER, c.BAG_NUMBER
        INTO #SEA_KEYS
        FROM [DATA_CENTER].[dbo].[CLEARANCE_INFO] AS c
        WHERE @ORIGINAL = N'SEA'
          AND c.SIGN_IN_TIME BETWEEN @SDate AND @EDate
          AND EXISTS
          (
              SELECT 1
              FROM [DATA_CENTER].[dbo].[SEA_ORDER_ORIGINAL] AS o
              WHERE o.MAINNUMBER = c.MAIN_NUMBER
                AND o.BL_NO = c.BAG_NUMBER
          )
        OPTION (RECOMPILE);

        CREATE UNIQUE CLUSTERED INDEX IX_SEA_KEYS
            ON #SEA_KEYS (MAIN_NUMBER, BAG_NUMBER);

        SELECT
            k.MAIN_NUMBER, k.BAG_NUMBER, c.DATA_TYPE, c.CLEARANCE_MODEL,
            c.CARGO_PIECE, c.CARGO_WEIGHT, c.SIGN_IN_TIME, c.SIGN_OUT_TIME
        INTO #SEA_LATEST
        FROM #SEA_KEYS AS k
        CROSS APPLY
        (
            SELECT TOP (1)
                ci.DATA_TYPE, ci.CLEARANCE_MODEL, ci.CARGO_PIECE,
                ci.CARGO_WEIGHT, ci.SIGN_IN_TIME, ci.SIGN_OUT_TIME
            FROM [DATA_CENTER].[dbo].[CLEARANCE_INFO] AS ci
            WHERE ci.MAIN_NUMBER = k.MAIN_NUMBER
              AND ci.BAG_NUMBER = k.BAG_NUMBER
            ORDER BY ci.MODIFY_TIME DESC, ci.MODIFY_SEQ DESC, ci.ROW_ID DESC
        ) AS c
        WHERE c.SIGN_IN_TIME BETWEEN @SDate AND @EDate
        OPTION (RECOMPILE);

        CREATE UNIQUE CLUSTERED INDEX IX_SEA_LATEST
            ON #SEA_LATEST (MAIN_NUMBER, BAG_NUMBER);

        -- 先將時段內原單獨立實體化，再查費用，保留每張原單的筆數。
        INSERT INTO #SEA_SCOPE
        (
            DESPATCH_NAME, TRANS_NAME, JETF_SERIAL, MAINNUMBER, GW, PIECE,
            DATA_TYPE, BAG_NUMBER, CLEARANCE_MODEL, CARGO_PIECE,
            CARGO_WEIGHT, SIGN_IN_TIME, SIGN_OUT_TIME
        )
        SELECT
            o.DESPATCH_NAME, o.TRANS_NAME, o.JETF_SERIAL,
            o.MAINNUMBER, o.GW, o.PIECE,
            c.DATA_TYPE, c.BAG_NUMBER, c.CLEARANCE_MODEL,
            c.CARGO_PIECE, c.CARGO_WEIGHT, c.SIGN_IN_TIME, c.SIGN_OUT_TIME
        FROM #SEA_LATEST AS c
        INNER JOIN [DATA_CENTER].[dbo].[SEA_ORDER_ORIGINAL] AS o WITH (INDEX(SEA_ORDER_ORIGINAL_INDEX_3), FORCESEEK)
            ON o.MAINNUMBER = c.MAIN_NUMBER
           AND o.BL_NO = c.BAG_NUMBER
        OPTION (RECOMPILE);

        DROP TABLE #SEA_LATEST;
        DROP TABLE #SEA_KEYS;
    END
    ELSE IF @ORIGINAL = N'SEA'
    BEGIN
        -- 短區間從原單主號取得最新清關。
        INSERT INTO #SEA_SCOPE
        (
            DESPATCH_NAME, TRANS_NAME, JETF_SERIAL, MAINNUMBER, GW, PIECE,
            DATA_TYPE, BAG_NUMBER, CLEARANCE_MODEL, CARGO_PIECE,
            CARGO_WEIGHT, SIGN_IN_TIME, SIGN_OUT_TIME
        )
        SELECT
            o.DESPATCH_NAME, o.TRANS_NAME, o.JETF_SERIAL,
            o.MAINNUMBER, o.GW, o.PIECE,
            c.DATA_TYPE, c.BAG_NUMBER, c.CLEARANCE_MODEL,
            c.CARGO_PIECE, c.CARGO_WEIGHT, c.SIGN_IN_TIME, c.SIGN_OUT_TIME
        FROM [DATA_CENTER].[dbo].[SEA_ORDER_ORIGINAL] AS o
        INNER JOIN #MAIN_NUMBERS AS m
            ON m.MAIN_NUMBER = o.MAINNUMBER
        OUTER APPLY
        (
            SELECT TOP (1)
                ci.DATA_TYPE, ci.BAG_NUMBER, ci.CLEARANCE_MODEL,
                ci.CARGO_PIECE, ci.CARGO_WEIGHT, ci.SIGN_IN_TIME, ci.SIGN_OUT_TIME
            FROM [DATA_CENTER].[dbo].[CLEARANCE_INFO] AS ci
            WHERE ci.MAIN_NUMBER = o.MAINNUMBER
              AND ci.BAG_NUMBER = o.BL_NO
            ORDER BY ci.MODIFY_TIME DESC, ci.MODIFY_SEQ DESC, ci.ROW_ID DESC
        ) AS c
        WHERE @ORIGINAL = N'SEA'
          AND c.SIGN_IN_TIME BETWEEN @SDate AND @EDate
        OPTION (RECOMPILE);

    END

    -- 與原 MERGE_ORIGINALLIST 海運欄位相同的資料來源。
    SELECT
        CONVERT(varchar(3), 'SEA') AS ORIGINAL,
        s.DATA_TYPE AS I_DATA_TYPE,
        CONVERT(nvarchar(200), s.DESPATCH_NAME) AS DESPATCH_NAME,
        CONVERT(nvarchar(200), s.TRANS_NAME) AS TRANS_NAME,
        CONVERT(nvarchar(200), NULL) AS TRANS_TAXPAYMENT,
        CONVERT(varchar(20), s.BAG_NUMBER) AS I_BAG_NUMBER,
        CONVERT(varchar(30), NULL) AS I_MERGE_NUMBER,
        s.JETF_SERIAL,
        s.MAINNUMBER,
        s.GW,
        s.PIECE,
        s.CLEARANCE_MODEL AS I_CLEARANCE_MODEL,
        s.CARGO_PIECE AS I_CARGO_PIECE,
        CONVERT(float, s.CARGO_WEIGHT) AS I_CARGO_WEIGHT,
        s.SIGN_IN_TIME AS I_SIGN_IN_TIME,
        s.SIGN_OUT_TIME AS I_SIGN_OUT_TIME,
        f.FEE AS F_FEE,
        f.TAX1 AS F_TAX1,
        f.TAX2 AS F_TAX2,
        f.CCFEE AS F_CCFEE
    INTO #SOURCE
    FROM #SEA_SCOPE AS s
    OUTER APPLY
    (
        SELECT TOP (1) fm.FEE, fm.TAX1, fm.TAX2, fm.CCFEE
        FROM [jetf].[dbo].[FEE_MASTER] AS fm
        WHERE fm.DLV_INV = s.JETF_SERIAL
          AND (fm.SOURCE_TYPE = N'2' OR (fm.SOURCE_TYPE = N'1' AND s.GW > 0))
        ORDER BY CASE WHEN fm.SOURCE_TYPE = N'2' THEN 0 ELSE 1 END,
                 fm.MODIFTYDATE DESC, fm.ID DESC
    ) AS f
    WHERE @ORIGINAL = N'SEA'
    OPTION (RECOMPILE);

    IF @ORIGINAL = N'ETL'
    BEGIN
        DECLARE @ETLFilterStart datetime = DATEADD(HOUR, 9, @SDate);
        DECLARE @ETLFilterEnd datetime = DATEADD(HOUR, 9, @EDate);

        -- TOTAL_COUNT 仍包含整個報表日，且包含隔日 09:00 的邊界。
        DECLARE @ETLCountStart datetime = DATEADD
        (
            HOUR, 9, CONVERT(datetime, CONVERT(date, @SDate))
        );
        DECLARE @ETLCountEnd datetime = DATEADD
        (
            DAY, 1, DATEADD(HOUR, 9,
                CONVERT(datetime, CONVERT(date, @EDate)))
        );

        -- 先用日期與三種配對找候選原單；同一原單可能配到多筆清關，只保留 ID。
        SELECT o.ID AS ORDER_ID
        INTO #ETL_CANDIDATES
        FROM [DATA_CENTER].[dbo].[CLEARANCE_INFO] AS c
        INNER JOIN #MAIN_NUMBERS AS m
            ON m.MAIN_NUMBER = c.MAIN_NUMBER
        INNER JOIN [DATA_CENTER].[dbo].[ORIGINALLIST] AS o
            ON o.MAINNUMBER = c.MAIN_NUMBER
           AND o.TRACKINGUB = c.MERGE_NUMBER
        WHERE c.SIGN_IN_TIME BETWEEN @ETLCountStart AND @ETLCountEnd
          AND c.MERGE_NUMBER > N''

        UNION

        SELECT o.ID
        FROM [DATA_CENTER].[dbo].[CLEARANCE_INFO] AS c
        INNER JOIN #MAIN_NUMBERS AS m
            ON m.MAIN_NUMBER = c.MAIN_NUMBER
        INNER JOIN [DATA_CENTER].[dbo].[ORIGINALLIST] AS o
            ON o.MAINNUMBER = c.MAIN_NUMBER
           AND o.BAGNO = c.BAG_NUMBER
        WHERE c.SIGN_IN_TIME BETWEEN @ETLCountStart AND @ETLCountEnd
          AND c.MERGE_NUMBER = N''

        UNION

        SELECT o.ID
        FROM [DATA_CENTER].[dbo].[CLEARANCE_INFO] AS c
        INNER JOIN #MAIN_NUMBERS AS m
            ON m.MAIN_NUMBER = c.MAIN_NUMBER
        INNER JOIN [DATA_CENTER].[dbo].[ORIGINALLIST] AS o
            ON o.MAINNUMBER = c.MAIN_NUMBER
           AND o.BAGNO = c.MERGE_NUMBER
        WHERE c.SIGN_IN_TIME BETWEEN @ETLCountStart AND @ETLCountEnd
          AND c.DATA_TYPE = N'FTZ'
        OPTION (RECOMPILE);

        CREATE UNIQUE CLUSTERED INDEX IX_ETL_CANDIDATES
            ON #ETL_CANDIDATES (ORDER_ID);

        -- 計數欄位沿用 #SOURCE 型別與定序，避免跨資料庫文字欄位比較改變結果。
        SELECT TOP (0)
            s.MAINNUMBER, s.DESPATCH_NAME, s.TRANS_NAME,
            s.TRANS_TAXPAYMENT, o.DELIVERYNO,
            s.GW AS BAGWEIGHT, s.PIECE AS PIECES,
            s.I_DATA_TYPE, s.I_BAG_NUMBER, s.I_MERGE_NUMBER,
            s.I_CLEARANCE_MODEL, s.I_CARGO_PIECE, s.I_CARGO_WEIGHT,
            s.I_SIGN_IN_TIME, s.I_SIGN_OUT_TIME
        INTO #ETL_SCOPE
        FROM #SOURCE AS s
        CROSS JOIN [DATA_CENTER].[dbo].[ORIGINALLIST] AS o;

        -- TOP(1) 必須先跨所有日期選最新修改版，再篩清關日期。
        INSERT INTO #ETL_SCOPE
        (
            MAINNUMBER, DESPATCH_NAME, TRANS_NAME, TRANS_TAXPAYMENT,
            DELIVERYNO, BAGWEIGHT, PIECES, I_DATA_TYPE,
            I_BAG_NUMBER, I_MERGE_NUMBER, I_CLEARANCE_MODEL,
            I_CARGO_PIECE, I_CARGO_WEIGHT, I_SIGN_IN_TIME, I_SIGN_OUT_TIME
        )
        SELECT
            o.MAINNUMBER,
            CONVERT(nvarchar(200), o.DESPATCHNO) AS DESPATCH_NAME,
            CONVERT(nvarchar(200),
                [jetf].[dbo].[GetTRANS_NAME](o.CLEARANCEWAREHOUSING)) AS TRANS_NAME,
            CONVERT(nvarchar(200), o.TRANS_TAXPAYMENT) AS TRANS_TAXPAYMENT,
            o.DELIVERYNO, o.BAGWEIGHT, o.PIECES,
            c.DATA_TYPE AS I_DATA_TYPE,
            CONVERT(varchar(20), c.BAG_NUMBER) AS I_BAG_NUMBER,
            CONVERT(varchar(30), c.MERGE_NUMBER) AS I_MERGE_NUMBER,
            c.CLEARANCE_MODEL AS I_CLEARANCE_MODEL,
            c.CARGO_PIECE AS I_CARGO_PIECE,
            CONVERT(float, CASE
                WHEN c.ROW_ID IS NULL THEN NULL
                WHEN c.MERGE_NUMBER > N'' AND o.TRACKINGUB = c.MERGE_NUMBER
                    THEN c.CARGO_WEIGHT
                WHEN o.ID =
                     (SELECT MIN(o2.ID)
                      FROM [DATA_CENTER].[dbo].[ORIGINALLIST] AS o2
                      WHERE o2.MAINNUMBER = o.MAINNUMBER AND o2.BAGNO = o.BAGNO)
                    THEN c.CARGO_WEIGHT
                ELSE 0
            END) AS I_CARGO_WEIGHT,
            c.SIGN_IN_TIME AS I_SIGN_IN_TIME,
            c.SIGN_OUT_TIME AS I_SIGN_OUT_TIME
        FROM #ETL_CANDIDATES AS candidate
        INNER JOIN [DATA_CENTER].[dbo].[ORIGINALLIST] AS o
            ON o.ID = candidate.ORDER_ID
        CROSS APPLY
        (
            SELECT TOP (1)
                ci.ROW_ID, ci.DATA_TYPE, ci.BAG_NUMBER, ci.MERGE_NUMBER,
                ci.CLEARANCE_MODEL, ci.CARGO_PIECE, ci.CARGO_WEIGHT,
                ci.SIGN_IN_TIME, ci.SIGN_OUT_TIME
            FROM [DATA_CENTER].[dbo].[CLEARANCE_INFO] AS ci
            WHERE ci.MAIN_NUMBER = o.MAINNUMBER
              AND
              (
                  (ci.MERGE_NUMBER > N'' AND o.TRACKINGUB = ci.MERGE_NUMBER)
                  OR (ci.MERGE_NUMBER = N'' AND o.BAGNO = ci.BAG_NUMBER)
                  OR (ci.DATA_TYPE = N'FTZ' AND o.BAGNO = ci.MERGE_NUMBER)
              )
            ORDER BY ci.MODIFY_TIME DESC, ci.MODIFY_SEQ DESC,
                     CASE
                         WHEN ci.MERGE_NUMBER > N'' AND o.TRACKINGUB = ci.MERGE_NUMBER THEN 1
                         WHEN ci.MERGE_NUMBER = N'' AND o.BAGNO = ci.BAG_NUMBER THEN 2
                         ELSE 3
                     END,
                     ci.ROW_ID DESC
        ) AS c
        WHERE c.SIGN_IN_TIME BETWEEN @ETLCountStart AND @ETLCountEnd
        OPTION (RECOMPILE);

        DROP TABLE #ETL_CANDIDATES;

        CREATE NONCLUSTERED INDEX IX_ETL_SCOPE_COUNT
            ON #ETL_SCOPE (MAINNUMBER, I_SIGN_IN_TIME)
            INCLUDE (I_DATA_TYPE, DESPATCH_NAME, TRANS_NAME,
                     I_BAG_NUMBER, I_MERGE_NUMBER);

        -- 費用只查實際輸出的原單；計數所需的整日原單保留在 #ETL_SCOPE。
        INSERT INTO #SOURCE
        (
            ORIGINAL, I_DATA_TYPE, DESPATCH_NAME, TRANS_NAME, TRANS_TAXPAYMENT,
            I_BAG_NUMBER, I_MERGE_NUMBER, JETF_SERIAL, MAINNUMBER, GW, PIECE,
            I_CLEARANCE_MODEL, I_CARGO_PIECE, I_CARGO_WEIGHT,
            I_SIGN_IN_TIME, I_SIGN_OUT_TIME, F_FEE, F_TAX1, F_TAX2, F_CCFEE
        )
        SELECT
            N'ETL', s.I_DATA_TYPE, s.DESPATCH_NAME, s.TRANS_NAME,
            s.TRANS_TAXPAYMENT, s.I_BAG_NUMBER, s.I_MERGE_NUMBER,
            NULL, s.MAINNUMBER, s.BAGWEIGHT, s.PIECES,
            s.I_CLEARANCE_MODEL, s.I_CARGO_PIECE, s.I_CARGO_WEIGHT,
            s.I_SIGN_IN_TIME, s.I_SIGN_OUT_TIME,
            f.FEE, f.TAX1, f.TAX2, f.CCFEE
        FROM #ETL_SCOPE AS s
        OUTER APPLY
        (
            SELECT TOP (1) fm.FEE, fm.TAX1, fm.TAX2, fm.CCFEE
            FROM [jetf].[dbo].[FEE_MASTER] AS fm
            WHERE fm.DLV_INV = s.DELIVERYNO AND fm.SOURCE_TYPE = N'3'
            ORDER BY fm.MODIFTYDATE DESC, fm.ID DESC
        ) AS f
        WHERE s.I_SIGN_IN_TIME BETWEEN @ETLFilterStart AND @ETLFilterEnd
        OPTION (RECOMPILE);
    END

    -- 建立與原 #SOURCE 聚合相同的欄位型別；資料依來源分開填入。
    SELECT TOP (0)
        MAINNUMBER,
        SUM(GW) AS SEA_TOTAL_GW_ALL,
        SUM(PIECE) AS SEA_TOTAL_PIECE_ALL,
        SUM(I_CARGO_WEIGHT) AS ETL_TOTAL_GW_ALL,
        SUM(I_CARGO_PIECE) AS ETL_TOTAL_PIECE_ALL
    INTO #MAIN_TOTALS
    FROM #SOURCE
    GROUP BY MAINNUMBER;

    IF @ORIGINAL = N'ETL'
    BEGIN
        -- 全主號貨重/件數只對真正輸出的主號重算，仍包含所有日期與海運原單。
        SELECT DISTINCT s.MAINNUMBER
        INTO #ETL_OUTPUT_MAINS
        FROM #SOURCE AS s;

        CREATE UNIQUE CLUSTERED INDEX IX_ETL_OUTPUT_MAINS
            ON #ETL_OUTPUT_MAINS (MAINNUMBER);

        INSERT INTO #MAIN_TOTALS
        (
            MAINNUMBER, ETL_TOTAL_GW_ALL, ETL_TOTAL_PIECE_ALL
        )
        SELECT
            m.MAINNUMBER,
            SUM(all_clearance.I_CARGO_WEIGHT),
            SUM(all_clearance.I_CARGO_PIECE)
        FROM #ETL_OUTPUT_MAINS AS m
        OUTER APPLY
        (
            SELECT
                CONVERT(float, CASE
                    WHEN c.ROW_ID IS NULL THEN NULL
                    WHEN c.MERGE_NUMBER > N'' AND o.TRACKINGUB = c.MERGE_NUMBER
                        THEN c.CARGO_WEIGHT
                    WHEN o.ID =
                         (SELECT MIN(o2.ID)
                          FROM [DATA_CENTER].[dbo].[ORIGINALLIST] AS o2
                          WHERE o2.MAINNUMBER = o.MAINNUMBER AND o2.BAGNO = o.BAGNO)
                        THEN c.CARGO_WEIGHT
                    ELSE 0
                END) AS I_CARGO_WEIGHT,
                c.CARGO_PIECE AS I_CARGO_PIECE
            FROM [DATA_CENTER].[dbo].[ORIGINALLIST] AS o
            OUTER APPLY
            (
                SELECT TOP (1)
                    ci.ROW_ID, ci.MERGE_NUMBER, ci.CARGO_WEIGHT, ci.CARGO_PIECE
                FROM [DATA_CENTER].[dbo].[CLEARANCE_INFO] AS ci
                WHERE ci.MAIN_NUMBER = o.MAINNUMBER
                  AND
                  (
                      (ci.MERGE_NUMBER > N'' AND o.TRACKINGUB = ci.MERGE_NUMBER)
                      OR (ci.MERGE_NUMBER = N'' AND o.BAGNO = ci.BAG_NUMBER)
                      OR (ci.DATA_TYPE = N'FTZ' AND o.BAGNO = ci.MERGE_NUMBER)
                  )
                ORDER BY ci.MODIFY_TIME DESC, ci.MODIFY_SEQ DESC,
                         CASE
                             WHEN ci.MERGE_NUMBER > N'' AND o.TRACKINGUB = ci.MERGE_NUMBER THEN 1
                             WHEN ci.MERGE_NUMBER = N'' AND o.BAGNO = ci.BAG_NUMBER THEN 2
                             ELSE 3
                         END,
                         ci.ROW_ID DESC
            ) AS c
            WHERE o.MAINNUMBER = m.MAINNUMBER

            UNION ALL

            SELECT CONVERT(float, c.CARGO_WEIGHT), c.CARGO_PIECE
            FROM [DATA_CENTER].[dbo].[SEA_ORDER_ORIGINAL] AS o
            OUTER APPLY
            (
                SELECT TOP (1) ci.CARGO_WEIGHT, ci.CARGO_PIECE
                FROM [DATA_CENTER].[dbo].[CLEARANCE_INFO] AS ci
                WHERE ci.MAIN_NUMBER = o.MAINNUMBER
                  AND ci.BAG_NUMBER = o.BL_NO
                ORDER BY ci.MODIFY_TIME DESC, ci.MODIFY_SEQ DESC, ci.ROW_ID DESC
            ) AS c
            WHERE o.MAINNUMBER = m.MAINNUMBER
        ) AS all_clearance
        GROUP BY m.MAINNUMBER
        OPTION (RECOMPILE);

        DROP TABLE #ETL_OUTPUT_MAINS;
    END

    -- 海運的全日期總量只需要原單重量/件數；依實際輸出主號彙總，避免先載入所有原單明細。
    -- 空運重量轉為海運原單的 numeric(18, 3)，維持原 #SOURCE 的轉型規則。
    IF @ORIGINAL = N'SEA'
    BEGIN
        SELECT DISTINCT s.MAINNUMBER
        INTO #SEA_OUTPUT_MAINS
        FROM #SOURCE AS s;

        CREATE UNIQUE CLUSTERED INDEX IX_SEA_OUTPUT_MAINS
            ON #SEA_OUTPUT_MAINS (MAINNUMBER);

        INSERT INTO #MAIN_TOTALS
        (
            MAINNUMBER, SEA_TOTAL_GW_ALL, SEA_TOTAL_PIECE_ALL
        )
        SELECT m.MAINNUMBER, total.GW, total.PIECE
        FROM #SEA_OUTPUT_MAINS AS m
        CROSS APPLY
        (
            SELECT SUM(raw.GW) AS GW, SUM(raw.PIECE) AS PIECE
            FROM
            (
                SELECT s.GW, s.PIECE
                FROM [DATA_CENTER].[dbo].[SEA_ORDER_ORIGINAL] AS s
                WHERE s.MAINNUMBER = m.MAINNUMBER

                UNION ALL

                SELECT CONVERT(numeric(18, 3), a.BAGWEIGHT), a.PIECES
                FROM [DATA_CENTER].[dbo].[ORIGINALLIST] AS a
                WHERE a.MAINNUMBER = m.MAINNUMBER
            ) AS raw
        ) AS total
        OPTION (RECOMPILE);

        DROP TABLE #SEA_OUTPUT_MAINS;
    END

    CREATE UNIQUE CLUSTERED INDEX IX_MAIN_TOTALS
        ON #MAIN_TOTALS (MAINNUMBER);

    IF @ORIGINAL = N'SEA'
    BEGIN
        SELECT
            g.DATADATE, g.I_DATA_TYPE, g.DESPATCH_NO, g.DESPATCH_NAME,
            g.TRANS_NO, g.TRANS_NAME, g.INCLUDE_TAX, g.MAINNUMBER,
            g.TARIFF, g.CC, g.Total_TARIFF, g.Total_CC, g.TOTAL_FEE,
            g.TOTAL_TAX_N, g.TOTAL_TAX_C, g.TOTAL_TAX_Y, g.TOTAL_CCFEE,
            g.TOTAL_PIECE, g.TOTAL_OUT_PIECE, g.TOTAL_GW,
            g.TOTAL_BAG_NUMBER, g.TOTAL_COUNT, g.TOTAL_PIECE_C3,
            CONVERT(nvarchar(50),
                CONVERT(nvarchar(20), totals.SEA_TOTAL_GW_ALL) + N',' +
                CONVERT(nvarchar(20), totals.SEA_TOTAL_PIECE_ALL)) AS TOTAL_GW_PIECE_All
        FROM
        (
            SELECT
                DATADATE, I_DATA_TYPE, DESPATCH_NO, DESPATCH_NAME,
                TRANS_NO, TRANS_NAME, INCLUDE_TAX, MAINNUMBER, TARIFF, CC,
                CEILING(SUM(GW) * TARIFF) AS Total_TARIFF,
                CEILING(SUM(GW) * CC) AS Total_CC,
                SUM(F_FEE) AS TOTAL_FEE,
                SUM(TOTAL_TAX_N) AS TOTAL_TAX_N,
                SUM(TOTAL_TAX_C) AS TOTAL_TAX_C,
                SUM(TOTAL_TAX_Y) AS TOTAL_TAX_Y,
                SUM(F_CCFEE) AS TOTAL_CCFEE,
                SUM(PIECE) AS TOTAL_PIECE,
                SUM(OUT_PIECE) AS TOTAL_OUT_PIECE,
                SUM(GW) AS TOTAL_GW,
                COUNT(DISTINCT I_BAG_NUMBER) AS TOTAL_BAG_NUMBER,
                COUNT(DISTINCT JETF_SERIAL) AS TOTAL_COUNT,
                SUM(TOTAL_PIECE_C3) AS TOTAL_PIECE_C3
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
                    c.TARIFF,
                    c.CC,
                    a.GW,
                    a.F_FEE,
                    a.F_TAX1,
                    a.F_TAX2,
                    a.F_CCFEE,
                    a.PIECE,
                    CASE
                        WHEN b.INCLUDE_TAX = N'D' OR b.INCLUDE_TAX = N'N'
                          OR b.INCLUDE_TAX IS NULL
                            THEN a.F_TAX1 + a.F_TAX2
                        ELSE 0
                    END AS TOTAL_TAX_N,
                    CASE WHEN b.INCLUDE_TAX = N'C'
                         THEN a.F_TAX1 + a.F_TAX2 ELSE 0 END AS TOTAL_TAX_C,
                    CASE WHEN b.INCLUDE_TAX = N'Y'
                         THEN a.F_TAX1 + a.F_TAX2 ELSE 0 END AS TOTAL_TAX_Y,
                    CASE WHEN a.I_CLEARANCE_MODEL = N'C3' AND a.GW > 0
                         THEN a.I_CARGO_PIECE ELSE 0 END AS TOTAL_PIECE_C3,
                    CASE WHEN a.I_SIGN_OUT_TIME > ''
                         THEN a.PIECE ELSE 0 END AS OUT_PIECE
                FROM #SOURCE AS a
                LEFT JOIN [jetf].[dbo].[customer_master] AS b
                    ON b.CUST_ID = a.DESPATCH_NAME
                   AND b.TRANS_NAME = a.TRANS_NAME
                   AND b.TRAN_TYPE = N'海運'
                LEFT JOIN [jetf].[dbo].[customer_price] AS c
                    ON c.CUST_ID = a.DESPATCH_NAME
                   AND c.TRAN_TYPE = N'進口海快'
                   AND c.INCLUDE_TAX = b.INCLUDE_TAX
                WHERE a.ORIGINAL = @ORIGINAL
                  AND a.I_SIGN_IN_TIME BETWEEN @SDate AND @EDate
            ) AS a
            GROUP BY
                DATADATE, I_DATA_TYPE, TRANS_NAME, DESPATCH_NO, DESPATCH_NAME,
                TRANS_NO, MAINNUMBER, INCLUDE_TAX, TARIFF, CC
        ) AS g
        LEFT JOIN #MAIN_TOTALS AS totals
            ON totals.MAINNUMBER = g.MAINNUMBER
        OPTION (RECOMPILE);
    END
    ELSE IF @ORIGINAL = N'ETL'
    BEGIN
        SELECT
            g.DATADATE, g.I_DATA_TYPE, g.DESPATCH_NO, g.DESPATCH_NAME,
            g.TRANS_NO, g.TRANS_NAME, g.INCLUDE_TAX, g.MAINNUMBER,
            g.TARIFF, g.CC, g.Total_TARIFF, g.Total_CC, g.TOTAL_FEE,
            g.TOTAL_TAX_N, g.TOTAL_TAX_C, g.TOTAL_TAX_Y, g.TOTAL_CCFEE,
            g.TOTAL_PIECE, g.TOTAL_OUT_PIECE, g.TOTAL_GW,
            g.TOTAL_BAG_NUMBER, counts.TOTAL_COUNT, g.TOTAL_PIECE_C3,
            CONVERT(nvarchar(50),
                CONVERT(nvarchar(20), totals.ETL_TOTAL_GW_ALL) + N',' +
                CONVERT(nvarchar(20), totals.ETL_TOTAL_PIECE_ALL)) AS TOTAL_GW_PIECE_All
        FROM
        (
            SELECT
                DATADATE, I_DATA_TYPE, DESPATCH_NO, DESPATCH_NAME,
                TRANS_NO, TRANS_NAME, INCLUDE_TAX, MAINNUMBER, TARIFF, CC,
                CONVERT(int, CEILING(SUM(GW) * TARIFF)) AS Total_TARIFF,
                CONVERT(int, CEILING(SUM(GW) * CC)) AS Total_CC,
                SUM(F_FEE) AS TOTAL_FEE,
                SUM(TOTAL_TAX_N) AS TOTAL_TAX_N,
                SUM(TOTAL_TAX_C) AS TOTAL_TAX_C,
                SUM(TOTAL_TAX_Y) AS TOTAL_TAX_Y,
                SUM(F_CCFEE) AS TOTAL_CCFEE,
                SUM(PIECE) AS TOTAL_PIECE,
                SUM(OUT_PIECE) AS TOTAL_OUT_PIECE,
                CONVERT(numeric(8, 2), SUM(GW)) AS TOTAL_GW,
                COUNT(DISTINCT I_BAG_NUMBER) AS TOTAL_BAG_NUMBER,
                SUM(TOTAL_PIECE_C3) AS TOTAL_PIECE_C3
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
                    c.TARIFF,
                    c.CC,
                    a.I_CARGO_WEIGHT AS GW,
                    a.F_FEE,
                    a.F_TAX1,
                    a.F_TAX2,
                    a.F_CCFEE,
                    a.I_CARGO_PIECE AS PIECE,
                    CASE
                        WHEN b.INCLUDE_TAX = N'D' OR b.INCLUDE_TAX = N'N'
                          OR b.INCLUDE_TAX IS NULL
                            THEN a.F_TAX1 + a.F_TAX2
                        ELSE 0
                    END AS TOTAL_TAX_N,
                    CASE WHEN b.INCLUDE_TAX = N'C'
                         THEN a.F_TAX1 + a.F_TAX2 ELSE 0 END AS TOTAL_TAX_C,
                    CASE WHEN b.INCLUDE_TAX = N'Y'
                         THEN a.F_TAX1 + a.F_TAX2 ELSE 0 END AS TOTAL_TAX_Y,
                    0 AS TOTAL_PIECE_C3,
                    CASE WHEN a.I_SIGN_OUT_TIME > ''
                         THEN a.I_CARGO_PIECE ELSE 0 END AS OUT_PIECE
                FROM #SOURCE AS a
                LEFT JOIN [jetf].[dbo].[customer_master] AS b
                    ON b.CUST_ID = a.DESPATCH_NAME
                   AND b.TRANS_NO = a.TRANS_TAXPAYMENT
                   AND b.TRAN_TYPE = N'空運'
                LEFT JOIN [jetf].[dbo].[customer_price] AS c
                    ON c.CUST_ID = a.DESPATCH_NAME
                   AND c.TRAN_TYPE = N'進口空快'
                   AND c.INCLUDE_TAX = b.INCLUDE_TAX
                WHERE a.ORIGINAL = @ORIGINAL
                  AND a.I_SIGN_IN_TIME BETWEEN DATEADD(HH, 9, @SDate)
                                           AND DATEADD(HH, 9, @EDate)
            ) AS a
            GROUP BY
                DATADATE, I_DATA_TYPE, TRANS_NAME, DESPATCH_NO, DESPATCH_NAME,
                TRANS_NO, MAINNUMBER, INCLUDE_TAX, TARIFF, CC
        ) AS g
        OUTER APPLY
        (
            SELECT COUNT(1) AS TOTAL_COUNT
            FROM
            (
                SELECT DISTINCT s.I_BAG_NUMBER, s.I_MERGE_NUMBER
                FROM #ETL_SCOPE AS s
                WHERE s.I_DATA_TYPE = g.I_DATA_TYPE
                  AND s.DESPATCH_NAME = g.DESPATCH_NO
                  AND s.MAINNUMBER = g.MAINNUMBER
                  AND s.I_SIGN_IN_TIME BETWEEN
                      DATEADD(HH, 9, CONVERT(datetime, g.DATADATE))
                      AND DATEADD(DAY, 1, DATEADD(HH, 9, CONVERT(datetime, g.DATADATE)))
                  AND
                  (
                      (g.TRANS_NAME > N'' AND s.TRANS_NAME = g.TRANS_NAME)
                      OR ((g.TRANS_NAME IS NULL OR g.TRANS_NAME <= N'')
                          AND s.TRANS_NAME IS NULL)
                  )
            ) AS distinct_bags
        ) AS counts
        LEFT JOIN #MAIN_TOTALS AS totals
            ON totals.MAINNUMBER = g.MAINNUMBER
        OPTION (RECOMPILE);

        DROP TABLE #ETL_SCOPE;
    END

    DROP TABLE #MAIN_TOTALS;
    DROP TABLE #SOURCE;
    DROP TABLE #SEA_SCOPE;
    DROP TABLE #MAIN_NUMBERS;
END;
GO
