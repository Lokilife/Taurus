namespace Taurus.Core.Credentials;

public sealed class CredentialEntity
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public CredentialType Type { get; set; } = CredentialType.Password;
    public int FormatVersion { get; set; } = 1;
    public string Material { get; set; } = string.Empty;
    public CredentialStatus Status { get; set; }
}
