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
INSERT [dbo].[Employee] ([EmployeeID], [FullName], [Date_of_birth], [Phone], [Email], [Role_ID], [Department_ID], [HireDate], [Status], [Avatar]) VALUES (2, N'John Smith', CAST(N'1998-05-18T00:00:00.0000000' AS DateTime2), N'0987654321', N'john@company.com', 3, 4, CAST(N'2026-07-20T18:30:28.3333333' AS DateTime2), N'Active', NULL)
INSERT [dbo].[Employee] ([EmployeeID], [FullName], [Date_of_birth], [Phone], [Email], [Role_ID], [Department_ID], [HireDate], [Status], [Avatar]) VALUES (3, N'QA Manager', CAST(N'1997-02-01T00:00:00.0000000' AS DateTime2), N'0123456789', N'manager@company.com', 2, 4, CAST(N'2026-07-20T18:38:49.3200000' AS DateTime2), N'Active', NULL)
INSERT [dbo].[Employee] ([EmployeeID], [FullName], [Date_of_birth], [Phone], [Email], [Role_ID], [Department_ID], [HireDate], [Status], [Avatar]) VALUES (4, N'Dev Employee', CAST(N'1999-05-15T00:00:00.0000000' AS DateTime2), N'0912345678', N'dev.employee@company.com', 3, 2, CAST(N'2026-07-22T10:19:49.8333333' AS DateTime2), N'Active', NULL)
INSERT [dbo].[Employee] ([EmployeeID], [FullName], [Date_of_birth], [Phone], [Email], [Role_ID], [Department_ID], [HireDate], [Status], [Avatar]) VALUES (5, N'Dev Manager', CAST(N'1992-08-20T00:00:00.0000000' AS DateTime2), N'0987654321', N'dev.manager@company.com', 2, 2, CAST(N'2026-07-22T10:19:49.8333333' AS DateTime2), N'Active', NULL)
SET IDENTITY_INSERT [dbo].[Employee] OFF
GO
SET IDENTITY_INSERT [dbo].[Account] ON 

INSERT [dbo].[Account] ([Account_ID], [Username], [Password], [Role_ID], [Employee_ID]) VALUES (1, N'admin', N'$2a$11$rzlgNllEONzBWJNJvfNgGO3TggXA8.eHynPyVprwu5bvjtPPJgKOe', 1, 1)
INSERT [dbo].[Account] ([Account_ID], [Username], [Password], [Role_ID], [Employee_ID]) VALUES (2, N'user1', N'$2a$11$yVZIhuTOTqS55amAKgkAyeEWAueOWNR7QVOEuAC9eGVmuzPD7oB9K', 3, 2)
INSERT [dbo].[Account] ([Account_ID], [Username], [Password], [Role_ID], [Employee_ID]) VALUES (5, N'manager', N'$2a$11$JNZmXnz9g9OzDKMNqDVWs.L8nIwIG2tdOKXX6yHQHhKj4fOHu5muS', 2, 3)
INSERT [dbo].[Account] ([Account_ID], [Username], [Password], [Role_ID], [Employee_ID]) VALUES (6, N'user2', N'$2a$11$.3RQpJL5r1bKWRe.Kle6oOaiqCmJXh/vAG84X8lb6uffbYNBuDEE2', 3, 4)
INSERT [dbo].[Account] ([Account_ID], [Username], [Password], [Role_ID], [Employee_ID]) VALUES (7, N'manager2', N'$2a$11$JX.fTkZ/s5AerhFSUW4CEecLCgPlf0w/r9Orj2icRuRoreztMPWrS', 2, 5)
SET IDENTITY_INSERT [dbo].[Account] OFF
GO
SET IDENTITY_INSERT [dbo].[Contract] ON 

INSERT [dbo].[Contract] ([Contract_ID], [Employee_ID], [Role_ID], [ContractType], [StartDate], [EndDate], [Status], [BaseSalary]) VALUES (1, 1, 1, N'Full-Time', CAST(N'2026-07-20T18:31:34.4533333' AS DateTime2), NULL, N'Active', CAST(3000.00 AS Decimal(18, 2)))
INSERT [dbo].[Contract] ([Contract_ID], [Employee_ID], [Role_ID], [ContractType], [StartDate], [EndDate], [Status], [BaseSalary]) VALUES (2, 2, 3, N'Full-Time', CAST(N'2026-07-20T18:31:34.4533333' AS DateTime2), NULL, N'Active', CAST(1800.00 AS Decimal(18, 2)))
INSERT [dbo].[Contract] ([Contract_ID], [Employee_ID], [Role_ID], [ContractType], [StartDate], [EndDate], [Status], [BaseSalary]) VALUES (3, 4, 3, N'Full-time', CAST(N'2026-07-20T00:00:00.0000000' AS DateTime2), NULL, N'Active', CAST(2200.00 AS Decimal(18, 2)))
SET IDENTITY_INSERT [dbo].[Contract] OFF
GO
SET IDENTITY_INSERT [dbo].[Attendance] ON 

