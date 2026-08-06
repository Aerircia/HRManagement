CREATE TABLE [dbo].[Contract] (
    [Contract_ID]  INT             IDENTITY (1, 1) NOT NULL,
    [Employee_ID]  INT             NOT NULL,
    [ContractType] NVARCHAR (100)  NOT NULL,
    [StartDate]    DATETIME2 (7)   NOT NULL,
    [EndDate]      DATETIME2 (7)   NULL,
    [Status]       NVARCHAR (50)   NOT NULL,
    [BaseSalary]   DECIMAL (18, 2) NOT NULL,
    [Position_ID]  INT             NOT NULL,
    PRIMARY KEY CLUSTERED ([Contract_ID] ASC),
    CONSTRAINT [CK_Contract_Salary] CHECK ([BaseSalary]>=(0)),
    CONSTRAINT [FK_Contract_Employee] FOREIGN KEY ([Employee_ID]) REFERENCES [dbo].[Employee] ([EmployeeID]),
    CONSTRAINT [FK_Contract_Position] FOREIGN KEY ([Position_ID]) REFERENCES [dbo].[Position] ([Position_ID])
);




GO
CREATE NONCLUSTERED INDEX [IX_Contract_Employee]
    ON [dbo].[Contract]([Employee_ID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Contract_Position]
    ON [dbo].[Contract]([Position_ID] ASC);

