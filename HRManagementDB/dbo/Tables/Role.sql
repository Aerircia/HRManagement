CREATE TABLE [dbo].[Role] (
    [Role_ID]  INT             IDENTITY (1, 1) NOT NULL,
    [RoleName] NVARCHAR (50)   NOT NULL,
    [PayRate]  DECIMAL (18, 2) NOT NULL,
    PRIMARY KEY CLUSTERED ([Role_ID] ASC),
    CONSTRAINT [CK_Role_PayRate] CHECK ([PayRate]>=(0)),
    CONSTRAINT [UQ_Role_RoleName] UNIQUE NONCLUSTERED ([RoleName] ASC)
);

