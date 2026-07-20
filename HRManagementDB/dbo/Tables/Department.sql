CREATE TABLE [dbo].[Department] (
    [Department_ID]  INT           IDENTITY (1, 1) NOT NULL,
    [DepartmentName] NVARCHAR (50) NOT NULL,
    PRIMARY KEY CLUSTERED ([Department_ID] ASC),
    CONSTRAINT [UQ_Department_Name] UNIQUE NONCLUSTERED ([DepartmentName] ASC)
);

