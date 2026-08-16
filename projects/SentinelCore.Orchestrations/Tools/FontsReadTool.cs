// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         FontsReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using System.ComponentModel;
using System.Drawing;
using System.Drawing.Text;
using System.Text;
using System.Text.Json;




namespace SentinelCore.Tools;





/// <summary>
///     Read-only tool for enumerating installed fonts using the GDI+ font APIs (InstalledFontCollection).
/// </summary>
public sealed class FontsReadTool : AITool
{
    public override string Description { get; } = "Read-only tool for enumerating installed fonts using GDI+ font APIs.";
    public override string Name { get; } = "Fonts_Read";








    [Description("Lists installed font families on the system.")]
    public Task<ToolResult> font_list([Description("Optional font name filter (partial match).")] string? filter = null)
    {
        try
        {
            using InstalledFontCollection fonts = new();
            var results = fonts.Families.Where(f => string.IsNullOrWhiteSpace(filter) || f.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0).Select(f => f.Name).OrderBy(n => n).ToList();

            string json = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
            return Task.FromResult(ToolResult.Ok(json));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"Font listing failed: {ex.Message}"));
        }
    }








    [Description("Reads detailed style information for a font family if available.")]
    public Task<ToolResult> font_read_styles([Description("The font family name.")] string fontName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fontName))
                return Task.FromResult(ToolResult.Fail("fontName is required."));

            using InstalledFontCollection fonts = new();
            FontFamily? family = fonts.Families.FirstOrDefault(f => f.Name.Equals(fontName, StringComparison.OrdinalIgnoreCase));
            if (family is null)
            {
                return Task.FromResult(ToolResult.Fail($"Font family not found: {fontName}"));
            }

            StringBuilder sb = new();
            sb.AppendLine($"Name={family.Name}");
            sb.AppendLine($"IsStyleAvailable(Regular)={family.IsStyleAvailable(FontStyle.Regular)}");
            sb.AppendLine($"IsStyleAvailable(Bold)={family.IsStyleAvailable(FontStyle.Bold)}");
            sb.AppendLine($"IsStyleAvailable(Italic)={family.IsStyleAvailable(FontStyle.Italic)}");
            sb.AppendLine($"IsStyleAvailable(Underline)={family.IsStyleAvailable(FontStyle.Underline)}");
            sb.AppendLine($"IsStyleAvailable(Strikeout)={family.IsStyleAvailable(FontStyle.Strikeout)}");
            sb.AppendLine($"LineSpacing={family.GetLineSpacing(FontStyle.Regular)}");
            sb.AppendLine($"EmHeight={family.GetEmHeight(FontStyle.Regular)}");

            return Task.FromResult(ToolResult.Ok(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"Font style read failed: {ex.Message}"));
        }
    }
}