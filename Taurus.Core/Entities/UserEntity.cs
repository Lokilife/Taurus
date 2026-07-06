namespace Taurus.Core.Entities;

public sealed class UserEntity
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string NormalizedUsername { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsEmailVerified { get; set; }
    public string? DisplayName { get; set; } // cn (Common Name)
    public string? GivenName { get; set; }
    public string? FamilyName { get; set; } // sn (surname)
    public string? PhoneNumber { get; set; }
    public bool IsPhoneVerified { get; set; }
    public bool Enabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    // EF mapped collection properties
    public ICollection<CredentialEntity> Credentials { get; set; } = null!;
    public ICollection<GroupEntity> Groups { get; set; } = null!;
    // public ICollection<UserAttributeEntity> Attributes { get; set; }
}
