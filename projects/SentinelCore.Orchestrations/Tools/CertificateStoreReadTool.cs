// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         CertificateStoreReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using System.ComponentModel;
using System.Security.Cryptography.X509Certificates;
using System.Text;




namespace SentinelCore.Tools;





/// <summary>
///     Read-only tool for querying Windows certificate stores.
/// </summary>
public sealed class CertificateStoreReadTool : AITool
{
    public override string Description { get; } = "Read-only tool for querying Windows certificate stores.";
    public override string Name { get; } = "Certificate_Store_Read";








    [Description("Lists certificates in the specified store and location.")]
    public Task<ToolResult> certificate_list([Description("The store name, e.g. My, Root, TrustedPublisher.")] string storeName, [Description("The store location: CurrentUser or LocalMachine. Defaults to LocalMachine.")] StoreLocation location = StoreLocation.LocalMachine)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(storeName))
                return Task.FromResult(ToolResult.Fail("storeName is required."));

            StringBuilder sb = new();
            using X509Store store = new(storeName, location);
            store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
            foreach (X509Certificate2 cert in store.Certificates)
                sb.AppendLine($"Subject={cert.Subject}, Issuer={cert.Issuer}, Thumbprint={cert.Thumbprint}, NotAfter={cert.NotAfter}, FriendlyName={cert.FriendlyName}");

            return Task.FromResult(ToolResult.Ok(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"Certificate store listing failed: {ex.Message}"));
        }
    }








    [Description("Reads details of a specific certificate by thumbprint.")]
    public Task<ToolResult> certificate_read([Description("The certificate thumbprint.")] string thumbprint, [Description("The store name, e.g. My, Root.")] string storeName, [Description("The store location: CurrentUser or LocalMachine. Defaults to LocalMachine.")] StoreLocation location = StoreLocation.LocalMachine)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(thumbprint) || string.IsNullOrWhiteSpace(storeName))
            {
                return Task.FromResult(ToolResult.Fail("thumbprint and storeName are required."));
            }

            using X509Store store = new(storeName, location);
            store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
            X509Certificate2? cert = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false).FirstOrDefault();
            if (cert is null)
            {
                return Task.FromResult(ToolResult.Fail($"Certificate not found: {thumbprint} in {location}\\{storeName}"));
            }

            StringBuilder sb = new();
            sb.AppendLine($"Subject={cert.Subject}");
            sb.AppendLine($"Issuer={cert.Issuer}");
            sb.AppendLine($"Thumbprint={cert.Thumbprint}");
            sb.AppendLine($"NotBefore={cert.NotBefore}");
            sb.AppendLine($"NotAfter={cert.NotAfter}");
            sb.AppendLine($"HasPrivateKey={cert.HasPrivateKey}");
            sb.AppendLine($"FriendlyName={cert.FriendlyName}");
            sb.AppendLine($"SerialNumber={cert.SerialNumber}");
            sb.AppendLine($"SignatureAlgorithm={cert.SignatureAlgorithm.FriendlyName}");
            sb.AppendLine($"Version={cert.Version}");

            return Task.FromResult(ToolResult.Ok(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"Certificate read failed: {ex.Message}"));
        }
    }
}