namespace HRManagement.Services.Interfaces;

public interface ILogService
{
    void WriteLog(int accountId, string action);
}
