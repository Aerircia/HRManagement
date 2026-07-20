USE [HRManagement]
GO
SET IDENTITY_INSERT [dbo].[Role] ON 

INSERT [dbo].[Role] ([Role_ID], [RoleName], [PayRate]) VALUES (1, N'Admin', CAST(2.00 AS Decimal(18, 2)))
INSERT [dbo].[Role] ([Role_ID], [RoleName], [PayRate]) VALUES (2, N'Manager', CAST(1.50 AS Decimal(18, 2)))
INSERT [dbo].[Role] ([Role_ID], [RoleName], [PayRate]) VALUES (3, N'Employee', CAST(1.00 AS Decimal(18, 2)))
SET IDENTITY_INSERT [dbo].[Role] OFF
GO
SET IDENTITY_INSERT [dbo].[Department] ON 

INSERT [dbo].[Department] ([Department_ID], [DepartmentName]) VALUES (4, N'Design')
INSERT [dbo].[Department] ([Department_ID], [DepartmentName]) VALUES (2, N'Engineering')
INSERT [dbo].[Department] ([Department_ID], [DepartmentName]) VALUES (3, N'Finance')
INSERT [dbo].[Department] ([Department_ID], [DepartmentName]) VALUES (1, N'Human Resources')
INSERT [dbo].[Department] ([Department_ID], [DepartmentName]) VALUES (5, N'Marketing')
INSERT [dbo].[Department] ([Department_ID], [DepartmentName]) VALUES (6, N'Sales')
SET IDENTITY_INSERT [dbo].[Department] OFF
GO
SET IDENTITY_INSERT [dbo].[Employee] ON 

INSERT [dbo].[Employee] ([EmployeeID], [FullName], [Date_of_birth], [Phone], [Email], [Role_ID], [Department_ID], [HireDate], [Status], [Avatar]) VALUES (1, N'System Administrator', CAST(N'1995-01-01T00:00:00.0000000' AS DateTime2), N'0123456789', N'admin@company.com', 1, 1, CAST(N'2026-07-20T18:30:28.3333333' AS DateTime2), N'Active', NULL)
INSERT [dbo].[Employee] ([EmployeeID], [FullName], [Date_of_birth], [Phone], [Email], [Role_ID], [Department_ID], [HireDate], [Status], [Avatar]) VALUES (2, N'John Smith', CAST(N'1998-05-18T00:00:00.0000000' AS DateTime2), N'0987654321', N'john@company.com', 3, 2, CAST(N'2026-07-20T18:30:28.3333333' AS DateTime2), N'Active', NULL)
INSERT [dbo].[Employee] ([EmployeeID], [FullName], [Date_of_birth], [Phone], [Email], [Role_ID], [Department_ID], [HireDate], [Status], [Avatar]) VALUES (3, N'QA Manager', CAST(N'1997-02-01T00:00:00.0000000' AS DateTime2), N'0123456789', N'manager@company.com', 2, 2, CAST(N'2026-07-20T18:38:49.3200000' AS DateTime2), N'Active', NULL)
SET IDENTITY_INSERT [dbo].[Employee] OFF
GO
SET IDENTITY_INSERT [dbo].[Account] ON 

INSERT [dbo].[Account] ([Account_ID], [Username], [Password], [Role_ID], [Employee_ID]) VALUES (1, N'admin', N'$2a$11$rzlgNllEONzBWJNJvfNgGO3TggXA8.eHynPyVprwu5bvjtPPJgKOe', 1, 1)
INSERT [dbo].[Account] ([Account_ID], [Username], [Password], [Role_ID], [Employee_ID]) VALUES (2, N'user1', N'$2a$11$yVZIhuTOTqS55amAKgkAyeEWAueOWNR7QVOEuAC9eGVmuzPD7oB9K', 3, 2)
INSERT [dbo].[Account] ([Account_ID], [Username], [Password], [Role_ID], [Employee_ID]) VALUES (5, N'manager', N'$2a$11$JNZmXnz9g9OzDKMNqDVWs.L8nIwIG2tdOKXX6yHQHhKj4fOHu5muS', 2, 3)
SET IDENTITY_INSERT [dbo].[Account] OFF
GO
SET IDENTITY_INSERT [dbo].[Contract] ON 

INSERT [dbo].[Contract] ([Contract_ID], [Employee_ID], [Role_ID], [ContractType], [StartDate], [EndDate], [Status], [BaseSalary]) VALUES (1, 1, 1, N'Full-Time', CAST(N'2026-07-20T18:31:34.4533333' AS DateTime2), NULL, N'Active', CAST(3000.00 AS Decimal(18, 2)))
INSERT [dbo].[Contract] ([Contract_ID], [Employee_ID], [Role_ID], [ContractType], [StartDate], [EndDate], [Status], [BaseSalary]) VALUES (2, 2, 3, N'Full-Time', CAST(N'2026-07-20T18:31:34.4533333' AS DateTime2), NULL, N'Active', CAST(1800.00 AS Decimal(18, 2)))
SET IDENTITY_INSERT [dbo].[Contract] OFF
GO
SET IDENTITY_INSERT [dbo].[Attendance] ON 

INSERT [dbo].[Attendance] ([Attendance_ID], [Employee_ID], [Check_in], [Check_out], [Status]) VALUES (1, 2, CAST(N'2026-07-20T10:31:39.2166667' AS DateTime2), CAST(N'2026-07-20T18:31:39.2166667' AS DateTime2), N'Present')
SET IDENTITY_INSERT [dbo].[Attendance] OFF
GO
SET IDENTITY_INSERT [dbo].[RequestForm] ON 

INSERT [dbo].[RequestForm] ([Request_ID], [Employee_ID], [RequestType], [Content], [SubmitDate], [Status]) VALUES (1, 2, N'Leave', N'Annual leave for 2 days.', CAST(N'2026-07-20T18:32:01.4300000' AS DateTime2), N'Pending')
SET IDENTITY_INSERT [dbo].[RequestForm] OFF
GO
SET IDENTITY_INSERT [dbo].[EmployeeEvaluation] ON 

INSERT [dbo].[EmployeeEvaluation] ([Evaluation_ID], [Employee_ID], [EvaluationType], [BonusType], [Amount], [Bonus_Date]) VALUES (1, 2, N'Monthly', N'Performance Bonus', CAST(150.00 AS Decimal(18, 2)), CAST(N'2026-07-20T18:31:47.4800000' AS DateTime2))
SET IDENTITY_INSERT [dbo].[EmployeeEvaluation] OFF
GO
SET IDENTITY_INSERT [dbo].[Payroll] ON 

INSERT [dbo].[Payroll] ([Payroll_ID], [Employee_ID], [Evaluation_ID], [Month], [Year]) VALUES (1, 2, 1, 7, 2026)
SET IDENTITY_INSERT [dbo].[Payroll] OFF
GO
SET IDENTITY_INSERT [dbo].[SystemLog] ON 

INSERT [dbo].[SystemLog] ([Log_ID], [Account_ID], [Action], [TimeStamp]) VALUES (1, 1, N'System initialized.', CAST(N'2026-07-20T18:32:04.6100000' AS DateTime2))
SET IDENTITY_INSERT [dbo].[SystemLog] OFF
GO
