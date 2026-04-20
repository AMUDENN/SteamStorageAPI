USE [SteamStorage]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Roles
-- =============================================
IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Title] = N'Admin')
    INSERT INTO [dbo].[Roles] ([Title]) VALUES (N'Admin');

IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Title] = N'User')
    INSERT INTO [dbo].[Roles] ([Title]) VALUES (N'User');
GO

-- =============================================
-- Pages
-- =============================================
IF NOT EXISTS (SELECT 1 FROM [dbo].[Pages] WHERE [Title] = N'Main')
    INSERT INTO [dbo].[Pages] ([Title]) VALUES (N'Main');

IF NOT EXISTS (SELECT 1 FROM [dbo].[Pages] WHERE [Title] = N'Actives')
    INSERT INTO [dbo].[Pages] ([Title]) VALUES (N'Actives');

IF NOT EXISTS (SELECT 1 FROM [dbo].[Pages] WHERE [Title] = N'Archive')
    INSERT INTO [dbo].[Pages] ([Title]) VALUES (N'Archive');

IF NOT EXISTS (SELECT 1 FROM [dbo].[Pages] WHERE [Title] = N'Inventory')
    INSERT INTO [dbo].[Pages] ([Title]) VALUES (N'Inventory');

IF NOT EXISTS (SELECT 1 FROM [dbo].[Pages] WHERE [Title] = N'Profile')
    INSERT INTO [dbo].[Pages] ([Title]) VALUES (N'Profile');
GO

-- =============================================
-- Currencies
-- =============================================
IF NOT EXISTS (SELECT 1 FROM [dbo].[Currencies] WHERE [SteamCurrencyID] = 1)
    INSERT INTO [dbo].[Currencies] ([SteamCurrencyID], [Title], [Mark], [CultureInfo], [IsBase])
    VALUES (1, N'Dollar', N'$', N'en-US', 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Currencies] WHERE [SteamCurrencyID] = 5)
    INSERT INTO [dbo].[Currencies] ([SteamCurrencyID], [Title], [Mark], [CultureInfo], [IsBase])
    VALUES (5, N'Ruble', N'руб.', N'ru-RU', 0);
GO

-- =============================================
-- Games
-- =============================================
IF NOT EXISTS (SELECT 1 FROM [dbo].[Games] WHERE [SteamGameID] = 570)
    INSERT INTO [dbo].[Games] ([SteamGameID], [Title], [GameIconUrl], [IsBase])
    VALUES (570, N'Dota 2', N'0bbb630d63262dd66d2fdd0f7d37e8661a410075', 0);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Games] WHERE [SteamGameID] = 730)
    INSERT INTO [dbo].[Games] ([SteamGameID], [Title], [GameIconUrl], [IsBase])
    VALUES (730, N'Counter-Strike 2', N'8dbc71957312bbd3baea65848b545be9eae2a355', 1);
GO