INSERT [dbo].[Attendance] ([Attendance_ID], [Employee_ID], [Check_in], [Check_out], [Status]) VALUES (1, 2, CAST(N'2026-07-20T10:31:39.2166667' AS DateTime2), CAST(N'2026-07-20T18:31:39.2166667' AS DateTime2), N'Present')
INSERT [dbo].[Attendance] ([Attendance_ID], [Employee_ID], [Check_in], [Check_out], [Status]) VALUES (2, 1, CAST(N'2026-07-20T07:59:39.0000000' AS DateTime2), CAST(N'2026-07-20T18:40:39.0000000' AS DateTime2), N'Present')
INSERT [dbo].[Attendance] ([Attendance_ID], [Employee_ID], [Check_in], [Check_out], [Status]) VALUES (3, 1, CAST(N'2026-07-21T08:02:39.0000000' AS DateTime2), CAST(N'2026-07-21T19:00:01.0000000' AS DateTime2), N'Present')
INSERT [dbo].[Attendance] ([Attendance_ID], [Employee_ID], [Check_in], [Check_out], [Status]) VALUES (4, 1, CAST(N'2026-07-22T08:23:39.0000000' AS DateTime2), CAST(N'2026-07-22T18:30:02.0000000' AS DateTime2), N'Present')
INSERT [dbo].[Attendance] ([Attendance_ID], [Employee_ID], [Check_in], [Check_out], [Status]) VALUES (5, 4, CAST(N'2026-07-23T06:50:11.0000000' AS DateTime2), CAST(N'2026-07-23T17:00:23.0000000' AS DateTime2), N'Present')
INSERT [dbo].[Attendance] ([Attendance_ID], [Employee_ID], [Check_in], [Check_out], [Status]) VALUES (6, 5, CAST(N'2026-07-23T22:50:47.8966667' AS DateTime2), CAST(N'2026-07-23T22:50:48.1866667' AS DateTime2), N'Late')
SET IDENTITY_INSERT [dbo].[Attendance] OFF
GO
SET IDENTITY_INSERT [dbo].[RequestForm] ON 

