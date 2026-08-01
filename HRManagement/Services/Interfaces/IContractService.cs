using HRManagement.Models;
using System.Collections.Generic;

namespace HRManagement.Services.Interfaces
{
    public interface IContractService
    {
        bool CurrentUserHasAccess();

        List<IdNamePair> GetRoleOptions();
        List<IdNamePair> GetEmployees();
        List<ContractItemModel> GetContracts();

        ContractItemModel AddContract(ContractInput input);
        ContractItemModel UpdateContract(ContractInput input);
        void DeleteContract(int contractId);

        // Employee-facing "My Contract" lookup (used by ContractViewModel).
        ContractDetailModel? GetContract(int employeeId);
    }
}
