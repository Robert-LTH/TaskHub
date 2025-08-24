using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using TaskHub.Server;
using Xunit;

namespace TaskHub.Server.Tests;

public class PayloadVerifierTests
{
    [Fact]
    public void VerifyReturnsTrueForValidSignature()
    {
        using var rsa = RSA.Create(2048);
        var req = new CertificateRequest("CN=test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var cert = req.CreateSelfSigned(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddDays(1));
        var path = Path.GetTempFileName();
        File.WriteAllBytes(path, cert.Export(X509ContentType.Cert));

        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PayloadVerification:CertificatePath"] = path
        }).Build();

        var verifier = new PayloadVerifier(config);
        var payload = JsonDocument.Parse("{\"value\":1}").RootElement;
        var data = JsonSerializer.SerializeToUtf8Bytes(payload);
        var signature = Convert.ToBase64String(rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));

        try
        {
            Assert.True(verifier.Verify(payload, signature));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void VerifyReturnsFalseForInvalidSignature()
    {
        using var rsa = RSA.Create(2048);
        var req = new CertificateRequest("CN=test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var cert = req.CreateSelfSigned(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddDays(1));
        var path = Path.GetTempFileName();
        File.WriteAllBytes(path, cert.Export(X509ContentType.Cert));

        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PayloadVerification:CertificatePath"] = path
        }).Build();

        var verifier = new PayloadVerifier(config);
        var payload = JsonDocument.Parse("{\"value\":1}").RootElement;
        var fakeSig = Convert.ToBase64String(new byte[256]);

        try
        {
            Assert.False(verifier.Verify(payload, fakeSig));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
