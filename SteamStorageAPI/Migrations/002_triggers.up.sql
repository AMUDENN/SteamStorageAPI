USE [SteamStorage]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Trigger: update Skins.CurrentPrice on new dynamic record
-- =============================================
IF OBJECT_ID(N'[dbo].[UpdateSkinsCurrentPrice]', N'TR') IS NOT NULL
    DROP TRIGGER [dbo].[UpdateSkinsCurrentPrice];
GO

CREATE TRIGGER [dbo].[UpdateSkinsCurrentPrice]
ON [dbo].[SkinsDynamic]
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE s
    SET    s.[CurrentPrice] = i.[Price]
    FROM   [dbo].[Skins] s
    INNER JOIN inserted i ON s.[ID] = i.[SkinID];
END
GO

-- =============================================
-- Trigger: auto-unset previous base currency
-- when a new one is set to IsBase = 1
-- =============================================
IF OBJECT_ID(N'[dbo].[TR_Currencies_SingleBase]', N'TR') IS NOT NULL
    DROP TRIGGER [dbo].[TR_Currencies_SingleBase];
GO

CREATE TRIGGER [dbo].[TR_Currencies_SingleBase]
ON [dbo].[Currencies]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF UPDATE(IsBase) AND EXISTS (SELECT 1 FROM inserted WHERE IsBase = 1)
    BEGIN
        UPDATE [dbo].[Currencies]
        SET    [IsBase] = 0
        WHERE  [IsBase] = 1
          AND  [ID] NOT IN (SELECT [ID] FROM inserted WHERE [IsBase] = 1);
    END
END
GO

-- =============================================
-- Trigger: auto-unset previous base game
-- when a new one is set to IsBase = 1
-- =============================================
IF OBJECT_ID(N'[dbo].[TR_Games_SingleBase]', N'TR') IS NOT NULL
    DROP TRIGGER [dbo].[TR_Games_SingleBase];
GO

CREATE TRIGGER [dbo].[TR_Games_SingleBase]
ON [dbo].[Games]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF UPDATE(IsBase) AND EXISTS (SELECT 1 FROM inserted WHERE IsBase = 1)
    BEGIN
        UPDATE [dbo].[Games]
        SET    [IsBase] = 0
        WHERE  [IsBase] = 1
          AND  [ID] NOT IN (SELECT [ID] FROM inserted WHERE [IsBase] = 1);
    END
END
GO
