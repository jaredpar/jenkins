
USE [jenkins]
GO

/****** Object: Table [dbo].[TestResultQueries] Script Date: 3/2/2016 7:12:25 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[TestResultQueries] (
    [Checksum]     VARCHAR (32)  NOT NULL,
    [QueryDate]    DATETIME      NOT NULL,
    [IsHit]        BIT           NOT NULL,
    [AssemblyName] VARCHAR (100) NULL,
    [IsJenkins]    BIT           NULL
);


USE [jenkins]
GO

/****** Object: Table [dbo].[TestResultStore] Script Date: 3/2/2016 7:12:58 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[TestResultStore] (
    [Checksum]             VARCHAR (32)  NOT NULL,
    [OutputStandardLength] INT           NOT NULL,
    [OutputErrorLength]    INT           NOT NULL,
    [ContentLength]        NCHAR (10)    NOT NULL,
    [AssemblyName]         VARCHAR (100) NULL,
    [ElapsedSeconds]      INT           NOT NULL
);


