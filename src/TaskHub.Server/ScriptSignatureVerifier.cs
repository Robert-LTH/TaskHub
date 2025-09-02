using System;
using System.IO;
using System.Management.Automation;

namespace TaskHub.Server;

public class ScriptSignatureVerifier
{
    // Returns true if the PowerShell Authenticode signature is valid.
    public bool IsAuthenticodeValid(string scriptContent)
    {
        var tmp = Path.Combine(Path.GetTempPath(), $"taskhub_script_{Guid.NewGuid():N}.ps1");
        try
        {
            File.WriteAllText(tmp, scriptContent);
            using var ps = PowerShell.Create();
            ps.AddScript("$args[0] | Get-AuthenticodeSignature | ForEach-Object { $_.Status -eq [System.Management.Automation.SignatureStatus]::Valid }")
              .AddArgument(tmp);
            var results = ps.Invoke();
            if (ps.HadErrors || results.Count == 0) return false;
            var first = results[0]?.BaseObject;
            return first is bool b && b;
        }
        catch
        {
            return false;
        }
        finally
        {
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
        }
    }
}

