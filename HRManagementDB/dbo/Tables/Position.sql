CREATE TABLE [dbo].[Position] (
    [Position_ID]  INT             IDENTITY (1, 1) NOT NULL,
    [PositionName] NVARCHAR (50)   NOT NULL,
    [PayRate]      DECIMAL (18, 2) NOT NULL,
    PRIMARY KEY CLUSTERED ([Position_ID] ASC),
    CONSTRAINT [CK_Position_PayRate] CHECK ([PayRate]>=(0)),
    CONSTRAINT [UQ_Position_PositionName] UNIQUE NONCLUSTERED ([PositionName] ASC)
);

