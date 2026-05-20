using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace TaskHub.Server;

public class PayloadVerifier : IDisposable
{
    private readonly X509Certificate2? _certificate;
    private readonly bool _certificateConfigured;
    private readonly bool _verificationRequired;

    public PayloadVerifier(IConfiguration configuration)
    {
        var path = configuration["PayloadVerification:CertificatePath"];
        _certificateConfigured = !string.IsNullOrWhiteSpace(path);
        _verificationRequired = configuration.GetValue<bool>("PayloadVerification:Required");
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            _certificate = new X509Certificate2(File.ReadAllBytes(path));
        }
    }

    public bool Verify(JsonElement payload, string? signature)
    {
        if (_certificate == null)
        {
            return !_certificateConfigured && !_verificationRequired;
        }
        if (string.IsNullOrEmpty(signature))
        {
            return false;
        }

        byte[] data = JsonSerializer.SerializeToUtf8Bytes(payload);
        byte[] sig;
        try
        {
            sig = Convert.FromBase64String(signature);
        }
        catch (FormatException)
        {
            return false;
        }

        using var rsa = _certificate.GetRSAPublicKey();
        return rsa != null && rsa.VerifyData(data, sig, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }

    public void Dispose()
    {
        _certificate?.Dispose();
        GC.SuppressFinalize(this);
    }
}
