using Taurus.Core.Common;

namespace Taurus.Core.Entities;

public sealed class CredentialEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public CredentialType Type { get; set; }
    public string Value { get; set; } = string.Empty;
    public bool Valid { get; set; }
    public bool Required { get; set; } // when true the user is forced to pass this credential, used for MFA
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    // EF mapped navigation property
    public UserEntity User { get; set; } = null!;
}
