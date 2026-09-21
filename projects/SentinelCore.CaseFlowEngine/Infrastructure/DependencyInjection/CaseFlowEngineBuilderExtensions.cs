// Solution: SentinelCore
// Project:   SentinelCore.CaseFlowEngine
// File:         CaseFlowEngineBuilderExtensions.cs
// Author: Kyle L. Crowder
// Build Num:  091419



using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using SentinelCore.CaseFlowEngine.Cfe;
using SentinelCore.CaseFlowEngine.Infrastructure.Persistence;
using SentinelCore.Contracts.Abstractions;





namespace SentinelCore.CaseFlowEngine.Infrastructure.DependencyInjection;





/// <summary>
///     Extension methods for registering the Case Flow Engine and its internal services.
///     <para>
///         The Case Flow Engine (CFE) is the single owner of case lifecycle state.
///         This registration method wires both the public <see cref="ICaseFlowEngine" /> facade
///         and its internal persistence layer. External consumers should depend only on
///         <see cref="ICaseFlowEngine" /> — never on the internal repository.
///     </para>
///     <para>
///         CFE is an OPTIONAL module (pattern-lock PL-4/PL-5): <c>AddSentinelCore</c>
///         registers null-object defaults so the system runs without persistence.
///         Calling this method opts the host in to the real EF Core-backed engine,
///         overriding those defaults via <c>RemoveAll&lt;T&gt;() + Add&lt;T&gt;()</c>.
///         The host must also register the DbContext factory via
///         <c>AddDbContextFactory&lt;SentinelCoreDBContext&gt;</c> (pattern-lock PL-7).
///     </para>
/// </summary>
public static class CaseFlowEngineBuilderExtensions
{
    /// <summary>
    ///     Registers the EF Core-backed Case Flow Engine and its persistence services,
    ///     replacing the null-object defaults registered by <c>AddSentinelCore</c>.
    ///     Requires a matching <c>AddDbContextFactory&lt;SentinelCoreDBContext&gt;</c>
    ///     registration in the host's composition root (PL-7).
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddCaseFlowEngine(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Transient so persistence services do not capture scoped/transient
        // dependencies and can be resolved safely from any scope. The DbContext
        // factory they inject is a singleton (PL-7), making this lifetime safe.
        services.RemoveAll<ICaseFlowEngine>();
        services.AddTransient<ICaseFlowEngine, CaseFlowEngine.Cfe.CaseFlowEngine>();

        services.RemoveAll<IEvidenceStore>();
        services.AddTransient<IEvidenceStore, EvidenceStore>();

        services.RemoveAll<IPatternMemoryStore>();
        services.AddTransient<IPatternMemoryStore, PatternMemoryStore>();

        services.RemoveAll<ISignalRepository>();
        services.AddTransient<ISignalRepository, SignalRepository>();

        return services;
    }
}
