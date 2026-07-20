CREATE TABLE [dbo].[Payroll] (
    [Payroll_ID]    INT IDENTITY (1, 1) NOT NULL,
    [Employee_ID]   INT NOT NULL,
    [Evaluation_ID] INT NULL,
    [Month]         INT NOT NULL,
    [Year]          INT NOT NULL,
    PRIMARY KEY CLUSTERED ([Payroll_ID] ASC),
    CONSTRAINT [CK_Payroll_Month] CHECK ([Month]>=(1) AND [Month]<=(12)),
    CONSTRAINT [CK_Payroll_Year] CHECK ([Year]>=(2000)),
    CONSTRAINT [FK_Payroll_Employee] FOREIGN KEY ([Employee_ID]) REFERENCES [dbo].[Employee] ([EmployeeID]),
    CONSTRAINT [FK_Payroll_Evaluation] FOREIGN KEY ([Evaluation_ID]) REFERENCES [dbo].[EmployeeEvaluation] ([Evaluation_ID])
);


GO
CREATE NONCLUSTERED INDEX [IX_Payroll_Employee]
    ON [dbo].[Payroll]([Employee_ID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Payroll_Evaluation]
    ON [dbo].[Payroll]([Evaluation_ID] ASC);

