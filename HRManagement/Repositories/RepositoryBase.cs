using HRManagement.Data;

namespace HRManagement.Repositories;

public abstract class RepositoryBase
{
    protected readonly DatabaseContext Db = new();
}