-- 取得批量貨況查詢明細表資料
-- 優化重點：
-- 1. 不再讀取舊的彙整來源表
-- 2. 先縮小本次上傳批次的 TrackingNo，再查詢大表
-- 3. 將原本 OR 條件拆成多段 UNION，讓 BL_NO/JETF_SERIAL/BAGNO/TRACKINGUB 的索引可被使用
-- 4. I_DATA_TYPE / I_SIGN_IN_TIME / I_SIGN_OUT_TIME 改由 [DATA_CENTER].[dbo].[CLEARANCE_INFO] 取得

CREATE OR ALTER PROCEDURE [dbo].[USP_GetBatchSearchCargo_test]
    @Upload_Ope nvarchar(10),
    @Upload_Time datetime
AS
BEGIN
    SET NOCOUNT ON;

    DROP TABLE IF EXISTS #UploadRows;
    DROP TABLE IF EXISTS #UploadTracking;
    DROP TABLE IF EXISTS #SysCust;
    DROP TABLE IF EXISTS #CargoData;
    DROP TABLE IF EXISTS #CargoKeys;

    SELECT a.TrackingNo
    INTO #UploadRows
    FROM [jetf].[dbo].[BatchSearchCargo2] a WITH (NOLOCK)
    WHERE a.Upload_Ope = @Upload_Ope
      AND a.Upload_Time = @Upload_Time;

    CREATE CLUSTERED INDEX [IX_UploadRows_TrackingNo]
        ON #UploadRows ([TrackingNo]);

    SELECT DISTINCT a.TrackingNo
    INTO #UploadTracking
    FROM #UploadRows a
    WHERE a.TrackingNo IS NOT NULL;

    CREATE UNIQUE CLUSTERED INDEX [IX_UploadTracking_TrackingNo]
        ON #UploadTracking ([TrackingNo]);

    SELECT
        x.CUST_TYPE,
        x.CUST_CODE,
        MAX(x.CUST_NAME) AS CUST_NAME
    INTO #SysCust
    FROM
    (
        SELECT
            N'AIR' AS CUST_TYPE,
            c.OLD_CODE AS CUST_CODE,
            c.CUST_NAME
        FROM [DATA_CENTER].[dbo].[SYS_CUST] c WITH (NOLOCK)
        WHERE c.CUST_TYPE = 'AIR'

        UNION ALL

        SELECT
            N'SEA' AS CUST_TYPE,
            c.CUST_CODE,
            c.CUST_NAME
        FROM [DATA_CENTER].[dbo].[SYS_CUST] c WITH (NOLOCK)
        WHERE c.CUST_TYPE = 'SEA'
    ) x
    WHERE x.CUST_CODE IS NOT NULL
    GROUP BY
        x.CUST_TYPE,
        x.CUST_CODE;

    CREATE UNIQUE CLUSTERED INDEX [IX_SysCust_CustType_CustCode]
        ON #SysCust ([CUST_TYPE], [CUST_CODE])
        WITH (IGNORE_DUP_KEY = ON);

    ;WITH SourceCargo AS
    (
        SELECT
            u.TrackingNo,
            s.TRANS_TAXPAYMENT,
            s.ETA,
            ci.I_DATA_TYPE,
            s.DESPATCH_NAME,
            seaCust.CUST_NAME AS CUSTOMER,
            s.MAINNUMBER,
            s.BL_NO,
            s.JETF_SERIAL,
            ci.I_SIGN_IN_TIME,
            ci.I_SIGN_OUT_TIME,
            s.TRANS_NAME,
            s.JETF_SERIAL AS DELIVERYNO,
            s.IMPORTER,
            s.IM_PHONENO,
            s.IM_ADD,
            CAST(NULL AS nvarchar(100)) AS FIELD_X,
            CAST(NULL AS nvarchar(100)) AS ORDER_NO,
            CAST(NULL AS nvarchar(100)) AS EXPRESS_NO
        FROM #UploadTracking u
        INNER JOIN [DATA_CENTER].[dbo].[SEA_ORDER_ORIGINAL] s WITH (NOLOCK)
            ON s.BL_NO = u.TrackingNo
        LEFT JOIN #SysCust seaCust
            ON seaCust.CUST_TYPE = N'SEA'
           AND seaCust.CUST_CODE = s.DESPATCH_NAME
        OUTER APPLY
        (
            SELECT TOP (1)
                c.DATA_TYPE AS I_DATA_TYPE,
                c.SIGN_IN_TIME AS I_SIGN_IN_TIME,
                c.SIGN_OUT_TIME AS I_SIGN_OUT_TIME
            FROM [DATA_CENTER].[dbo].[CLEARANCE_INFO] c WITH (NOLOCK)
            WHERE c.MAIN_NUMBER = s.MAINNUMBER
              AND c.BAG_NUMBER = s.BL_NO
            ORDER BY c.SIGN_OUT_TIME DESC, c.SIGN_IN_TIME DESC
        ) ci

        UNION

        SELECT
            u.TrackingNo,
            s.TRANS_TAXPAYMENT,
            s.ETA,
            ci.I_DATA_TYPE,
            s.DESPATCH_NAME,
            seaCust.CUST_NAME AS CUSTOMER,
            s.MAINNUMBER,
            s.BL_NO,
            s.JETF_SERIAL,
            ci.I_SIGN_IN_TIME,
            ci.I_SIGN_OUT_TIME,
            s.TRANS_NAME,
            s.JETF_SERIAL AS DELIVERYNO,
            s.IMPORTER,
            s.IM_PHONENO,
            s.IM_ADD,
            CAST(NULL AS nvarchar(100)) AS FIELD_X,
            CAST(NULL AS nvarchar(100)) AS ORDER_NO,
            CAST(NULL AS nvarchar(100)) AS EXPRESS_NO
        FROM #UploadTracking u
        INNER JOIN [DATA_CENTER].[dbo].[SEA_ORDER_ORIGINAL] s WITH (NOLOCK)
            ON s.JETF_SERIAL = u.TrackingNo
        LEFT JOIN #SysCust seaCust
            ON seaCust.CUST_TYPE = N'SEA'
           AND seaCust.CUST_CODE = s.DESPATCH_NAME
        OUTER APPLY
        (
            SELECT TOP (1)
                c.DATA_TYPE AS I_DATA_TYPE,
                c.SIGN_IN_TIME AS I_SIGN_IN_TIME,
                c.SIGN_OUT_TIME AS I_SIGN_OUT_TIME
            FROM [DATA_CENTER].[dbo].[CLEARANCE_INFO] c WITH (NOLOCK)
            WHERE c.MAIN_NUMBER = s.MAINNUMBER
              AND c.BAG_NUMBER = s.BL_NO
            ORDER BY c.SIGN_OUT_TIME DESC, c.SIGN_IN_TIME DESC
        ) ci

        UNION

        SELECT
            u.TrackingNo,
            o.TRANS_TAXPAYMENT,
            m.DELIVERYDATE AS ETA,
            ci.I_DATA_TYPE,
            o.DESPATCHNO AS DESPATCH_NAME,
            airCust.CUST_NAME AS CUSTOMER,
            o.MAINNUMBER,
            o.BAGNO AS BL_NO,
            o.TRACKINGUB AS JETF_SERIAL,
            ci.I_SIGN_IN_TIME,
            ci.I_SIGN_OUT_TIME,
            [jetf].[dbo].[GetTRANS_NAME](o.CLEARANCEWAREHOUSING) AS TRANS_NAME,
            o.DELIVERYNO,
            o.RECIPIENT AS IMPORTER,
            o.RECPHONE AS IM_PHONENO,
            o.RECADDRESS AS IM_ADD,
            o.FIELD_X,
            o.ORDER_NO,
            o.EXPRESS_NO
        FROM #UploadTracking u
        INNER JOIN [DATA_CENTER].[dbo].[ORIGINALLIST] o WITH (NOLOCK)
            ON o.BAGNO = u.TrackingNo
        LEFT JOIN [DATA_CENTER].[dbo].[MAINORDERINFO] m WITH (NOLOCK)
            ON m.MAINNUMBER = o.MAINNUMBER
        OUTER APPLY
        (
            SELECT CASE
                WHEN o.DESPATCHNO IS NULL THEN NULL
                WHEN LEN(CONVERT(nvarchar(50), o.DESPATCHNO)) >= 5 THEN CONVERT(nvarchar(50), o.DESPATCHNO)
                ELSE RIGHT(N'00000' + CONVERT(nvarchar(50), o.DESPATCHNO), 5)
            END AS CUST_CODE
        ) airCode
        LEFT JOIN #SysCust airCust
            ON airCust.CUST_TYPE = N'AIR'
           AND airCust.CUST_CODE = airCode.CUST_CODE
        OUTER APPLY
        (
            SELECT TOP (1)
                c.DATA_TYPE AS I_DATA_TYPE,
                c.SIGN_IN_TIME AS I_SIGN_IN_TIME,
                c.SIGN_OUT_TIME AS I_SIGN_OUT_TIME
            FROM [DATA_CENTER].[dbo].[CLEARANCE_INFO] c WITH (NOLOCK)
            WHERE c.MAIN_NUMBER = o.MAINNUMBER
              AND c.MERGE_NUMBER = o.TRACKINGUB
            ORDER BY c.SIGN_OUT_TIME DESC, c.SIGN_IN_TIME DESC
        ) ci

        UNION

        SELECT
            u.TrackingNo,
            o.TRANS_TAXPAYMENT,
            m.DELIVERYDATE AS ETA,
            ci.I_DATA_TYPE,
            o.DESPATCHNO AS DESPATCH_NAME,
            airCust.CUST_NAME AS CUSTOMER,
            o.MAINNUMBER,
            o.BAGNO AS BL_NO,
            o.TRACKINGUB AS JETF_SERIAL,
            ci.I_SIGN_IN_TIME,
            ci.I_SIGN_OUT_TIME,
            [jetf].[dbo].[GetTRANS_NAME](o.CLEARANCEWAREHOUSING) AS TRANS_NAME,
            o.DELIVERYNO,
            o.RECIPIENT AS IMPORTER,
            o.RECPHONE AS IM_PHONENO,
            o.RECADDRESS AS IM_ADD,
            o.FIELD_X,
            o.ORDER_NO,
            o.EXPRESS_NO
        FROM #UploadTracking u
        INNER JOIN [DATA_CENTER].[dbo].[ORIGINALLIST] o WITH (NOLOCK)
            ON o.TRACKINGUB = u.TrackingNo
        LEFT JOIN [DATA_CENTER].[dbo].[MAINORDERINFO] m WITH (NOLOCK)
            ON m.MAINNUMBER = o.MAINNUMBER
        OUTER APPLY
        (
            SELECT CASE
                WHEN o.DESPATCHNO IS NULL THEN NULL
                WHEN LEN(CONVERT(nvarchar(50), o.DESPATCHNO)) >= 5 THEN CONVERT(nvarchar(50), o.DESPATCHNO)
                ELSE RIGHT(N'00000' + CONVERT(nvarchar(50), o.DESPATCHNO), 5)
            END AS CUST_CODE
        ) airCode
        LEFT JOIN #SysCust airCust
            ON airCust.CUST_TYPE = N'AIR'
           AND airCust.CUST_CODE = airCode.CUST_CODE
        OUTER APPLY
        (
            SELECT TOP (1)
                c.DATA_TYPE AS I_DATA_TYPE,
                c.SIGN_IN_TIME AS I_SIGN_IN_TIME,
                c.SIGN_OUT_TIME AS I_SIGN_OUT_TIME
            FROM [DATA_CENTER].[dbo].[CLEARANCE_INFO] c WITH (NOLOCK)
            WHERE c.MAIN_NUMBER = o.MAINNUMBER
              AND c.MERGE_NUMBER = o.TRACKINGUB
            ORDER BY c.SIGN_OUT_TIME DESC, c.SIGN_IN_TIME DESC
        ) ci
    )
    SELECT *
    INTO #CargoData
    FROM SourceCargo
    OPTION (RECOMPILE);

    CREATE CLUSTERED INDEX [IX_CargoData_TrackingNo]
        ON #CargoData ([TrackingNo]);

    CREATE NONCLUSTERED INDEX [IX_CargoData_BL_NO]
        ON #CargoData ([BL_NO])
        INCLUDE ([TrackingNo], [JETF_SERIAL]);

    CREATE NONCLUSTERED INDEX [IX_CargoData_JETF_SERIAL]
        ON #CargoData ([JETF_SERIAL])
        INCLUDE ([TrackingNo], [BL_NO]);

    SELECT DISTINCT
        x.TrackingNo,
        x.CargoKey
    INTO #CargoKeys
    FROM
    (
        SELECT
            c.TrackingNo,
            c.BL_NO AS CargoKey
        FROM #CargoData c
        WHERE c.BL_NO IS NOT NULL

        UNION

        SELECT
            c.TrackingNo,
            c.JETF_SERIAL AS CargoKey
        FROM #CargoData c
        WHERE c.JETF_SERIAL IS NOT NULL
    ) x;

    CREATE NONCLUSTERED INDEX [IX_CargoKeys_CargoKey]
        ON #CargoKeys ([CargoKey])
        INCLUDE ([TrackingNo]);

    ;WITH Cte_PdtScanCargoUpload AS
    (
        SELECT
            x.TrackingNo,
            x.PdtTransName,
            x.CarNo,
            x.UploadTime,
            x.UploadOpe
        FROM
        (
            SELECT
                ROW_NUMBER() OVER
                (
                    PARTITION BY k.TrackingNo
                    ORDER BY a.UploadTime DESC
                ) AS RowNum,
                k.TrackingNo,
                b.TransName AS PdtTransName,
                a.CarNo,
                a.UploadTime,
                a.UploadOpe
            FROM [jetf].[dbo].[PdtScanCargoUpload] a WITH (NOLOCK)
            INNER JOIN #CargoKeys k
                ON a.Data = k.CargoKey
            INNER JOIN [jetf].[dbo].[PdtTrans] b WITH (NOLOCK)
                ON a.TransNo = b.TransNo
        ) x
        WHERE x.RowNum = 1
    ),
    Cte_CLEARANCE_INFO AS
    (
        SELECT
            b.BL_NO AS BAG_NUMBER,
            COUNT(DISTINCT c.SIGN_OUT_TIME) AS SignOutTimeCount
        FROM
        (
            SELECT DISTINCT c.BL_NO
            FROM #CargoData c
            WHERE c.BL_NO IS NOT NULL
        ) b
        INNER JOIN [DATA_CENTER].[dbo].[CLEARANCE_INFO] c WITH (NOLOCK)
            ON c.BAG_NUMBER = b.BL_NO
        GROUP BY b.BL_NO
    )
    SELECT
        a.TrackingNo,
        b.ETA,
        b.I_DATA_TYPE,
        b.DESPATCH_NAME,
        b.CUSTOMER,
        b.MAINNUMBER,
        b.BL_NO,
        b.JETF_SERIAL,
        b.I_SIGN_IN_TIME,
        b.I_SIGN_OUT_TIME,
        b.TRANS_NAME,
        ISNULL(d.TRANS_NAME, b.TRANS_TAXPAYMENT) AS TRANS_NAME_NEW,
        b.DELIVERYNO,
        b.IMPORTER,
        b.IM_PHONENO,
        b.IM_ADD,
        c.TRANS_MODIFY_TIME,
        c.TRANS_STATUS_DESC,
        b.FIELD_X,
        b.ORDER_NO,
        b.EXPRESS_NO,
        e.UploadTime AS CargoUploadTime,
        e.PdtTransName,
        f.SignOutTimeCount
    FROM #UploadRows a
    LEFT JOIN #CargoData b
        ON a.TrackingNo = b.TrackingNo
    LEFT JOIN [DATA_CENTER].[dbo].[CARGO_STATUS] c WITH (NOLOCK)
        ON c.TRANS_SERIAL = b.DELIVERYNO
    LEFT JOIN [jetf].[dbo].[customer_master] d WITH (NOLOCK)
        ON b.DESPATCH_NAME = d.CUST_ID
       AND b.TRANS_TAXPAYMENT = d.TRANS_NO
    LEFT JOIN Cte_PdtScanCargoUpload e
        ON a.TrackingNo = e.TrackingNo
    LEFT JOIN Cte_CLEARANCE_INFO f
        ON b.BL_NO = f.BAG_NUMBER
    OPTION (RECOMPILE);

    DROP TABLE IF EXISTS #CargoKeys;
    DROP TABLE IF EXISTS #CargoData;
    DROP TABLE IF EXISTS #SysCust;
    DROP TABLE IF EXISTS #UploadTracking;
    DROP TABLE IF EXISTS #UploadRows;
