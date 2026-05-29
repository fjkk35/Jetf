-- 取得批量貨況查詢明細表資料
-- 優化重點：
-- 1. 不再讀取舊的彙整來源表
-- 2. 先縮小本次上傳批次的 TrackingNo，再查詢大表
-- 3. 將原本 OR 條件拆成多段 UNION，讓 BL_NO/JETF_SERIAL/BAGNO/TRACKINGUB 的索引可被使用
-- 4. I_DATA_TYPE / I_SIGN_IN_TIME / I_SIGN_OUT_TIME 改由 [DATA_CENTER].[dbo].[CLEARANCE_INFO] 取得

CREATE OR ALTER PROCEDURE [dbo].[USP_GetBatchSearchCargo_test]
    @Upload_Ope nvarchar(10),
    @Upload_Time datetime,
    @EnableLog bit = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE
        @LogStart datetime2(7) = SYSDATETIME(),
        @LogStep datetime2(7) = SYSDATETIME(),
        @LogNow datetime2(7),
        @LogMessage nvarchar(4000),
        @RowCount int;

    IF @EnableLog = 1
    BEGIN
        SET @LogMessage = CONCAT(N'[USP_GetBatchSearchCargo_Optimized] Start. Upload_Ope=', @Upload_Ope, N', Upload_Time=', CONVERT(nvarchar(30), @Upload_Time, 121));
        RAISERROR(@LogMessage, 0, 1) WITH NOWAIT;
    END;

    DROP TABLE IF EXISTS #UploadRows;
    DROP TABLE IF EXISTS #UploadTracking;
    DROP TABLE IF EXISTS #SysCust;
    DROP TABLE IF EXISTS #TransName;
    DROP TABLE IF EXISTS #CargoDataRaw;
    DROP TABLE IF EXISTS #CargoData;
    DROP TABLE IF EXISTS #CargoClearance;
    DROP TABLE IF EXISTS #CargoKeys;
    DROP TABLE IF EXISTS #PdtScanCargoUpload;
    DROP TABLE IF EXISTS #ClearanceInfo;

    SELECT a.TrackingNo
    INTO #UploadRows
    FROM [jetf].[dbo].[BatchSearchCargo2] a WITH (NOLOCK)
    WHERE a.Upload_Ope = @Upload_Ope
      AND a.Upload_Time = @Upload_Time;

    SET @RowCount = @@ROWCOUNT;
    IF @EnableLog = 1
    BEGIN
        SET @LogNow = SYSDATETIME();
        SET @LogMessage = CONCAT(N'[USP_GetBatchSearchCargo_Optimized] Load #UploadRows elapsed_ms=', DATEDIFF(MILLISECOND, @LogStep, @LogNow), N', total_ms=', DATEDIFF(MILLISECOND, @LogStart, @LogNow), N', rows=', @RowCount);
        RAISERROR(@LogMessage, 0, 1) WITH NOWAIT;
        SET @LogStep = @LogNow;
    END;

    CREATE CLUSTERED INDEX [IX_UploadRows_TrackingNo]
        ON #UploadRows ([TrackingNo]);

    IF @EnableLog = 1
    BEGIN
        SET @LogNow = SYSDATETIME();
        SET @LogMessage = CONCAT(N'[USP_GetBatchSearchCargo_Optimized] Create IX_UploadRows_TrackingNo elapsed_ms=', DATEDIFF(MILLISECOND, @LogStep, @LogNow), N', total_ms=', DATEDIFF(MILLISECOND, @LogStart, @LogNow));
        RAISERROR(@LogMessage, 0, 1) WITH NOWAIT;
        SET @LogStep = @LogNow;
    END;

    SELECT DISTINCT
        a.TrackingNo,
        CONVERT(varchar(100), a.TrackingNo) AS TrackingNoKey
    INTO #UploadTracking
    FROM #UploadRows a
    WHERE a.TrackingNo IS NOT NULL;

    SET @RowCount = @@ROWCOUNT;
    IF @EnableLog = 1
    BEGIN
        SET @LogNow = SYSDATETIME();
        SET @LogMessage = CONCAT(N'[USP_GetBatchSearchCargo_Optimized] Load #UploadTracking elapsed_ms=', DATEDIFF(MILLISECOND, @LogStep, @LogNow), N', total_ms=', DATEDIFF(MILLISECOND, @LogStart, @LogNow), N', rows=', @RowCount);
        RAISERROR(@LogMessage, 0, 1) WITH NOWAIT;
        SET @LogStep = @LogNow;
    END;

    CREATE UNIQUE CLUSTERED INDEX [IX_UploadTracking_TrackingNo]
        ON #UploadTracking ([TrackingNo]);

    IF @EnableLog = 1
    BEGIN
        SET @LogNow = SYSDATETIME();
        SET @LogMessage = CONCAT(N'[USP_GetBatchSearchCargo_Optimized] Create IX_UploadTracking_TrackingNo elapsed_ms=', DATEDIFF(MILLISECOND, @LogStep, @LogNow), N', total_ms=', DATEDIFF(MILLISECOND, @LogStart, @LogNow));
        RAISERROR(@LogMessage, 0, 1) WITH NOWAIT;
        SET @LogStep = @LogNow;
    END;

    CREATE NONCLUSTERED INDEX [IX_UploadTracking_TrackingNoKey]
        ON #UploadTracking ([TrackingNoKey])
        INCLUDE ([TrackingNo]);

    IF @EnableLog = 1
    BEGIN
        SET @LogNow = SYSDATETIME();
        SET @LogMessage = CONCAT(N'[USP_GetBatchSearchCargo_Optimized] Create IX_UploadTracking_TrackingNoKey elapsed_ms=', DATEDIFF(MILLISECOND, @LogStep, @LogNow), N', total_ms=', DATEDIFF(MILLISECOND, @LogStart, @LogNow));
        RAISERROR(@LogMessage, 0, 1) WITH NOWAIT;
        SET @LogStep = @LogNow;
    END;

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

    SET @RowCount = @@ROWCOUNT;
    IF @EnableLog = 1
    BEGIN
        SET @LogNow = SYSDATETIME();
        SET @LogMessage = CONCAT(N'[USP_GetBatchSearchCargo_Optimized] Load #SysCust elapsed_ms=', DATEDIFF(MILLISECOND, @LogStep, @LogNow), N', total_ms=', DATEDIFF(MILLISECOND, @LogStart, @LogNow), N', rows=', @RowCount);
        RAISERROR(@LogMessage, 0, 1) WITH NOWAIT;
        SET @LogStep = @LogNow;
    END;

    CREATE UNIQUE CLUSTERED INDEX [IX_SysCust_CustType_CustCode]
        ON #SysCust ([CUST_TYPE], [CUST_CODE])
        WITH (IGNORE_DUP_KEY = ON);

    IF @EnableLog = 1
    BEGIN
        SET @LogNow = SYSDATETIME();
        SET @LogMessage = CONCAT(N'[USP_GetBatchSearchCargo_Optimized] Create IX_SysCust_CustType_CustCode elapsed_ms=', DATEDIFF(MILLISECOND, @LogStep, @LogNow), N', total_ms=', DATEDIFF(MILLISECOND, @LogStart, @LogNow));
        RAISERROR(@LogMessage, 0, 1) WITH NOWAIT;
        SET @LogStep = @LogNow;
    END;

   SELECT
    TRY_CONVERT(int, c.TRANS_NO) AS TRANS_NO,
    MAX(c.TRANS_NAME) AS TRANS_NAME
	INTO #TransName
	FROM [jetf].[dbo].[customer_master] c WITH (NOLOCK)
	WHERE TRY_CONVERT(int, c.TRANS_NO) IS NOT NULL
	GROUP BY TRY_CONVERT(int, c.TRANS_NO);

    SET @RowCount = @@ROWCOUNT;
    IF @EnableLog = 1
    BEGIN
        SET @LogNow = SYSDATETIME();
        SET @LogMessage = CONCAT(N'[USP_GetBatchSearchCargo_Optimized] Load #TransName elapsed_ms=', DATEDIFF(MILLISECOND, @LogStep, @LogNow), N', total_ms=', DATEDIFF(MILLISECOND, @LogStart, @LogNow), N', rows=', @RowCount);
        RAISERROR(@LogMessage, 0, 1) WITH NOWAIT;
        SET @LogStep = @LogNow;
    END;

    CREATE UNIQUE CLUSTERED INDEX [IX_TransName_TRANS_NO]
        ON #TransName ([TRANS_NO]);

    IF @EnableLog = 1
    BEGIN
        SET @LogNow = SYSDATETIME();
        SET @LogMessage = CONCAT(N'[USP_GetBatchSearchCargo_Optimized] Create IX_TransName_TRANS_NO elapsed_ms=', DATEDIFF(MILLISECOND, @LogStep, @LogNow), N', total_ms=', DATEDIFF(MILLISECOND, @LogStart, @LogNow));
        RAISERROR(@LogMessage, 0, 1) WITH NOWAIT;
        SET @LogStep = @LogNow;
    END;

    SELECT
        u.TrackingNo,
        N'SEA' AS SourceType,
        s.BL_NO AS ClearanceKey,
        s.TRANS_TAXPAYMENT,
        s.ETA,
        s.DESPATCH_NAME,
        seaCust.CUST_NAME AS CUSTOMER,
        s.MAINNUMBER,
        s.BL_NO,
        s.JETF_SERIAL,
        s.TRANS_NAME,
        s.JETF_SERIAL AS DELIVERYNO,
        s.IMPORTER,
        s.IM_PHONENO,
        s.IM_ADD,
        CAST(NULL AS nvarchar(100)) AS FIELD_X,
        CAST(NULL AS nvarchar(100)) AS ORDER_NO,
        CAST(NULL AS nvarchar(100)) AS EXPRESS_NO
    INTO #CargoDataRaw
    FROM #UploadTracking u
    INNER JOIN [DATA_CENTER].[dbo].[SEA_ORDER_ORIGINAL] s WITH (NOLOCK)
        ON s.BL_NO = u.TrackingNoKey
    LEFT JOIN #SysCust seaCust
        ON seaCust.CUST_TYPE = N'SEA'
       AND seaCust.CUST_CODE = s.DESPATCH_NAME
    OPTION (RECOMPILE);

    SET @RowCount = @@ROWCOUNT;
    IF @EnableLog = 1
    BEGIN
        SET @LogNow = SYSDATETIME();
        SET @LogMessage = CONCAT(N'[USP_GetBatchSearchCargo_Optimized] Load #CargoDataRaw SEA BL_NO elapsed_ms=', DATEDIFF(MILLISECOND, @LogStep, @LogNow), N', total_ms=', DATEDIFF(MILLISECOND, @LogStart, @LogNow), N', rows=', @RowCount);
        RAISERROR(@LogMessage, 0, 1) WITH NOWAIT;
        SET @LogStep = @LogNow;
    END;

    INSERT INTO #CargoDataRaw
    (
        TrackingNo,
        SourceType,
        ClearanceKey,
        TRANS_TAXPAYMENT,
        ETA,
        DESPATCH_NAME,
        CUSTOMER,
        MAINNUMBER,
        BL_NO,
        JETF_SERIAL,
        TRANS_NAME,
        DELIVERYNO,
        IMPORTER,
        IM_PHONENO,
        IM_ADD,
        FIELD_X,
        ORDER_NO,
        EXPRESS_NO
    )
    SELECT
        u.TrackingNo,
        N'SEA' AS SourceType,
        s.BL_NO AS ClearanceKey,
        s.TRANS_TAXPAYMENT,
        s.ETA,
        s.DESPATCH_NAME,
        seaCust.CUST_NAME AS CUSTOMER,
        s.MAINNUMBER,
        s.BL_NO,
        s.JETF_SERIAL,
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
        ON s.JETF_SERIAL = u.TrackingNoKey
    LEFT JOIN #SysCust seaCust
        ON seaCust.CUST_TYPE = N'SEA'
       AND seaCust.CUST_CODE = s.DESPATCH_NAME
    OPTION (RECOMPILE);

    SET @RowCount = @@ROWCOUNT;
    IF @EnableLog = 1
    BEGIN
        SET @LogNow = SYSDATETIME();
        SET @LogMessage = CONCAT(N'[USP_GetBatchSearchCargo_Optimized] Insert #CargoDataRaw SEA JETF_SERIAL elapsed_ms=', DATEDIFF(MILLISECOND, @LogStep, @LogNow), N', total_ms=', DATEDIFF(MILLISECOND, @LogStart, @LogNow), N', rows=', @RowCount);
        RAISERROR(@LogMessage, 0, 1) WITH NOWAIT;
        SET @LogStep = @LogNow;
    END;

    INSERT INTO #CargoDataRaw
    (
        TrackingNo,
        SourceType,
        ClearanceKey,
        TRANS_TAXPAYMENT,
        ETA,
        DESPATCH_NAME,
        CUSTOMER,
        MAINNUMBER,
        BL_NO,
        JETF_SERIAL,
        TRANS_NAME,
        DELIVERYNO,
        IMPORTER,
        IM_PHONENO,
        IM_ADD,
        FIELD_X,
        ORDER_NO,
        EXPRESS_NO
    )
    SELECT
        u.TrackingNo,
        N'AIR' AS SourceType,
        o.TRACKINGUB AS ClearanceKey,
        o.TRANS_TAXPAYMENT,
        m.DELIVERYDATE AS ETA,
        o.DESPATCHNO AS DESPATCH_NAME,
        airCust.CUST_NAME AS CUSTOMER,
        o.MAINNUMBER,
        o.BAGNO AS BL_NO,
        o.TRACKINGUB AS JETF_SERIAL,
        transName.TRANS_NAME,
        o.DELIVERYNO,
        o.RECIPIENT AS IMPORTER,
        o.RECPHONE AS IM_PHONENO,
        o.RECADDRESS AS IM_ADD,
        o.FIELD_X,
        o.ORDER_NO,
        o.EXPRESS_NO
    FROM #UploadTracking u
    INNER JOIN [DATA_CENTER].[dbo].[ORIGINALLIST] o WITH (NOLOCK)
        ON o.BAGNO = u.TrackingNoKey
    LEFT JOIN [DATA_CENTER].[dbo].[MAINORDERINFO] m WITH (NOLOCK)
        ON m.MAINNUMBER = o.MAINNUMBER
    LEFT JOIN #SysCust airCust
        ON airCust.CUST_TYPE = N'AIR'
       AND airCust.CUST_CODE = o.DESPATCHNO
    LEFT JOIN #TransName transName
        ON transName.TRANS_NO = o.CLEARANCEWAREHOUSING
    OPTION (RECOMPILE);

    SET @RowCount = @@ROWCOUNT;
    IF @EnableLog = 1
    BEGIN
        SET @LogNow = SYSDATETIME();
        SET @LogMessage = CONCAT(N'[USP_GetBatchSearchCargo_Optimized] Insert #CargoDataRaw AIR BAGNO elapsed_ms=', DATEDIFF(MILLISECOND, @LogStep, @LogNow), N', total_ms=', DATEDIFF(MILLISECOND, @LogStart, @LogNow), N', rows=', @RowCount);
        RAISERROR(@LogMessage, 0, 1) WITH NOWAIT;
        SET @LogStep = @LogNow;
    END;

    INSERT INTO #CargoDataRaw
    (
        TrackingNo,
        SourceType,
        ClearanceKey,
        TRANS_TAXPAYMENT,
        ETA,
        DESPATCH_NAME,
        CUSTOMER,
        MAINNUMBER,
        BL_NO,
        JETF_SERIAL,
        TRANS_NAME,
        DELIVERYNO,
        IMPORTER,
        IM_PHONENO,
        IM_ADD,
        FIELD_X,
        ORDER_NO,
        EXPRESS_NO
    )
    SELECT
        u.TrackingNo,
        N'AIR' AS SourceType,
        o.TRACKINGUB AS ClearanceKey,
        o.TRANS_TAXPAYMENT,
        m.DELIVERYDATE AS ETA,
        o.DESPATCHNO AS DESPATCH_NAME,
        airCust.CUST_NAME AS CUSTOMER,
        o.MAINNUMBER,
        o.BAGNO AS BL_NO,
        o.TRACKINGUB AS JETF_SERIAL,
        transName.TRANS_NAME,
        o.DELIVERYNO,
        o.RECIPIENT AS IMPORTER,
        o.RECPHONE AS IM_PHONENO,
        o.RECADDRESS AS IM_ADD,
        o.FIELD_X,
        o.ORDER_NO,
        o.EXPRESS_NO
    FROM #UploadTracking u
    INNER JOIN [DATA_CENTER].[dbo].[ORIGINALLIST] o WITH (NOLOCK)
        ON o.TRACKINGUB = u.TrackingNoKey
    LEFT JOIN [DATA_CENTER].[dbo].[MAINORDERINFO] m WITH (NOLOCK)
        ON m.MAINNUMBER = o.MAINNUMBER
    LEFT JOIN #SysCust airCust
        ON airCust.CUST_TYPE = N'AIR'
       AND airCust.CUST_CODE = o.DESPATCHNO
    LEFT JOIN #TransName transName
        ON transName.TRANS_NO = o.CLEARANCEWAREHOUSING
    OPTION (RECOMPILE);

    SET @RowCount = @@ROWCOUNT;
    IF @EnableLog = 1
    BEGIN
        SET @LogNow = SYSDATETIME();
        SET @LogMessage = CONCAT(N'[USP_GetBatchSearchCargo_Optimized] Insert #CargoDataRaw AIR TRACKINGUB elapsed_ms=', DATEDIFF(MILLISECOND, @LogStep, @LogNow), N', total_ms=', DATEDIFF(MILLISECOND, @LogStart, @LogNow), N', rows=', @RowCount);
        RAISERROR(@LogMessage, 0, 1) WITH NOWAIT;
        SET @LogStep = @LogNow;
    END;

    SELECT DISTINCT *
    INTO #CargoData
    FROM #CargoDataRaw
    OPTION (RECOMPILE);

    SET @RowCount = @@ROWCOUNT;
    IF @EnableLog = 1
    BEGIN
        SET @LogNow = SYSDATETIME();
        SET @LogMessage = CONCAT(N'[USP_GetBatchSearchCargo_Optimized] Load #CargoData from SourceCargo distinct elapsed_ms=', DATEDIFF(MILLISECOND, @LogStep, @LogNow), N', total_ms=', DATEDIFF(MILLISECOND, @LogStart, @LogNow), N', rows=', @RowCount);
        RAISERROR(@LogMessage, 0, 1) WITH NOWAIT;
        SET @LogStep = @LogNow;
    END;

    CREATE CLUSTERED INDEX [IX_CargoData_TrackingNo]
        ON #CargoData ([TrackingNo]);

    IF @EnableLog = 1
    BEGIN
        SET @LogNow = SYSDATETIME();
        SET @LogMessage = CONCAT(N'[USP_GetBatchSearchCargo_Optimized] Create IX_CargoData_TrackingNo elapsed_ms=', DATEDIFF(MILLISECOND, @LogStep, @LogNow), N', total_ms=', DATEDIFF(MILLISECOND, @LogStart, @LogNow));
        RAISERROR(@LogMessage, 0, 1) WITH NOWAIT;
        SET @LogStep = @LogNow;
    END;

    CREATE NONCLUSTERED INDEX [IX_CargoData_BL_NO]
        ON #CargoData ([BL_NO])
        INCLUDE ([TrackingNo], [JETF_SERIAL]);

    IF @EnableLog = 1
    BEGIN
        SET @LogNow = SYSDATETIME();
        SET @LogMessage = CONCAT(N'[USP_GetBatchSearchCargo_Optimized] Create IX_CargoData_BL_NO elapsed_ms=', DATEDIFF(MILLISECOND, @LogStep, @LogNow), N', total_ms=', DATEDIFF(MILLISECOND, @LogStart, @LogNow));
        RAISERROR(@LogMessage, 0, 1) WITH NOWAIT;
        SET @LogStep = @LogNow;
    END;

    CREATE NONCLUSTERED INDEX [IX_CargoData_JETF_SERIAL]
        ON #CargoData ([JETF_SERIAL])
        INCLUDE ([TrackingNo], [BL_NO]);

    IF @EnableLog = 1
    BEGIN
        SET @LogNow = SYSDATETIME();
        SET @LogMessage = CONCAT(N'[USP_GetBatchSearchCargo_Optimized] Create IX_CargoData_JETF_SERIAL elapsed_ms=', DATEDIFF(MILLISECOND, @LogStep, @LogNow), N', total_ms=', DATEDIFF(MILLISECOND, @LogStart, @LogNow));
        RAISERROR(@LogMessage, 0, 1) WITH NOWAIT;
        SET @LogStep = @LogNow;
    END;

    ;WITH CargoClearanceKeys AS
    (
        SELECT DISTINCT
            c.SourceType,
            c.MAINNUMBER,
            c.ClearanceKey
        FROM #CargoData c
        WHERE c.SourceType IN (N'SEA', N'AIR')
          AND c.MAINNUMBER IS NOT NULL
          AND c.ClearanceKey IS NOT NULL
    ),
    RankedCargoClearance AS
    (
        SELECT
            ROW_NUMBER() OVER
            (
                PARTITION BY k.SourceType, k.MAINNUMBER, k.ClearanceKey
                ORDER BY c.SIGN_OUT_TIME DESC, c.SIGN_IN_TIME DESC
            ) AS RowNum,
            k.SourceType,
            k.MAINNUMBER,
            k.ClearanceKey,
            c.DATA_TYPE AS I_DATA_TYPE,
            c.SIGN_IN_TIME AS I_SIGN_IN_TIME,
            c.SIGN_OUT_TIME AS I_SIGN_OUT_TIME
        FROM CargoClearanceKeys k
        INNER JOIN [DATA_CENTER].[dbo].[CLEARANCE_INFO] c WITH (NOLOCK)
            ON c.MAIN_NUMBER = k.MAINNUMBER
           AND c.BAG_NUMBER = k.ClearanceKey
        WHERE k.SourceType = N'SEA'

        UNION ALL

        SELECT
            ROW_NUMBER() OVER
            (
                PARTITION BY k.SourceType, k.MAINNUMBER, k.ClearanceKey
                ORDER BY c.SIGN_OUT_TIME DESC, c.SIGN_IN_TIME DESC
            ) AS RowNum,
            k.SourceType,
            k.MAINNUMBER,
            k.ClearanceKey,
            c.DATA_TYPE AS I_DATA_TYPE,
            c.SIGN_IN_TIME AS I_SIGN_IN_TIME,
            c.SIGN_OUT_TIME AS I_SIGN_OUT_TIME
        FROM CargoClearanceKeys k
        INNER JOIN [DATA_CENTER].[dbo].[CLEARANCE_INFO] c WITH (NOLOCK)
            ON c.MAIN_NUMBER = k.MAINNUMBER
           AND c.MERGE_NUMBER = k.ClearanceKey
        WHERE k.SourceType = N'AIR'
    )
    SELECT
        SourceType,
        MAINNUMBER,
        ClearanceKey,
        I_DATA_TYPE,
        I_SIGN_IN_TIME,
        I_SIGN_OUT_TIME
    INTO #CargoClearance
    FROM RankedCargoClearance
    WHERE RowNum = 1
    OPTION (RECOMPILE);

    SET @RowCount = @@ROWCOUNT;
    IF @EnableLog = 1
    BEGIN
        SET @LogNow = SYSDATETIME();
        SET @LogMessage = CONCAT(N'[USP_GetBatchSearchCargo_Optimized] Load #CargoClearance elapsed_ms=', DATEDIFF(MILLISECOND, @LogStep, @LogNow), N', total_ms=', DATEDIFF(MILLISECOND, @LogStart, @LogNow), N', rows=', @RowCount);
        RAISERROR(@LogMessage, 0, 1) WITH NOWAIT;
        SET @LogStep = @LogNow;
    END;

    CREATE UNIQUE CLUSTERED INDEX [IX_CargoClearance_Key]
        ON #CargoClearance ([SourceType], [MAINNUMBER], [ClearanceKey]);

    IF @EnableLog = 1
    BEGIN
        SET @LogNow = SYSDATETIME();
        SET @LogMessage = CONCAT(N'[USP_GetBatchSearchCargo_Optimized] Create IX_CargoClearance_Key elapsed_ms=', DATEDIFF(MILLISECOND, @LogStep, @LogNow), N', total_ms=', DATEDIFF(MILLISECOND, @LogStart, @LogNow));
        RAISERROR(@LogMessage, 0, 1) WITH NOWAIT;
        SET @LogStep = @LogNow;
    END;

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

    SET @RowCount = @@ROWCOUNT;
    IF @EnableLog = 1
    BEGIN
        SET @LogNow = SYSDATETIME();
        SET @LogMessage = CONCAT(N'[USP_GetBatchSearchCargo_Optimized] Load #CargoKeys elapsed_ms=', DATEDIFF(MILLISECOND, @LogStep, @LogNow), N', total_ms=', DATEDIFF(MILLISECOND, @LogStart, @LogNow), N', rows=', @RowCount);
        RAISERROR(@LogMessage, 0, 1) WITH NOWAIT;
        SET @LogStep = @LogNow;
    END;

    CREATE NONCLUSTERED INDEX [IX_CargoKeys_CargoKey]
        ON #CargoKeys ([CargoKey])
        INCLUDE ([TrackingNo]);

    IF @EnableLog = 1
    BEGIN
        SET @LogNow = SYSDATETIME();
        SET @LogMessage = CONCAT(N'[USP_GetBatchSearchCargo_Optimized] Create IX_CargoKeys_CargoKey elapsed_ms=', DATEDIFF(MILLISECOND, @LogStep, @LogNow), N', total_ms=', DATEDIFF(MILLISECOND, @LogStart, @LogNow));
        RAISERROR(@LogMessage, 0, 1) WITH NOWAIT;
        SET @LogStep = @LogNow;
    END;

    SELECT
        x.TrackingNo,
        x.PdtTransName,
        x.CarNo,
        x.UploadTime,
        x.UploadOpe
    INTO #PdtScanCargoUpload
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
    OPTION (RECOMPILE);

    SET @RowCount = @@ROWCOUNT;
    IF @EnableLog = 1
    BEGIN
        SET @LogNow = SYSDATETIME();
        SET @LogMessage = CONCAT(N'[USP_GetBatchSearchCargo_Optimized] Load #PdtScanCargoUpload elapsed_ms=', DATEDIFF(MILLISECOND, @LogStep, @LogNow), N', total_ms=', DATEDIFF(MILLISECOND, @LogStart, @LogNow), N', rows=', @RowCount);
        RAISERROR(@LogMessage, 0, 1) WITH NOWAIT;
        SET @LogStep = @LogNow;
    END;

    CREATE UNIQUE CLUSTERED INDEX [IX_PdtScanCargoUpload_TrackingNo]
        ON #PdtScanCargoUpload ([TrackingNo]);

    IF @EnableLog = 1
    BEGIN
        SET @LogNow = SYSDATETIME();
        SET @LogMessage = CONCAT(N'[USP_GetBatchSearchCargo_Optimized] Create IX_PdtScanCargoUpload_TrackingNo elapsed_ms=', DATEDIFF(MILLISECOND, @LogStep, @LogNow), N', total_ms=', DATEDIFF(MILLISECOND, @LogStart, @LogNow));
        RAISERROR(@LogMessage, 0, 1) WITH NOWAIT;
        SET @LogStep = @LogNow;
    END;

    SELECT
        b.BL_NO AS BAG_NUMBER,
        COUNT(DISTINCT c.SIGN_OUT_TIME) AS SignOutTimeCount
    INTO #ClearanceInfo
    FROM
    (
        SELECT DISTINCT c.BL_NO
        FROM #CargoData c
        WHERE c.BL_NO IS NOT NULL
    ) b
    INNER JOIN [DATA_CENTER].[dbo].[CLEARANCE_INFO] c WITH (NOLOCK)
        ON c.BAG_NUMBER = b.BL_NO
    GROUP BY b.BL_NO
    OPTION (RECOMPILE);

    SET @RowCount = @@ROWCOUNT;
    IF @EnableLog = 1
    BEGIN
        SET @LogNow = SYSDATETIME();
        SET @LogMessage = CONCAT(N'[USP_GetBatchSearchCargo_Optimized] Load #ClearanceInfo elapsed_ms=', DATEDIFF(MILLISECOND, @LogStep, @LogNow), N', total_ms=', DATEDIFF(MILLISECOND, @LogStart, @LogNow), N', rows=', @RowCount);
        RAISERROR(@LogMessage, 0, 1) WITH NOWAIT;
        SET @LogStep = @LogNow;
    END;

    CREATE UNIQUE CLUSTERED INDEX [IX_ClearanceInfo_BAG_NUMBER]
        ON #ClearanceInfo ([BAG_NUMBER]);

    IF @EnableLog = 1
    BEGIN
        SET @LogNow = SYSDATETIME();
        SET @LogMessage = CONCAT(N'[USP_GetBatchSearchCargo_Optimized] Create IX_ClearanceInfo_BAG_NUMBER elapsed_ms=', DATEDIFF(MILLISECOND, @LogStep, @LogNow), N', total_ms=', DATEDIFF(MILLISECOND, @LogStart, @LogNow));
        RAISERROR(@LogMessage, 0, 1) WITH NOWAIT;
        SET @LogStep = @LogNow;
    END;

    SELECT
        a.TrackingNo,
        b.ETA,
        cc.I_DATA_TYPE,
        b.DESPATCH_NAME,
        b.CUSTOMER,
        b.MAINNUMBER,
        b.BL_NO,
        b.JETF_SERIAL,
        cc.I_SIGN_IN_TIME,
        cc.I_SIGN_OUT_TIME,
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
    LEFT JOIN #CargoClearance cc
        ON cc.SourceType = b.SourceType
       AND cc.MAINNUMBER = b.MAINNUMBER
       AND cc.ClearanceKey = b.ClearanceKey
    LEFT JOIN [DATA_CENTER].[dbo].[CARGO_STATUS] c WITH (NOLOCK)
        ON c.TRANS_SERIAL = b.DELIVERYNO
    LEFT JOIN [jetf].[dbo].[customer_master] d WITH (NOLOCK)
        ON b.DESPATCH_NAME = d.CUST_ID
       AND b.TRANS_TAXPAYMENT = d.TRANS_NO
    LEFT JOIN #PdtScanCargoUpload e
        ON a.TrackingNo = e.TrackingNo
    LEFT JOIN #ClearanceInfo f
        ON b.BL_NO = f.BAG_NUMBER
    OPTION (RECOMPILE);

    SET @RowCount = @@ROWCOUNT;
    IF @EnableLog = 1
    BEGIN
        SET @LogNow = SYSDATETIME();
        SET @LogMessage = CONCAT(N'[USP_GetBatchSearchCargo_Optimized] Final select elapsed_ms=', DATEDIFF(MILLISECOND, @LogStep, @LogNow), N', total_ms=', DATEDIFF(MILLISECOND, @LogStart, @LogNow), N', rows=', @RowCount);
        RAISERROR(@LogMessage, 0, 1) WITH NOWAIT;
        SET @LogStep = @LogNow;
    END;

    DROP TABLE IF EXISTS #ClearanceInfo;
    DROP TABLE IF EXISTS #PdtScanCargoUpload;
    DROP TABLE IF EXISTS #CargoKeys;
    DROP TABLE IF EXISTS #CargoClearance;
    DROP TABLE IF EXISTS #CargoData;
    DROP TABLE IF EXISTS #CargoDataRaw;
    DROP TABLE IF EXISTS #TransName;
    DROP TABLE IF EXISTS #SysCust;
    DROP TABLE IF EXISTS #UploadTracking;
    DROP TABLE IF EXISTS #UploadRows;

    IF @EnableLog = 1
    BEGIN
        SET @LogNow = SYSDATETIME();
        SET @LogMessage = CONCAT(N'[USP_GetBatchSearchCargo_Optimized] Cleanup and end elapsed_ms=', DATEDIFF(MILLISECOND, @LogStep, @LogNow), N', total_ms=', DATEDIFF(MILLISECOND, @LogStart, @LogNow));
        RAISERROR(@LogMessage, 0, 1) WITH NOWAIT;
    END;