INSERT [dbo].[RequestForm] ([Request_ID], [Employee_ID], [RequestType], [Content], [SubmitDate], [Status], [StartDate], [EndDate]) VALUES (4, 3, N'Day Off', N'Reason: test 1', CAST(N'2026-07-21T23:00:12.6600000' AS DateTime2), N'Rejected', CAST(N'2026-07-26T00:00:00.0000000' AS DateTime2), CAST(N'2026-07-30T00:00:00.0000000' AS DateTime2))
INSERT [dbo].[RequestForm] ([Request_ID], [Employee_ID], [RequestType], [Content], [SubmitDate], [Status], [StartDate], [EndDate]) VALUES (5, 3, N'Resignation', N'Reason: Im done', CAST(N'2026-07-21T23:00:19.2466667' AS DateTime2), N'Approved', NULL, CAST(N'2026-07-31T00:00:00.0000000' AS DateTime2))
INSERT [dbo].[RequestForm] ([Request_ID], [Employee_ID], [RequestType], [Content], [SubmitDate], [Status], [StartDate], [EndDate]) VALUES (6, 3, N'Other', N'Subject: test 3
Description: wii', CAST(N'2026-07-21T23:00:27.3566667' AS DateTime2), N'Pending', NULL, NULL)
INSERT [dbo].[RequestForm] ([Request_ID], [Employee_ID], [RequestType], [Content], [SubmitDate], [Status], [StartDate], [EndDate]) VALUES (7, 2, N'support', N'the printer is out of ink, the coffee machine is broken, call someone to fix them.', CAST(N'2026-07-22T08:05:57.6766667' AS DateTime2), N'Pending', NULL, NULL)
INSERT [dbo].[RequestForm] ([Request_ID], [Employee_ID], [RequestType], [Content], [SubmitDate], [Status], [StartDate], [EndDate]) VALUES (8, 4, N'Day Off', N'Im tired boss', CAST(N'2026-07-22T10:29:58.0633333' AS DateTime2), N'Approved', CAST(N'2026-07-29T00:00:00.0000000' AS DateTime2), CAST(N'2026-07-30T00:00:00.0000000' AS DateTime2))
INSERT [dbo].[RequestForm] ([Request_ID], [Employee_ID], [RequestType], [Content], [SubmitDate], [Status], [StartDate], [EndDate]) VALUES (9, 4, N'Day Off', N'boss?', CAST(N'2026-07-22T23:38:56.3533333' AS DateTime2), N'Pending', CAST(N'2026-07-31T00:00:00.0000000' AS DateTime2), CAST(N'2026-08-01T00:00:00.0000000' AS DateTime2))
INSERT [dbo].[RequestForm] ([Request_ID], [Employee_ID], [RequestType], [Content], [SubmitDate], [Status], [StartDate], [EndDate]) VALUES (10, 4, N'Day Off', N'duplicate request', CAST(N'2026-07-22T23:39:25.1700000' AS DateTime2), N'Pending', CAST(N'2026-07-29T00:00:00.0000000' AS DateTime2), CAST(N'2026-08-30T00:00:00.0000000' AS DateTime2))
INSERT [dbo].[RequestForm] ([Request_ID], [Employee_ID], [RequestType], [Content], [SubmitDate], [Status], [StartDate], [EndDate]) VALUES (11, 5, N'Resignation', N'I don''t like dealing with problems', CAST(N'2026-07-22T23:40:00.3166667' AS DateTime2), N'Pending', NULL, CAST(N'2026-07-28T00:00:00.0000000' AS DateTime2))
INSERT [dbo].[RequestForm] ([Request_ID], [Employee_ID], [RequestType], [Content], [SubmitDate], [Status], [StartDate], [EndDate]) VALUES (12, 1, N'Layoff', N'Have anyone seen John? Tell him if he want to keep his job, show up tomorrow or the contract is done.', CAST(N'2026-07-23T13:35:59.7033333' AS DateTime2), N'Pending', NULL, NULL)
SET IDENTITY_INSERT [dbo].[RequestForm] OFF
GO
SET IDENTITY_INSERT [dbo].[EmployeeEvaluation] ON 

INSERT [dbo].[EmployeeEvaluation] ([Evaluation_ID], [Employee_ID], [EvaluationType], [BonusType], [Amount], [Bonus_Date]) VALUES (1, 2, N'Monthly', N'Performance Bonus', CAST(150.00 AS Decimal(18, 2)), CAST(N'2026-07-20T18:31:47.4800000' AS DateTime2))
INSERT [dbo].[EmployeeEvaluation] ([Evaluation_ID], [Employee_ID], [EvaluationType], [BonusType], [Amount], [Bonus_Date]) VALUES (2, 1, N'Monthly', N'Performance Bonus', CAST(200.00 AS Decimal(18, 2)), CAST(N'2026-07-20T19:33:42.0000000' AS DateTime2))
INSERT [dbo].[EmployeeEvaluation] ([Evaluation_ID], [Employee_ID], [EvaluationType], [BonusType], [Amount], [Bonus_Date]) VALUES (3, 4, N'Monthly', N'Performance Bonus', CAST(10.00 AS Decimal(18, 2)), CAST(N'2026-07-20T18:31:47.0000000' AS DateTime2))
SET IDENTITY_INSERT [dbo].[EmployeeEvaluation] OFF
GO
SET IDENTITY_INSERT [dbo].[Payroll] ON 

INSERT [dbo].[Payroll] ([Payroll_ID], [Employee_ID], [Evaluation_ID], [Month], [Year]) VALUES (1, 2, 1, 7, 2026)
INSERT [dbo].[Payroll] ([Payroll_ID], [Employee_ID], [Evaluation_ID], [Month], [Year]) VALUES (2, 1, 2, 7, 2026)
INSERT [dbo].[Payroll] ([Payroll_ID], [Employee_ID], [Evaluation_ID], [Month], [Year]) VALUES (3, 4, 1, 7, 2026)
SET IDENTITY_INSERT [dbo].[Payroll] OFF
GO
SET IDENTITY_INSERT [dbo].[SystemLog] ON 

INSERT [dbo].[SystemLog] ([Log_ID], [Account_ID], [Action], [TimeStamp]) VALUES (1, 1, N'System initialized.', CAST(N'2026-07-20T18:32:04.6100000' AS DateTime2))
SET IDENTITY_INSERT [dbo].[SystemLog] OFF
GO
