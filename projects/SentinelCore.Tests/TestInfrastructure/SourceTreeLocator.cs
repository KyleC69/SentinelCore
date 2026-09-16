// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         SourceTreeLocator.cs
// Author: Kyle L. Crowder
// Build Num:  091418



using System.IO;
using System.Reflection;




namespace SentinelCore.Tests.TestInfrastructure;





/// <summary>
///     Locates the SentinelCore source tree from the running test assembly and
///     loads its C# source files so architecture conformance tests can scan the
///     real solution sources instead of compiled output.
/// </summary>
public static class SourceTreeLocator
{
    /// <summary>
    ///     Finds the solution's source root (the projects directory). Reads the
    ///     build-time SentinelCoreSourceRoot assembly metadata first — output
    ///     redirection via ArtifactsPath means the test assembly may live far
    ///     from the source tree — then falls back to walking upward from the
    ///     test assembly location until the SentinelCore solution file is found.
    /// </summary>
    /// <returns>The absolute path of the solution's projects directory.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when no solution file and matching projects directory can be found.
    /// </exception>
    public static string FindSourceRoot()
    {
        // Preferred — the build embeds the projects directory as assembly metadata,
        // which stays correct even when output is redirected via ArtifactsPath.
        AssemblyMetadataAttribute[] metadata = typeof(SourceTreeLocator).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().ToArray();

        foreach (AssemblyMetadataAttribute entry in metadata)
        {
            if (string.Equals(entry.Key, "SentinelCoreSourceRoot", StringComparison.Ordinal) && entry.Value is not null)
            {
                string configured = Path.GetFullPath(entry.Value);
                if (Directory.Exists(configured))
                {
                    return configured;
                }
            }
        }

        // Fallback — walk upward from the test assembly until the solution file is
        // found next to the projects directory (in-place output layouts).
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (directory.EnumerateFiles("*.slnx").Any())
            {
                string candidate = Path.Combine(directory.FullName, "SentinelCore", "projects");

                if (Directory.Exists(candidate))
                {
                    return candidate;
                }
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate the SentinelCore source root walking up from '" + AppContext.BaseDirectory + "'. Expected a SentinelCore.slnx with a 'SentinelCore/projects' directory beside it.");
    }








    /// <summary>
    ///     Determines whether a source file path points at build output rather
    ///     than authored source.
    /// </summary>
    /// <param name="path">The candidate file path.</param>
    /// <returns><see langword="true" /> when the path lives under a bin or obj folder.</returns>
    public static bool IsBuildOutput(string path)
    {
        return path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) || path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);
    }








    /// <summary>
    ///     Loads every C# source file under the solution's source root, excluding
    ///     build output folders. Callers apply any rule-specific exemptions
    ///     (migrations, generated partials) on top of this list.
    /// </summary>
    /// <returns>
    ///     An ordered list of file paths paired with their full source text, ready
    ///     for pattern scanning.
    /// </returns>
    public static IReadOnlyList<(string Path, string Source)> LoadSourceFiles()
    {
        string root = FindSourceRoot();

        return Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories).Where(path => !IsBuildOutput(path)).Select(path => (path, Source: File.ReadAllText(path))).ToList();
    }
}