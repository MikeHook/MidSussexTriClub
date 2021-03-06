CREATE TABLE [dbo].[EventParticipant](
	[Id] int IDENTITY(1,1) NOT NULL,
	[EventSlotId] INT NOT NULL CONSTRAINT [FK_EventParticipant_EventSlotId] FOREIGN KEY([EventSlotId]) REFERENCES [dbo].[EventSlot] ([Id]),
	[MemberId] INT,
	[AmountPaid] decimal NOT NULL,
	[RaceDistance] nvarchar(80) NULL
 CONSTRAINT [PK_EventParticipant] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO