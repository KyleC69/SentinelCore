// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         InputValidator.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.Text.RegularExpressions;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Shared input validation helpers for all MCP tools.
///     Provides consistent validation patterns for common parameter types.
/// </summary>
internal static class InputValidator
{

    /// <summary>
    ///     Validates that a maxRecords parameter is within acceptable bounds (1-500).
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The name of the parameter. Defaults to "maxRecords".</param>
    /// <returns>A <see cref="ToolResult" /> indicating failure if validation fails, or <c>null</c> if validation passes.</returns>
    internal static ToolResult? ValidateMaxRecords(int value, string paramName = "maxRecords")
    {
        if (value < 1 || value > 500)
        {
            return ToolResult.Fail($"{paramName} must be between 1 and 500. Received: {value}", "Input validation");
        }

        return null;
    }








    /// <summary>
    ///     Validates that a required integer parameter is greater than zero.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The name of the parameter (for error messages).</param>
    /// <returns>A <see cref="ToolResult" /> indicating failure if validation fails, or <c>null</c> if validation passes.</returns>
    internal static ToolResult? ValidatePositive(int value, string paramName)
    {
        if (value <= 0)
        {
            return ToolResult.Fail($"{paramName} must be a positive integer. Received: {value}", "Input validation");
        }

        return null;
    }








    /// <summary>
    ///     Validates that a registry hive abbreviation is one of the supported values.
    /// </summary>
    /// <param name="hive">The hive abbreviation to validate.</param>
    /// <returns>A <see cref="ToolResult" /> indicating failure if validation fails, or <c>null</c> if validation passes.</returns>
    internal static ToolResult? ValidateRegistryHive(string? hive)
    {
        ToolResult? requiredResult = ValidateRequired(hive, "hive");
        if (requiredResult is not null)
        {
            return requiredResult;
        }

        string upper = hive!.ToUpperInvariant();
        if (upper is not ("HKLM" or "HKCU" or "HKCR" or "HKU" or "HKCC"))
        {
            return ToolResult.Fail($"Unknown registry hive: {hive}. Supported hives: HKLM, HKCU, HKCR, HKU, HKCC.", "Input validation");
        }

        return null;
    }








    /// <summary>
    ///     Validates that a required string parameter is not null, empty, or whitespace.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The name of the parameter (for error messages).</param>
    /// <returns>A <see cref="ToolResult" /> indicating failure if validation fails, or <c>null</c> if validation passes.</returns>
    internal static ToolResult? ValidateRequired(string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return ToolResult.Fail($"{paramName} is required.", "Input validation");
        }

        return null;
    }








    /// <summary>
    ///     Sanitizes a string value for safe interpolation into a WQL WHERE clause.
    ///     Validates against injection patterns and ensures only safe characters are present.
    ///     Backslashes are allowed because WMI paths (e.g., BitLocker volume IDs) contain them.
    /// </summary>
    /// <param name="value">The value to sanitize.</param>
    /// <param name="paramName">The parameter name for error messages.</param>
    /// <returns>A failure result if the value contains dangerous patterns, or <c>null</c> if validation passes.</returns>
    internal static ToolResult? ValidateSanitizedWqlValue(string? value, string paramName)
    {
        ToolResult? requiredResult = ValidateRequired(value, paramName);
        if (requiredResult is not null)
        {
            return requiredResult;
        }

        // Block patterns that could break out of a WQL string literal or inject WQL
        // Note: backslashes are NOT blocked because WMI paths like \\?\Volume{GUID}\ contain them
        string[] dangerousPatterns = ["'", ";", "--", "/*", "*/", "\0"];
        foreach (string pattern in dangerousPatterns)
        {
            if (value!.Contains(pattern, StringComparison.Ordinal))
            {
                return ToolResult.Fail($"{paramName} contains a character that is not permitted in a WQL value: '{pattern}'.", "Input validation");
            }
        }

        // Only allow safe characters: alphanumeric, spaces, hyphens, underscores, dots, colons, braces, backslashes, question marks
        if (!Regex.IsMatch(value!, @"^[a-zA-Z0-9\s\-_./:\\?{}]+$"))
        {
            return ToolResult.Fail($"{paramName} contains characters that are not permitted. Only alphanumeric characters, spaces, hyphens, underscores, dots, colons, backslashes, question marks, and braces are allowed.", "Input validation");
        }

        return null;
    }








    /// <summary>
    ///     Validates that a WQL query string contains only safe characters and no injection patterns.
    ///     Only SELECT queries are permitted. No method calls, DML statements, or dangerous patterns.
    /// </summary>
    /// <param name="query">The WQL query to validate.</param>
    /// <returns>A <see cref="ToolResult" /> indicating failure if validation fails, or <c>null</c> if validation passes.</returns>
    internal static ToolResult? ValidateWqlQuery(string? query)
    {
        ToolResult? requiredResult = ValidateRequired(query, "query");
        if (requiredResult is not null)
        {
            return requiredResult;
        }

        string trimmed = query!.Trim();

        // Must start with SELECT (case-insensitive)
        if (!trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
        {
            return ToolResult.Fail("Only SELECT queries are permitted. The query must start with SELECT.", "Input validation");
        }

        // Block dangerous keywords
        string[] forbiddenKeywords = ["INSERT", "UPDATE", "DELETE", "DROP", "CREATE", "ALTER", "EXEC", "EXECUTE", "CALL", "ASSOCIATORS", "REFERENCES", "METHOD"];
        foreach (string keyword in forbiddenKeywords)
        {
            if (trimmed.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return ToolResult.Fail($"WQL query contains forbidden keyword: {keyword}. Only SELECT queries are permitted.", "Input validation");
            }
        }

        // Block method invocation patterns
        if (trimmed.Contains("()", StringComparison.OrdinalIgnoreCase))
        {
            return ToolResult.Fail("WQL query cannot contain method invocations. Only property-based SELECT queries are permitted.", "Input validation");
        }

        return null;
    }
}