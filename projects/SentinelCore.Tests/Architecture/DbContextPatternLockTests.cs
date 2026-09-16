// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         DbContextPatternLockTests.cs
// Author: Kyle L. Crowder
// Build Num:  091418



using System.IO;
using System.Text.RegularExpressions;

using SentinelCore.Tests.TestInfrastructure;

using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;




namespace SentinelCore.Tests.Architecture;





/// <summary>
///     Source-scanning enforcement of pattern-lock rule PL-7 (see
///     architecture/pattern-lock.md): every EF Core DbContext in SentinelCore must
///     be registered through a DbContext factory and consumed exclusively through
///     IDbContextFactory-created short-lived contexts. These tests fail the build
///     when any source file drifts from the approved factory pattern.
/// </summary>
[TestClass]
public sealed class DbContextPatternLockTests
{
    [TestMethod]
    public void All_DbContext_Registrations_Use_The_Approved_Factory_Pattern()
    {
        // Arrange — the legacy registration extension methods never register a
        // factory, so any match is drift from the approved pattern.
        Regex bannedRegistrationRegex = new("AddDbContext(?:Pool)?\\s*<", RegexOptions.Compiled);
        List<string> violations = [];

        // Act
        foreach ((string path, string source) in SourceTreeLocator.LoadSourceFiles())
        {
            foreach (Match match in bannedRegistrationRegex.Matches(source))
            {
                violations.Add($"{path}: '{match.Value}' — PL-7 requires factory registration");
            }
        }

        // Assert
        Assert.IsTrue(violations.Count == 0, "PL-7 drift: DbContext registration without a factory is prohibited.\n" + string.Join(Environment.NewLine, violations));
    }








    [TestMethod]
    public void Every_DbContext_Is_Registered_Through_A_DbContext_Factory()
    {
        // Arrange — every DbContext subclass declared in solution source must have
        // a matching factory registration in a production composition root.
        Regex contextDeclarationRegex = new("class\\s+(\\w+)\\s*:\\s*DbContext\\b", RegexOptions.Compiled);
        IReadOnlyList<(string Path, string Source)> sources = SourceTreeLocator.LoadSourceFiles();
        HashSet<string> declaredContexts = [];

        // Act — discover every DbContext subclass in authored source.
        foreach ((string _, string source) in sources)
        {
            foreach (Match match in contextDeclarationRegex.Matches(source))
            {
                declaredContexts.Add(match.Groups[1].Value);
            }
        }

        Assert.IsTrue(declaredContexts.Count > 0, "Expected to discover at least one DbContext in solution source.");

        List<(string Path, string Source)> productionSources = sources.Where(file => !IsTestProjectFile(file.Path)).ToList();

        List<string> violations = [];
        foreach (string contextType in declaredContexts)
        {
            string requiredRegistration = $"AddDbContextFactory<{contextType}>";
            bool registered = productionSources.Any(file => file.Source.Contains(requiredRegistration, StringComparison.Ordinal));

            if (!registered)
            {
                violations.Add($"{contextType} has no '{requiredRegistration}' registration in any production composition root");
            }
        }

        // Assert
        Assert.IsTrue(violations.Count == 0, "PL-7 drift: every DbContext must be registered through its factory.\n" + string.Join(Environment.NewLine, violations));
    }








    /// <summary>
    ///     Determines whether a source file is an approved exemption from the
    ///     consumer scan: build output, generated EF Core Power Tools partials,
    ///     the design-time factory, and scaffolded migrations.
    /// </summary>
    /// <param name="path">The candidate file path.</param>
    /// <returns><see langword="true" /> when the file is exempt from the scan.</returns>
    private static bool IsExemptFromConsumerScan(string path)
    {
        return SourceTreeLocator.IsBuildOutput(path) || path.Contains($"{Path.DirectorySeparatorChar}SentinelCore.CaseFlowEngine{Path.DirectorySeparatorChar}Persistence{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) || path.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);
    }








    /// <summary>
    ///     Determines whether a source file belongs to the test project, which
    ///     must not satisfy production registration requirements.
    /// </summary>
    /// <param name="path">The candidate file path.</param>
    /// <returns><see langword="true" /> when the file lives under the test project.</returns>
    private static bool IsTestProjectFile(string path)
    {
        return path.Contains($"{Path.DirectorySeparatorChar}SentinelCore.Tests{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);
    }








    [TestMethod]
    public void No_Consumer_Captures_A_DbContext_Directly()
    {
        // Arrange — a directly injected scoped context becomes a captive
        // dependency in the root-resolved host, so field capture and constructor
        // capture of a raw DbContext are both prohibited.
        Regex[] bannedCaptureRegexes =
        [
                new("private\\s+readonly\\s+(?:SentinelCoreDBContext|SentinelRAGDBContext)\\b", RegexOptions.Compiled),
                new("[(,]\\s*(?:SentinelCoreDBContext|SentinelRAGDBContext)\\s+\\w+\\s*[,)]", RegexOptions.Compiled)
        ];

        List<string> violations = [];

        // Act
        foreach ((string path, string source) in SourceTreeLocator.LoadSourceFiles())
        {
            if (IsExemptFromConsumerScan(path))
            {
                continue;
            }

            foreach (Regex bannedCaptureRegex in bannedCaptureRegexes)
            {
                foreach (Match match in bannedCaptureRegex.Matches(source))
                {
                    violations.Add($"{path}: '{match.Value}' — PL-7 requires IDbContextFactory injection");
                }
            }
        }

        // Assert
        Assert.IsTrue(violations.Count == 0, "PL-7 drift: consumers must not capture a DbContext directly.\n" + string.Join(Environment.NewLine, violations));
    }
}