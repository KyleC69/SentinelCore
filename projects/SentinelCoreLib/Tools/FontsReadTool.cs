// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         FontsReadTool.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using System.ComponentModel;
using System.Drawing;
using System.Drawing.Text;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.AI;




namespace SentinelCoreLib.Tools;





/// <summary>
///     Read-only tool for enumerating installed fonts using the GDI+ font APIs (InstalledFontCollection).
/// </summary>
public sealed class FontsReadTool : AITool
{
    [Description("Lists installed font families on the system.")]
    public Task<ToolResult> font_list([Description("Optional font name filter (partial match).")] string? filter = null)
    {
        try
        {
            using InstalledFontCollection fonts = new();
            List<string> results = fonts.Families.Where(f => string.IsNullOrWhiteSpace(filter) || f.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0).Select(f => f.Name).OrderBy(n => n).ToList();

            string json = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
            return Task.FromResult(ToolResult.SuccessResult(json));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.FailureResult($"Font listing failed: {ex.Message}"));
        }
    }








    [Description("Reads detailed style information for a font family if available.")]
    public Task<ToolResult> font_read_styles([Description("The font family name.")] string fontName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fontName))
            {
                return Task.FromResult(ToolResult.FailureResult("fontName is required."));
            }

            using InstalledFontCollection fonts = new();
            FontFamily? family = fonts.Families.FirstOrDefault(f => f.Name.Equals(fontName, StringComparison.OrdinalIgnoreCase));
            if (family is null)
            {
                return Task.FromResult(ToolResult.FailureResult($"Font family not found: {fontName}"));
            }

            StringBuilder sb = new();
            sb.AppendLine($"Name={family.Name}");
            sb.AppendLine($"IsStyleAvailable(Regular)={family.IsStyleAvailable(System.Drawing.FontStyle.Regular)}");
            sb.AppendLine($"IsStyleAvailable(Bold)={family.IsStyleAvailable(System.Drawing.FontStyle.Bold)}");
            sb.AppendLine($"IsStyleAvailable(Italic)={family.IsStyleAvailable(System.Drawing.FontStyle.Italic)}");
            sb.AppendLine($"IsStyleAvailable(Underline)={family.IsStyleAvailable(System.Drawing.FontStyle.Underline)}");
            sb.AppendLine($"IsStyleAvailable(Strikeout)={family.IsStyleAvailable(System.Drawing.FontStyle.Strikeout)}");
            sb.AppendLine($"LineSpacing={family.GetLineSpacing(System.Drawing.FontStyle.Regular)}");
            sb.AppendLine($"EmHeight={family.GetEmHeight(System.Drawing.FontStyle.Regular)}");

            return Task.FromResult(ToolResult.SuccessResult(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.FailureResult($"Font style read failed: {ex.Message}"));
        }
    }
}