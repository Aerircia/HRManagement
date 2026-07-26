CREATE TABLE [dbo].[EmployeeEvaluation] (
    [Evaluation_ID]  INT             IDENTITY (1, 1) NOT NULL,
    [Employee_ID]    INT             NOT NULL,
    [EvaluationType] NVARCHAR (100)  NULL,
    [BonusType]      NVARCHAR (100)  NULL,
    [Amount]         DECIMAL (18, 2) NOT NULL,
    [Bonus_Date]     DATETIME2 (7)   NOT NULL,
    [Comment]        NVARCHAR (500)  NULL,
    PRIMARY KEY CLUSTERED ([Evaluation_ID] ASC),
    CONSTRAINT [CK_Evaluation_Amount] CHECK ([Amount]>=(0)),
    CONSTRAINT [FK_Evaluation_Employee] FOREIGN KEY ([Employee_ID]) REFERENCES [dbo].[Employee] ([EmployeeID])
);




GO
CREATE NONCLUSTERED INDEX [IX_Evaluation_Employee]
    ON [dbo].[EmployeeEvaluation]([Employee_ID] ASC);

