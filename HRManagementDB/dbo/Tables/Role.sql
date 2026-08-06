CREATE TABLE [dbo].[Role] (
    [Role_ID]  INT           IDENTITY (1, 1) NOT NULL,
    [RoleName] NVARCHAR (50) NOT NULL,
    PRIMARY KEY CLUSTERED ([Role_ID] ASC),
    CONSTRAINT [UQ_Role_RoleName] UNIQUE NONCLUSTERED ([RoleName] ASC)
);

