namespace Taurus.Infrastructure.Persistence.Repositories;

public abstract class BaseRepo
{
    protected readonly TaurusDbContext _db;

    protected BaseRepo(TaurusDbContext dbContext)
    {
        _db = dbContext;
    }
}
