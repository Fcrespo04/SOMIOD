USE [somiod_db];
GO

IF OBJECT_ID('[dbo].[subscription]', 'U') IS NOT NULL 
    DROP TABLE [dbo].[subscription];

IF OBJECT_ID('[dbo].[content-instance]', 'U') IS NOT NULL 
    DROP TABLE [dbo].[content-instance];

IF OBJECT_ID('[dbo].[container]', 'U') IS NOT NULL 
    DROP TABLE [dbo].[container];

-- 3. Elimine a tabela application
IF OBJECT_ID('[dbo].[application]', 'U') IS NOT NULL 
    DROP TABLE [dbo].[application];

GO