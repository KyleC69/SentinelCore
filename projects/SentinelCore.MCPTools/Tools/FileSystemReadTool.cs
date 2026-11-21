// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         FileSystemReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for inspecting file system metadata, attributes, ACLs, and file contents.
/// </summary>
[McpServerToolType]
public sealed class FileSystemReadTool
{

    [McpServerTool(Name = "sc_configuration_list_directory", ReadOnly = true, Destructive = false)]
    [Description("Lists the names of files and directories in the specified directory path.")]
    public async Task<ToolResult> FileSystemListDirectoryAsync([Description("The absolute directory path to list.")] string path, [Description("Optional search pattern, e.g. *.txt. Defaults to *.")] string? searchPattern = null, [Description("Maximum number of entries (files + directories) to return. Defaults to 50.")] int maxRecords = 50)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return ToolResult.Fail("Operation failed", "FileSystemReadTool");
            }

            DirectoryInfo dir = new(path);
            if (!dir.Exists)
            {
                return ToolResult.Fail($"Directory not found: {path}", "FileSystemReadTool");
            }

            string pattern = string.IsNullOrWhiteSpace(searchPattern) ? "*" : searchPattern;
            StringBuilder sb = new();
            int count = 0;

            sb.AppendLine("Directories:");
            foreach (DirectoryInfo subDir in dir.GetDirectories(pattern))
            {
                if (count >= maxRecords)
                {
                    break;
                }

                sb.AppendLine($"  {subDir.Name}");
                count++;
            }

            sb.AppendLine("Files:");
            foreach (FileInfo file in dir.GetFiles(pattern))
            {
                if (count >= maxRecords)
                {
                    break;
                }

                sb.AppendLine($"  {file.Name} ({file.Length} bytes)");
                count++;
            }

            var clean = Helpers.CleanValue(sb.ToString().Trim());
            return ToolResult.Ok(clean, "FileSystemReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "FileSystemReadTool");
        }
    }








    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_configuration_read_file_acl", ReadOnly = true, Destructive = false)]
    [Description("Reads the NTFS access control list (ACL) for a file or directory path.")]
    public async Task<ToolResult> FileSystemReadAclAsync([Description("The absolute file or directory path to inspect.")] string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return ToolResult.Fail("Operation failed", "FileSystemReadTool");
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
                return ToolResult.Fail($"Path not found: {path}", "FileSystemReadTool");
            }

            StringBuilder sb = new();
            sb.AppendLine($"Path={path}");
            sb.AppendLine($"Owner={security.GetOwner(typeof(NTAccount))}");
            sb.AppendLine($"Group={security.GetGroup(typeof(NTAccount))}");
            sb.AppendLine("AccessRules:");
            foreach (FileSystemAccessRule rule in security.GetAccessRules(true, true, typeof(NTAccount)))
                sb.AppendLine($"  Identity={rule.IdentityReference}, Rights={rule.FileSystemRights}, Type={rule.AccessControlType}, Inheritance={rule.InheritanceFlags}, Propagation={rule.PropagationFlags}");

            return ToolResult.Ok(sb.ToString(), "FileSystemReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "FileSystemReadTool");
        }
    }








    [McpServerTool(Name = "sc_configuration_read_file_content", ReadOnly = true, Destructive = false)]
    [Description("Reads the text content of a file. Supports optional line range selection and encoding detection. Binary files are rejected.")]
    public async Task<ToolResult> FileSystemReadContentAsync([Description("The absolute file path to read.")] string path, [Description("Optional 1-based starting line number. Defaults to 1.")] int startLine = 1, [Description("Optional number of lines to read from the starting line. Defaults to 0 (read all lines).")] int lineCount = 0, [Description("Optional encoding name (e.g. utf-8, ascii). Defaults to utf-8.")] string? encoding = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return ToolResult.Fail("Operation failed", "FileSystemReadTool");
            }

            FileInfo info = new(path);
            if (!info.Exists)
            {
                return ToolResult.Fail($"File not found: {path}", "FileSystemReadTool");
            }

            if (info.Attributes.HasFlag(FileAttributes.Directory))
            {
                return ToolResult.Fail($"Path is a directory, not a file: {path}", "FileSystemReadTool");
            }

            // Reject files that are likely binary by checking for null bytes in the first 8KB.
            byte[] probe = new byte[Math.Min(info.Length, 8192)];
            using (FileStream probeStream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                int bytesRead = probeStream.Read(probe, 0, probe.Length);
                for (int i = 0; i < bytesRead; i++)
                {
                    if (probe[i] == 0)
                    {
                        return ToolResult.Fail($"File appears to be binary and cannot be read as text: {path}", "FileSystemReadTool");
                    }
                }
            }

            // Resolve the encoding.
            Encoding fileEncoding = !string.IsNullOrWhiteSpace(encoding) ? Encoding.GetEncoding(encoding) : Encoding.UTF8;

            string[] lines = File.ReadAllLines(path, fileEncoding);

            if (lines.Length == 0)
            {
                return ToolResult.Ok("(file is empty)", "FileSystemReadTool");
            }

            // Validate and apply line range.
            int start = startLine < 1 ? 1 : startLine;
            int startIndex = start - 1;

            if (startIndex >= lines.Length)
            {
                return ToolResult.Fail($"startLine {startLine} exceeds total line count ({lines.Length}).", "FileSystemReadTool");
            }

            int count = lineCount > 0 ? lineCount : lines.Length - startIndex;
            int endIndex = Math.Min(startIndex + count, lines.Length);

            StringBuilder sb = new();
            int totalLines = endIndex - startIndex;
            sb.AppendLine($"File: {info.FullName}");
            sb.AppendLine($"Encoding: {fileEncoding.WebName}");
            sb.AppendLine($"Total lines: {lines.Length} | Showing: {start}–{endIndex} ({totalLines} lines)");
            sb.AppendLine(new string('-', 60));

            for (int i = startIndex; i < endIndex; i++)
            {
                sb.AppendLine($"{i + 1,6}  |  {lines[i]}");
            }

            return ToolResult.Ok(sb.ToString(), "FileSystemReadTool");
        }
        catch (ArgumentException ex) when (ex.Message.Contains("encoding", StringComparison.OrdinalIgnoreCase))
        {
            return ToolResult.Fail($"Unsupported encoding: {encoding}. Use a valid encoding name like 'utf-8' or 'ascii'.", "FileSystemReadTool");
        }
        catch (UnauthorizedAccessException)
        {
            return ToolResult.Fail($"Access denied reading file: {path}", "FileSystemReadTool");
        }
        catch (IOException)
        {
            return ToolResult.Fail($"I/O error reading file: {path}", "FileSystemReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "FileSystemReadTool");
        }
    }








    [McpServerTool(Name = "sc_configuration_read_file_metadata", ReadOnly = true, Destructive = false)]
    [Description("Reads metadata and attributes for a file or directory path.")]
    public async Task<ToolResult> FileSystemReadMetadataAsync([Description("The absolute file or directory path to inspect.")] string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return ToolResult.Fail("Operation failed", "FileSystemReadTool");
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

                    return ToolResult.Ok(dirResult, "File system metadata read.");
                }

                return ToolResult.Fail($"Path not found: {path}", "FileSystemReadTool");
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

            return ToolResult.Ok(fileResult, "File system metadata read.");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "FileSystemReadTool");
        }
    }
}
