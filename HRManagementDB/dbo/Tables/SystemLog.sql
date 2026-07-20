CREATE TABLE [dbo].[SystemLog] (
    [Log_ID]     INT            IDENTITY (1, 1) NOT NULL,
    [Account_ID] INT            NOT NULL,
    [Action]     NVARCHAR (MAX) NOT NULL,
    [TimeStamp]  DATETIME2 (7)  NOT NULL,
    PRIMARY KEY CLUSTERED ([Log_ID] ASC),
    CONSTRAINT [FK_SystemLog_Account] FOREIGN KEY ([Account_ID]) REFERENCES [dbo].[Account] ([Account_ID])
);


GO
CREATE NONCLUSTERED INDEX [IX_SystemLog_Account]
    ON [dbo].[SystemLog]([Account_ID] ASC);

