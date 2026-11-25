using System;
using AiEcoSys.Security;

namespace AiEcoSys.Desktop.Tests;

/// <summary>
/// Round-trip tests for the Windows Credential Manager backend used by the
/// Security view. The temporary credential is always deleted.
/// </summary>
public class CredentialStoreTests
{
    [Fact]
    public void Credential_Can_Be_Stored_Retrieved_And_Deleted()
    {
        string target = $"AiEcoSys:test-{Guid.NewGuid():N}";
        string secret = $"secret-{Guid.NewGuid():N}";

        try
        {
            SecurityService.StoreCredential(target, secret);
            Assert.Equal(secret, SecurityService.RetrieveCredential(target));

            SecurityService.DeleteCredential(target);
            Assert.Null(SecurityService.RetrieveCredential(target));
        }
        finally
        {
            try
            {
                SecurityService.DeleteCredential(target);
            }
            catch
            {
                // Cleanup is best-effort.
            }
        }
    }
}
