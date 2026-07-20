CREATE TABLE [dbo].[Attendance] (
    [Attendance_ID] INT           IDENTITY (1, 1) NOT NULL,
    [Employee_ID]   INT           NOT NULL,
    [Check_in]      DATETIME2 (7) NULL,
    [Check_out]     DATETIME2 (7) NULL,
    [Status]        NVARCHAR (50) NOT NULL,
    PRIMARY KEY CLUSTERED ([Attendance_ID] ASC),
    CONSTRAINT [FK_Attendance_Employee] FOREIGN KEY ([Employee_ID]) REFERENCES [dbo].[Employee] ([EmployeeID])
);


GO
CREATE NONCLUSTERED INDEX [IX_Attendance_Employee]
    ON [dbo].[Attendance]([Employee_ID] ASC);

