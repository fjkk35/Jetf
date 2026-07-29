CREATE TABLE [jetf].[dbo].[ReconciliationIncludeTaxFormat]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [FormatName] NVARCHAR(50) NOT NULL,
    [CreatedDate] DATETIME2(0) NOT NULL,
    [UpdatedDate] DATETIME2(0) NOT NULL,
    CONSTRAINT [PK_ReconciliationIncludeTaxFormat]
        PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [UQ_ReconciliationIncludeTaxFormat_FormatName]
        UNIQUE ([FormatName])
);
GO

CREATE TABLE [jetf].[dbo].[ReconciliationIncludeTaxFormatColumn]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [FormatId] INT NOT NULL,
    [SortOrder] INT NOT NULL,
    [ColumnName] NVARCHAR(50) NOT NULL,
    [SourceType] INT NOT NULL,
    [FieldKey] NVARCHAR(50) NULL,
    [DefaultValue] NVARCHAR(200) NULL,
    CONSTRAINT [PK_ReconciliationIncludeTaxFormatColumn]
        PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_ReconciliationIncludeTaxFormatColumn_Format]
        FOREIGN KEY ([FormatId])
        REFERENCES [jetf].[dbo].[ReconciliationIncludeTaxFormat] ([Id])
        ON DELETE CASCADE
);
GO

CREATE INDEX [IX_ReconciliationIncludeTaxFormatColumn_FormatId_SortOrder]
    ON [jetf].[dbo].[ReconciliationIncludeTaxFormatColumn] ([FormatId], [SortOrder]);
GO

IF NOT EXISTS
(
    SELECT 1
    FROM [jetf].[dbo].[Authority]
    WHERE [Id] = 'ReconciliationIncludeTaxFormat'
)
BEGIN
    INSERT INTO [jetf].[dbo].[Authority] ([Id], [Text], [PartnerId], [Sort])
    VALUES ('ReconciliationIncludeTaxFormat', N'包稅客戶格式', 'Reconciliation', 4);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM [jetf].[dbo].[Authority]
    WHERE [Id] = 'ReconciliationIncludeTaxDownload'
)
BEGIN
    INSERT INTO [jetf].[dbo].[Authority] ([Id], [Text], [PartnerId], [Sort])
    VALUES ('ReconciliationIncludeTaxDownload', N'包稅客戶明細下載', 'Reconciliation', 5);
END;
GO
