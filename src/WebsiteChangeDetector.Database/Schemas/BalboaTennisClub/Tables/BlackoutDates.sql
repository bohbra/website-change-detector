CREATE TABLE [BalboaTennisClub].[BlackoutDates]
(
	[BlackoutDateTime] DATETIME2 NOT NULL, 
    [Reservation] BIT NOT NULL DEFAULT 0, 
    [LastModifiedDateTime] DATETIME2 NOT NULL DEFAULT (sysdatetime())
)
