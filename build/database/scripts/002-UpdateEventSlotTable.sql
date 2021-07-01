Alter TABLE [dbo].[EventSlot] Add [IsGuestEvent] BIT NOT NULL CONSTRAINT DF_EventSlot_IsGuestEvent DEFAULT 0, [RequiresBTFLicense] BIT NOT NULL CONSTRAINT DF_EventSlot_RequiresBTFLicense DEFAULT 0
