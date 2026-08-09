// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         FileSystemReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  080801



using System.ComponentModel;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Text.Json;




namespace SentinelCore.Tools;





/// <summary>
///     Read-only tool for inspecting file system metadata, attributes, and ACLs.
/// </summary>
public sealed class FileSystemReadTool : AITool
{
    public override string Description { get; } = "Read-only tool for inspecting file system metadata, attributes, and ACLs.";
    public override string Name { get; } = "File_System_Read";








    [Description("Lists the names of files and directories in the specified directory path.")]
    public Task<ToolResult> file_system_list_directory([Description("The absolute directory path to list.")] string path, [Description("Optional search pattern, e.g. *.txt. Defaults to *.")] string? searchPattern = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path)) return Task.FromResult(ToolResult.Fail("path is required."));

            DirectoryInfo dir = new(path);
            if (!dir.Exists) return Task.FromResult(ToolResult.Fail($"Directory not found: {path}"));

            string pattern = string.IsNullOrWhiteSpace(searchPattern) ? "*" : searchPattern;
            StringBuilder sb = new();
            sb.AppendLine("Directories:");
            foreach (DirectoryInfo subDir in dir.GetDirectories(pattern)) sb.AppendLine($"  {subDir.Name}");

            sb.AppendLine("Files:");
            foreach (FileInfo file in dir.GetFiles(pattern)) sb.AppendLine($"  {file.Name} ({file.Length} bytes)");

            return Task.FromResult(ToolResult.Ok(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"Directory listing failed: {ex.Message}"));
        }
    }








    [Description("Reads the NTFS access control list (ACL) for a file or directory path.")]
    public Task<ToolResult> file_system_read_acl([Description("The absolute file or directory path to inspect.")] string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return Task.FromResult(ToolResult.Fail("path is required."));
            }

            FileSystemSecurity security;
            bool isDirectory = Directory.Exists(path);
            if (isDirectory)
            {
                security = new DirectoryInfo(path).GetAccessControl();
            }
            else if (File.Exists(path))
            {
                security = new FileInfo(path).GetAccessControl();
            }
            else
            {
                return Task.FromResult(ToolResult.Fail($"Path not found: {path}"));
            }

            StringBuilder sb = new();
            sb.AppendLine($"Path={path}");
            sb.AppendLine($"Owner={security.GetOwner(typeof(NTAccount))}");
            sb.AppendLine($"Group={security.GetGroup(typeof(NTAccount))}");
            sb.AppendLine("AccessRules:");
            foreach (FileSystemAccessRule rule in security.GetAccessRules(true, true, typeof(NTAccount)))
                sb.AppendLine($"  Identity={rule.IdentityReference}, Rights={rule.FileSystemRights}, Type={rule.AccessControlType}, Inheritance={rule.InheritanceFlags}, Propagation={rule.PropagationFlags}");

            return Task.FromResult(ToolResult.Ok(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"ACL read failed: {ex.Message}"));
        }
    }








    [Description("Reads metadata and attributes for a file or directory path.")]
    public Task<ToolResult> file_system_read_metadata([Description("The absolute file or directory path to inspect.")] string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return Task.FromResult(ToolResult.Fail("path is required."));
            }

            FileInfo info = new(path);
            if (!info.Exists)
            {
                DirectoryInfo dirInfo = new(path);
                if (dirInfo.Exists)
                {
                    var dirResult = new
                    {
                            Path = dirInfo.FullName,
                            Exists = true,
                            IsDirectory = true,
                            Attributes = dirInfo.Attributes.ToString(),
                            dirInfo.CreationTimeUtc,
                            dirInfo.LastWriteTimeUtc,
                            dirInfo.LastAccessTimeUtc
                    };

                    return Task.FromResult(ToolResult.Ok(JsonSerializer.Serialize(dirResult, new JsonSerializerOptions { WriteIndented = true })));
                }

                return Task.FromResult(ToolResult.Fail($"Path not found: {path}"));
            }

            var fileResult = new
            {
                    Path = info.FullName,
                    Exists = true,
                    IsDirectory = false,
                    Attributes = info.Attributes.ToString(),
                    info.Length,
                    info.CreationTimeUtc,
                    info.LastWriteTimeUtc,
                    info.LastAccessTimeUtc
            };

            return Task.FromResult(ToolResult.Ok(JsonSerializer.Serialize(fileResult, new JsonSerializerOptions { WriteIndented = true })));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"File system metadata read failed: {ex.Message}"));
        }
    }
}