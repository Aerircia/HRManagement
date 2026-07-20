CREATE TABLE [dbo].[Employee] (
    [EmployeeID]    INT            IDENTITY (1, 1) NOT NULL,
    [FullName]      NVARCHAR (200) NOT NULL,
    [Date_of_birth] DATETIME2 (7)  NOT NULL,
    [Phone]         NVARCHAR (20)  NULL,
    [Email]         NVARCHAR (200) NOT NULL,
    [Role_ID]       INT            NOT NULL,
    [Department_ID] INT            NOT NULL,
    [HireDate]      DATETIME2 (7)  NOT NULL,
    [Status]        NVARCHAR (50)  NOT NULL,
    [Avatar]        NVARCHAR (MAX) NULL,
    PRIMARY KEY CLUSTERED ([EmployeeID] ASC),
    CONSTRAINT [FK_Employee_Department] FOREIGN KEY ([Department_ID]) REFERENCES [dbo].[Department] ([Department_ID]),
    CONSTRAINT [FK_Employee_Role] FOREIGN KEY ([Role_ID]) REFERENCES [dbo].[Role] ([Role_ID]),
    CONSTRAINT [UQ_Employee_Email] UNIQUE NONCLUSTERED ([Email] ASC)
);


GO
CREATE NONCLUSTERED INDEX [IX_Employee_Role]
    ON [dbo].[Employee]([Role_ID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Employee_Department]
    ON [dbo].[Employee]([Department_ID] ASC);

