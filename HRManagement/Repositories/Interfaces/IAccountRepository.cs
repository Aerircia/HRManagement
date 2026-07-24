using HRManagement.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace HRManagement.Repositories.Interfaces
{
    public interface IAccountRepository
    {
        Account? GetByUsername(string username);

        bool VerifyPassword(Account account, string password);

        Account? Login(string username, string password);
    }
}