END;
GO

/*
建議確認或補強的索引：

1. [jetf].[dbo].[BatchSearchCargo2]
   (Upload_Ope, Upload_Time, TrackingNo)

2. [DATA_CENTER].[dbo].[SEA_ORDER_ORIGINAL]
   (BL_NO)
   (JETF_SERIAL)

3. [DATA_CENTER].[dbo].[ORIGINALLIST]
   (BAGNO)
   (TRACKINGUB)

4. [DATA_CENTER].[dbo].[MAINORDERINFO]
   (MAINNUMBER) INCLUDE (DELIVERYDATE)

5. [DATA_CENTER].[dbo].[SYS_CUST]
   (CUST_TYPE, CUST_CODE) INCLUDE (CUST_NAME)
   (CUST_TYPE, OLD_CODE) INCLUDE (CUST_NAME)

6. [DATA_CENTER].[dbo].[CLEARANCE_INFO]
   (MAIN_NUMBER, BAG_NUMBER) INCLUDE (DATA_TYPE, SIGN_IN_TIME, SIGN_OUT_TIME)
   (MAIN_NUMBER, MERGE_NUMBER) INCLUDE (DATA_TYPE, SIGN_IN_TIME, SIGN_OUT_TIME)

7. [jetf].[dbo].[PdtScanCargoUpload]
   (Data, UploadTime DESC) INCLUDE (TransNo, CarNo, UploadOpe)

8. [DATA_CENTER].[dbo].[CARGO_STATUS]
   (TRANS_SERIAL) INCLUDE (TRANS_MODIFY_TIME, TRANS_STATUS_DESC)
*/
