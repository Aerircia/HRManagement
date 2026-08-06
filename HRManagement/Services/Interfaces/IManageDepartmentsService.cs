using HRManagement.Models;

namespace HRManagement.Services.Interfaces;


public class DepartmentInput
{
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
}

public interface IManageDepartmentsService
{

    bool CurrentUserHasAccess();

    List<DepartmentRow> GetDepartments();

    DepartmentRow SaveDepartment(DepartmentInput input);


    void DeleteDepartment(int departmentId, string departmentName);
}
