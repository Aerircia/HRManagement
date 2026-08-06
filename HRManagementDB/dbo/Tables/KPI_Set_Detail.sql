CREATE TABLE [dbo].[KPI_Set_Detail] (
    [KPI_Set_Detail_ID] INT             IDENTITY (1, 1) NOT NULL,
    [KPI_Set_ID]        INT             NOT NULL,
    [KPI_ID]            INT             NOT NULL,
    [Target_Value]      DECIMAL (18, 2) NOT NULL,
    [Weight]            DECIMAL (5, 2)  NOT NULL,
    PRIMARY KEY CLUSTERED ([KPI_Set_Detail_ID] ASC),
    CONSTRAINT [CK_KPISetDetail_Target] CHECK ([Target_Value]>=(0)),
    CONSTRAINT [CK_KPISetDetail_Weight] CHECK ([Weight]>(0) AND [Weight]<=(100)),
    CONSTRAINT [FK_KPISetDetail_KPI] FOREIGN KEY ([KPI_ID]) REFERENCES [dbo].[KPI] ([KPI_ID]),
    CONSTRAINT [FK_KPISetDetail_Set] FOREIGN KEY ([KPI_Set_ID]) REFERENCES [dbo].[KPI_Set] ([KPI_Set_ID]),
    CONSTRAINT [UQ_KPISetDetail] UNIQUE NONCLUSTERED ([KPI_Set_ID] ASC, [KPI_ID] ASC)
);

