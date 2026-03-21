-- 建立會員權限群組關聯表（多對多關係）
-- 執行前請先備份資料庫

-- 1. 建立 UserAuthorityGroup 關聯表
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='UserAuthorityGroup' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[UserAuthorityGroup]
    (
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [UserId] [nvarchar](50) NOT NULL,
        [AuthorityGroupId] [int] NOT NULL,
        [CreatedTime] [datetime] NOT NULL DEFAULT (getdate()),
        CONSTRAINT [PK_UserAuthorityGroup] PRIMARY KEY CLUSTERED 
        (
            [Id] ASC
        ),
        CONSTRAINT [FK_UserAuthorityGroup_User] FOREIGN KEY([UserId])
        REFERENCES [jetf].[dbo].[USER_MASTER] ([USER_ID]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserAuthorityGroup_AuthorityGroup] FOREIGN KEY([AuthorityGroupId])
        REFERENCES [dbo].[AuthorityGroup] ([Id]) ON DELETE CASCADE
    )
    
    -- 建立索引以提高查詢效能
    CREATE NONCLUSTERED INDEX [IX_UserAuthorityGroup_UserId] ON [dbo].[UserAuthorityGroup]
    (
        [UserId] ASC
    )
    
    CREATE NONCLUSTERED INDEX [IX_UserAuthorityGroup_AuthorityGroupId] ON [dbo].[UserAuthorityGroup]
    (
        [AuthorityGroupId] ASC
    )
    
    -- 建立複合唯一索引，避免重複關聯
    CREATE UNIQUE NONCLUSTERED INDEX [IX_UserAuthorityGroup_Unique] ON [dbo].[UserAuthorityGroup]
    (
        [UserId] ASC,
        [AuthorityGroupId] ASC
    )
    
    PRINT '成功建立 UserAuthorityGroup 關聯表'
END
ELSE
BEGIN
    PRINT 'UserAuthorityGroup 關聯表已存在'
END

-- 2. 遷移現有資料：將 USER_MASTER 表中的 AuthorityGroupId 資料遷移到關聯表
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'USER_MASTER' AND COLUMN_NAME = 'AuthorityGroupId')
BEGIN
    -- 將現有的權限群組關聯遷移到新表
    INSERT INTO [dbo].[UserAuthorityGroup] ([UserId], [AuthorityGroupId])
    SELECT [USER_ID], [AuthorityGroupId] 
    FROM [jetf].[dbo].[USER_MASTER] 
    WHERE [AuthorityGroupId] IS NOT NULL
    AND NOT EXISTS (
        SELECT 1 FROM [dbo].[UserAuthorityGroup] 
        WHERE [UserId] = [USER_MASTER].[USER_ID] 
        AND [AuthorityGroupId] = [USER_MASTER].[AuthorityGroupId]
    )
    
    PRINT '成功遷移現有權限群組資料到關聯表'
    
    -- 注意：如果要移除 USER_MASTER 的 AuthorityGroupId 欄位，請手動執行以下 SQL：
    -- ALTER TABLE [jetf].[dbo].[USER_MASTER] DROP COLUMN [AuthorityGroupId]
    -- 建議先測試確認新功能正常運作後再移除舊欄位
END

GO