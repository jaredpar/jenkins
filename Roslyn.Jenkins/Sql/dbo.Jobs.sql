CREATE TABLE [dbo].[Jobs] (
	[Id]	INT NOT NULL,
    [Kind]  NVARCHAR (50) NOT NULL,
    [Sha]   NCHAR (40)    NOT NULL,
    [State] TINYINT       NOT NULL,
    [Date]  DATETIME      NOT NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC, [Kind])
);
