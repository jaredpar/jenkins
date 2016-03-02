CREATE TABLE [dbo].[TestResultStore] (
    [Checksum]             VARCHAR (32) NOT NULL,
	[AssemblyName] VARCHAR(100),
    [OutputStandardLength] INT          NOT NULL,
    [OutputErrorLength]    INT          NOT NULL,
    [ContentLength]        NCHAR (10)   NULL,
    PRIMARY KEY CLUSTERED ([Checksum] ASC)
);

