CREATE TABLE [dbo].[RequestForm] (
    [Request_ID]  INT            IDENTITY (1, 1) NOT NULL,
    [Employee_ID] INT            NOT NULL,
    [RequestType] NVARCHAR (100) NOT NULL,
    [Content]     NVARCHAR (MAX) NULL,
    [SubmitDate]  DATETIME2 (7)  NOT NULL,
    [Status]      NVARCHAR (50)  NOT NULL,
    [StartDate]   DATETIME2 (7)  NULL,
    [EndDate]     DATETIME2 (7)  NULL,
    PRIMARY KEY CLUSTERED ([Request_ID] ASC),
    CONSTRAINT [FK_Request_Employee] FOREIGN KEY ([Employee_ID]) REFERENCES [dbo].[Employee] ([EmployeeID])
);


GO
CREATE NONCLUSTERED INDEX [IX_Request_Employee]
    ON [dbo].[RequestForm]([Employee_ID] ASC);

