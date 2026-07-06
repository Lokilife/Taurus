using Taurus.Core.Interfaces;

namespace Taurus.Infrastructure.Persistence.Repositories;

public sealed class GroupRepo : BaseRepo, IGroupRepo
{
    public GroupRepo(TaurusDbContext dbContext) : base(dbContext) { }

    public Task<GroupEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _db.Groups.FindAsync(new Guid[] { id }, cancellationToken).AsTask();
    }

    public Task<GroupEntity?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return _db.Groups.FirstOrDefaultAsync(g => g.Name == name, cancellationToken);
    }

    public Task AddAsync(GroupEntity group, CancellationToken cancellationToken = default)
    {
        return _db.Groups.AddAsync(group, cancellationToken).AsTask();
    }

    public Task UpdateAsync(GroupEntity group, CancellationToken cancellationToken = default)
    {
        _db.Groups.Update(group);
        return _db.SaveChangesAsync(cancellationToken);
    }

    public Task DeleteAsync(GroupEntity group, CancellationToken cancellationToken = default)
    {
        _db.Groups.Remove(group);
        return _db.SaveChangesAsync(cancellationToken);
    }
}
