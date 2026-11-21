// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         CertificateStoreReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Runtime.Versioning;
using System.Security.Cryptography.X509Certificates;
using System.Text;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for querying Windows certificate stores.
/// </summary>
[McpServerToolType]
[SupportedOSPlatform("windows")]
public sealed class CertificateStoreReadTool
{

    [McpServerTool(Name = "sc_cryptography_list_certificates", ReadOnly = true, Destructive = false)]
    [Description("Lists certificates in the specified store and location.")]
    public async Task<ToolResult> CertificateListAsync([Description("The store name, e.g. My, Root, TrustedPublisher.")] string storeName, [Description("The store location: CurrentUser or LocalMachine. Defaults to LocalMachine.")] StoreLocation location = StoreLocation.LocalMachine, [Description("Maximum number of certificates to return. Defaults to 50.")] int maxRecords = 50)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(storeName))
            {
                return ToolResult.Fail("Operation failed", "CertificateStoreReadTool");
            }

            StringBuilder sb = new();
            int count = 0;
            using X509Store store = new(storeName, location);
            store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
            foreach (X509Certificate2 cert in store.Certificates)
            {
                if (count >= maxRecords)
                {
                    break;
                }

                sb.AppendLine($"Subject={cert.Subject}, Issuer={cert.Issuer}, Thumbprint={cert.Thumbprint}, NotAfter={cert.NotAfter}, FriendlyName={cert.FriendlyName}");
                count++;
            }

            return ToolResult.Ok(sb.ToString(), "CertificateStoreReadTool");
        }
        catch
        {

            return ToolResult.Fail("Operation failed", "CertificateStoreReadTool");

        }
    }








    [McpServerTool(Name = "sc_cryptography_read_certificate", ReadOnly = true, Destructive = false)]
    [Description("Reads details of a specific certificate by thumbprint.")]
    public async Task<ToolResult> CertificateReadAsync([Description("The certificate thumbprint.")] string thumbprint, [Description("The store name, e.g. My, Root.")] string storeName, [Description("The store location: CurrentUser or LocalMachine. Defaults to LocalMachine.")] StoreLocation location = StoreLocation.LocalMachine)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(thumbprint) || string.IsNullOrWhiteSpace(storeName))
            {
                return ToolResult.Fail("Operation failed", "CertificateStoreReadTool");
            }

            using X509Store store = new(storeName, location);
            store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
            X509Certificate2? cert = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false).FirstOrDefault();
            if (cert is null)
            {
                return ToolResult.Fail($"Certificate not found: {thumbprint} in {location}\\{storeName}", "CertificateStoreReadTool");
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

            return ToolResult.Ok(sb.ToString(), "CertificateStoreReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "CertificateStoreReadTool");
        }
    }
}
