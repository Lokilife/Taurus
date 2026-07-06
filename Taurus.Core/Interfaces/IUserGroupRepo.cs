using Taurus.Core.Entities;

namespace Taurus.Core.Interfaces;

public interface IUserGroupRepo
{
    Task<IEnumerable<GroupEntity>> GetGroupsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    // Task<IEnumerable<UserEntity>> GetUsersByGroupIdAsync(Guid groupId, CancellationToken cancellationToken = default); // paginated query probably?
    Task AddUserToGroupAsync(Guid userId, Guid groupId, CancellationToken cancellationToken = default);
    Task RemoveUserFromGroupAsync(Guid userId, Guid groupId, CancellationToken cancellationToken = default);
}
