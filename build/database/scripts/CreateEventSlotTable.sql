CREATE TABLE [dbo].[EventSlot](
	[Id] int IDENTITY(1,1) NOT NULL,
	[EventPageId] INT NOT NULL,
	[EventTypeId] INT NOT NULL,
	[Date] datetime NOT NULL,
	[Cost] decimal NOT NULL,
	[MaxParticipants] int NOT NULL,
	[Distances] nvarchar(500) NULL,
	[IndemnityWaiverDocumentLink] nvarchar(500) NULL,	
	[CovidDocumentLink] nvarchar(500) NULL	
 CONSTRAINT [PK_EventSlot] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO