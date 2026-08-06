CREATE TABLE [dbo].[KPI_Set] (
    [KPI_Set_ID]    INT            IDENTITY (1, 1) NOT NULL,
    [KPI_Set_Name]  NVARCHAR (200) NOT NULL,
    [Description]   NVARCHAR (500) NULL,
    [Department_ID] INT            NULL,
    [Is_Active]     BIT            CONSTRAINT [DF_KPISet_IsActive] DEFAULT ((1)) NOT NULL,
    [Created_At]    DATETIME2 (7)  CONSTRAINT [DF_KPISet_CreatedAt] DEFAULT (sysdatetime()) NOT NULL,
    PRIMARY KEY CLUSTERED ([KPI_Set_ID] ASC),
    CONSTRAINT [FK_KPISet_Department] FOREIGN KEY ([Department_ID]) REFERENCES [dbo].[Department] ([Department_ID])
);