END;
GO

/*
建議確認或補強的索引：

1. [jetf].[dbo].[BatchSearchCargo2]
   (Upload_Ope, Upload_Time, TrackingNo)

2. [DATA_CENTER].[dbo].[SEA_ORDER_ORIGINAL]
   (BL_NO) INCLUDE (TRANS_TAXPAYMENT, ETA, DESPATCH_NAME, MAINNUMBER, JETF_SERIAL, TRANS_NAME, IMPORTER, IM_PHONENO, IM_ADD)
   (JETF_SERIAL) INCLUDE (TRANS_TAXPAYMENT, ETA, DESPATCH_NAME, MAINNUMBER, BL_NO, TRANS_NAME, IMPORTER, IM_PHONENO, IM_ADD)

3. [DATA_CENTER].[dbo].[ORIGINALLIST]
   (BAGNO) INCLUDE (TRANS_TAXPAYMENT, MAINNUMBER, TRACKINGUB, DESPATCHNO, CLEARANCEWAREHOUSING, DELIVERYNO, RECIPIENT, RECPHONE, RECADDRESS, FIELD_X, ORDER_NO, EXPRESS_NO)
   (TRACKINGUB) INCLUDE (TRANS_TAXPAYMENT, MAINNUMBER, BAGNO, DESPATCHNO, CLEARANCEWAREHOUSING, DELIVERYNO, RECIPIENT, RECPHONE, RECADDRESS, FIELD_X, ORDER_NO, EXPRESS_NO)

4. [DATA_CENTER].[dbo].[MAINORDERINFO]
   (MAINNUMBER) INCLUDE (DELIVERYDATE)

5. [DATA_CENTER].[dbo].[SYS_CUST]
   (CUST_TYPE, CUST_CODE) INCLUDE (CUST_NAME)
   (CUST_TYPE, OLD_CODE) INCLUDE (CUST_NAME)

6. [DATA_CENTER].[dbo].[CLEARANCE_INFO]
   (MAIN_NUMBER, BAG_NUMBER, SIGN_OUT_TIME DESC, SIGN_IN_TIME DESC) INCLUDE (DATA_TYPE)
   (MAIN_NUMBER, MERGE_NUMBER, SIGN_OUT_TIME DESC, SIGN_IN_TIME DESC) INCLUDE (DATA_TYPE)

7. [jetf].[dbo].[customer_master]
   (TRANS_NO) INCLUDE (TRANS_NAME)
   (CUST_ID, TRANS_NO) INCLUDE (TRANS_NAME)

8. [jetf].[dbo].[PdtScanCargoUpload]
   (Data, UploadTime DESC) INCLUDE (TransNo, CarNo, UploadOpe)

9. [DATA_CENTER].[dbo].[CARGO_STATUS]
   (TRANS_SERIAL) INCLUDE (TRANS_MODIFY_TIME, TRANS_STATUS_DESC)
*/
