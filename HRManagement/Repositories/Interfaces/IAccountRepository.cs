using HRManagement.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace HRManagement.Repositories.Interfaces
{
    public interface IAccountRepository
    {
        Account? GetByUsername(string username);

        Account? GetByEmployeeId(int employeeId);

        bool UsernameExists(string username);

        bool VerifyPassword(Account account, string password);

        Account? Login(string username, string password);
        bool ChangePassword(int accountId, string currentPassword, string newPassword);

        bool SetPassword(int accountId, string newPassword);
        /// <summary>
        /// Creates a login account for an employee, hashing the plaintext password.
        /// Returns the new Account_ID.
        /// </summary>
        int Insert(Account account, string plainTextPassword);

        /// <summary>
        /// Deletes the account belonging to an employee, if one exists.
        /// Used by the cascading employee-delete flow. No-op if none exists.
        /// </summary>
        void DeleteByEmployeeId(int employeeId);
    }
}
