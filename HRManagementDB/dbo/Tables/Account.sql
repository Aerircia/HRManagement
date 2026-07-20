CREATE TABLE [dbo].[Account] (
    [Account_ID]  INT            IDENTITY (1, 1) NOT NULL,
    [Username]    NVARCHAR (100) NOT NULL,
    [Password]    NVARCHAR (255) NOT NULL,
    [Role_ID]     INT            NOT NULL,
    [Employee_ID] INT            NOT NULL,
    PRIMARY KEY CLUSTERED ([Account_ID] ASC),
    CONSTRAINT [FK_Account_Employee] FOREIGN KEY ([Employee_ID]) REFERENCES [dbo].[Employee] ([EmployeeID]),
    CONSTRAINT [FK_Account_Role] FOREIGN KEY ([Role_ID]) REFERENCES [dbo].[Role] ([Role_ID]),
    CONSTRAINT [UQ_Account_Employee] UNIQUE NONCLUSTERED ([Employee_ID] ASC),
    CONSTRAINT [UQ_Account_Username] UNIQUE NONCLUSTERED ([Username] ASC)
);


GO
CREATE NONCLUSTERED INDEX [IX_Account_Role]
    ON [dbo].[Account]([Role_ID] ASC);

