IF OBJECT_ID('dbo.SeaShenzhenOriginal', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SeaShenzhenOriginal]
    (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_SeaShenzhenOriginal] PRIMARY KEY,
        [DataDate] DATE NOT NULL,
        [TrackingNo] NVARCHAR(100) NULL,
        [BlNo] NVARCHAR(100) NULL,
        [OrderNo] NVARCHAR(100) NULL,
        [JetfSerial] NVARCHAR(100) NOT NULL,
        [TransTime] DATETIME NULL,
        [TransName] NVARCHAR(100) NULL,
        [Importer] NVARCHAR(100) NULL,
        [ImporterAddress] NVARCHAR(500) NULL,
        [ImporterPhone] NVARCHAR(100) NULL,
        [ItemName] NVARCHAR(500) NULL,
        [Cc] FLOAT NULL,
        [Quantity] INT NULL,
        [Gw] DECIMAL(18, 3) NULL,
        [Memo] NVARCHAR(500) NULL,
        [Claimant] NVARCHAR(100) NULL,
        [TaxPayment] NVARCHAR(100) NULL,
        [ModifiedUser] NVARCHAR(50) NULL,
        [ModifiedTime] DATETIME NULL,
        [CreatedUser] NVARCHAR(50) NULL,
        [CreatedTime] DATETIME NOT NULL
    );
END;

IF OBJECT_ID('dbo.SeaShenzhenOriginal', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.SeaShenzhenOriginal', 'DataDate') IS NULL
BEGIN
    ALTER TABLE [dbo].[SeaShenzhenOriginal]
    ADD [DataDate] DATE NOT NULL CONSTRAINT [DF_SeaShenzhenOriginal_DataDate] DEFAULT (CONVERT(date, GETDATE()));
END;

IF OBJECT_ID('dbo.SeaShenzhenOriginal', 'U') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1
       FROM sys.indexes
       WHERE name = 'UX_SeaShenzhenOriginal_JetfSerial'
         AND object_id = OBJECT_ID('dbo.SeaShenzhenOriginal')
   )
BEGIN
    CREATE UNIQUE INDEX [UX_SeaShenzhenOriginal_JetfSerial]
    ON [dbo].[SeaShenzhenOriginal] ([JetfSerial]);
END;
