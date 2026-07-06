using Taurus.Core.Interfaces;

namespace Taurus.Infrastructure.Persistence.Repositories;

public sealed class CredentialRepo : BaseRepo, ICredentialRepo
{
    public CredentialRepo(TaurusDbContext dbContext) : base(dbContext) { }

    public Task<CredentialEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _db.Credentials.FindAsync(new Guid[] { id }, cancellationToken).AsTask();
    }

    public Task<IEnumerable<CredentialEntity>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _db.Credentials.Where(c => c.UserId == userId).ToListAsync(cancellationToken).ContinueWith(t => (IEnumerable<CredentialEntity>)t.Result, cancellationToken);
    }

    public Task AddAsync(CredentialEntity credential, CancellationToken cancellationToken = default)
    {
        return _db.Credentials.AddAsync(credential, cancellationToken).AsTask();
    }

    public Task UpdateAsync(CredentialEntity credential, CancellationToken cancellationToken = default)
    {
        _db.Credentials.Update(credential);
        return _db.SaveChangesAsync(cancellationToken);
    }

    public Task DeleteAsync(CredentialEntity credential, CancellationToken cancellationToken = default)
    {
        _db.Credentials.Remove(credential);
        return _db.SaveChangesAsync(cancellationToken);
    }
}
