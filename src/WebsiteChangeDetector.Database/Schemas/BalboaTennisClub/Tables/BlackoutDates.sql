CREATE TABLE [BalboaTennisClub].[BlackoutDates]
(
	[BlackoutDateTime] DATETIME2 NOT NULL, 
    [LastModifiedDateTime] DATETIME2 NOT NULL DEFAULT (sysdatetime()) 
)
