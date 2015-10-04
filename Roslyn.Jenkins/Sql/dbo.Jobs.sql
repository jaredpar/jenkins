CREATE TABLE [dbo].[Jobs] (
	[Id]	INT NOT NULL,
    [Name]  NVARCHAR (50) NOT NULL,
    [Sha]   NCHAR (40)    NOT NULL,
    [State] TINYINT       NOT NULL,
    [Date]  DATETIME      NOT NULL,
	[Duration] INT       NOT NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC, [Name])
);
