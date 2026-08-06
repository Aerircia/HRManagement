using HRManagement.Models;
using System;
using System.Collections.Generic;

namespace HRManagement.Services.Interfaces
{
    /// <summary>
    /// Read model for one row of the employee list on Manage Profiles.
    /// Combines Employee with display-only lookups (department/role names).
    /// </summary>
    public class EmployeeProfileItemModel
    {
        public Employee Employee { get; set; } = null!;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string HireDateDisplay { get; set; } = string.Empty;
        public bool HasAccount { get; set; }
    }

    /// <summary>
    /// Input for creating or updating an employee profile from the Add/Edit form.
    /// </summary>
    public class EmployeeProfileInput
    {
        public int EmployeeId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public DateTime? HireDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public int DepartmentId { get; set; }

        // Account creation (new employees only, or an existing employee who
        // doesn't have an account yet). CreateAccount is ignored when
        // editing an existing employee that already has an account.
        public bool CreateAccount { get; set; }
        public string? AccountUsername { get; set; }
        public string? AccountPassword { get; set; }

        // Admin/manager-initiated password reset (edit mode only, for an
        // employee who already has an account). Independent of
        // CreateAccount/AccountPassword above, which only apply when
        // provisioning a brand new account. ResetPassword is ignored
        // unless the employee already has an account.
        public bool ResetPassword { get; set; }
        public string? NewPassword { get; set; }
    }

    public interface IManageProfilesService
    {
        bool CurrentUserHasAccess();

        /// <summary>
        /// True for Admin. False for Manager (who is scoped to their own
        /// department and cannot see/set the department filter or move
        /// employees to other departments).
        /// </summary>
        bool CurrentUserIsAdmin();

        List<IdNamePair> GetRoleOptions();

        List<IdNamePair> GetDepartments();

        List<EmployeeProfileItemModel> GetEmployees();

        /// <summary>
        /// Creates a new employee, optionally provisioning a login account
        /// for them at the same time. Returns the saved row for the list.
        /// </summary>
        EmployeeProfileItemModel AddEmployee(EmployeeProfileInput input);

        /// <summary>
        /// Updates an existing employee's profile fields and logs what changed.
        /// If input.ResetPassword is set and the employee already has an
        /// account, also resets their password (validated for complexity)
        /// and logs that separately.
        /// </summary>
        EmployeeProfileItemModel UpdateEmployee(EmployeeProfileInput input);

        /// <summary>
        /// Deletes the employee and all of their related data (attendance,
        /// evaluations, payroll, requests, contracts, account, logs).
        /// </summary>
        void DeleteEmployee(int employeeId);
    }

    public class IdNamePair
    {
        public IdNamePair(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public int Id { get; }
        public string Name { get; }
    }
}
