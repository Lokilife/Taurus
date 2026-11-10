using System.Security.Cryptography;
using OPAQUE.Net;

namespace Taurus.Infrastructure.Authentication.Keys;

public static class KeyBootstrap
{
    private const int CsrfKeyBytes = 32;

    public static string GenerateOpaqueSetup()
    {
        if (!new OpaqueServer().CreateSetup(out var setup) || setup is null)
            throw new InvalidOperationException("OPAQUE setup generation failed");

        return setup;
    }

    public static string GenerateCsrfKey() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(CsrfKeyBytes));
}
