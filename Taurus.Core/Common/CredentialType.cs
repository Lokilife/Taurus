namespace Taurus.Core.Common;

public enum CredentialType : byte
{
    Password, // simple as a piece of cake - value is a password hash
    OTP, // one time password - value is a code that user should type
    TOTP, // time-based one time password - value is a secret
    // WebAuthn
}
