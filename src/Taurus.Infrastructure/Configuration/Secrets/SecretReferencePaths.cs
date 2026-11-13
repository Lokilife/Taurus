namespace Taurus.Infrastructure.Configuration.Secrets;

public static class SecretReferencePaths
{
    public const string Database = "database";
    public const string Csrf = "keys.csrf";
    public const string OpaqueSetup = "opaque.setup";
    public const string CertificatePassword = "http.certificatePassword";

    public static string RootPassword(int tenantIndex) =>
        $"tenants[{tenantIndex}].rootPassword";
}
