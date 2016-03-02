CREATE TABLE [dbo].[TestResultQueries]
(
	[Checksum] VARCHAR(32) NOT NULL PRIMARY KEY, 
    [HitCount] INT NOT NULL,
    [MissCount] INT NOT NULL, 
    [LastHit] DATETIME NULL, 
    [LastMiss] DATETIME NULL
)
