CREATE TABLE [dbo].[KPI] (
    [KPI_ID]             INT             IDENTITY (1, 1) NOT NULL,
    [KPI_Name]           NVARCHAR (200)  NOT NULL,
    [Description]        NVARCHAR (1000) NULL,
    [KPI_Type]           NVARCHAR (30)   NOT NULL,
    [Measurement_Unit]   NVARCHAR (30)   NOT NULL,
    [Default_Target]     DECIMAL (18, 2) CONSTRAINT [DF_KPI_DefaultTarget] DEFAULT ((0)) NOT NULL,
    [Calculation_Method] NVARCHAR (30)   CONSTRAINT [DF_KPI_CalculationMethod] DEFAULT ('HigherIsBetter') NOT NULL,
    [Is_Active]          BIT             CONSTRAINT [DF_KPI_IsActive] DEFAULT ((1)) NOT NULL,
    [Created_At]         DATETIME2 (7)   CONSTRAINT [DF_KPI_CreatedAt] DEFAULT (sysdatetime()) NOT NULL,
    PRIMARY KEY CLUSTERED ([KPI_ID] ASC),
    CONSTRAINT [CK_KPI_CalculationMethod] CHECK ([Calculation_Method]='LowerIsBetter' OR [Calculation_Method]='HigherIsBetter'),
    CONSTRAINT [CK_KPI_DefaultTarget] CHECK ([Default_Target]>=(0)),
    CONSTRAINT [CK_KPI_MeasurementUnit] CHECK ([Measurement_Unit]='USD' OR [Measurement_Unit]='Percent' OR [Measurement_Unit]='Review' OR [Measurement_Unit]='Candidate' OR [Measurement_Unit]='Ticket' OR [Measurement_Unit]='Task'),
    CONSTRAINT [CK_KPI_Type] CHECK ([KPI_Type]='Financial' OR [KPI_Type]='Quality' OR [KPI_Type]='Productivity')
);

