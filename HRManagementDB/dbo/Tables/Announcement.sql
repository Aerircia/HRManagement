CREATE TABLE [dbo].[Announcement] (
    [Announcement_ID] INT            IDENTITY (1, 1) NOT NULL,
    [Title]           NVARCHAR (200) NOT NULL,
    [Content]         NVARCHAR (MAX) NOT NULL,
    [PostedBy]        INT            NOT NULL,
    [PostedDate]      DATETIME2 (7)  NOT NULL,
    [IsActive]        BIT            DEFAULT ((1)) NOT NULL,
    PRIMARY KEY CLUSTERED ([Announcement_ID] ASC),
    CONSTRAINT [FK_Announcement_Account] FOREIGN KEY ([PostedBy]) REFERENCES [dbo].[Account] ([Account_ID])
);


GO
CREATE NONCLUSTERED INDEX [IX_Announcement_PostedDate]
    ON [dbo].[Announcement]([PostedDate] DESC);

