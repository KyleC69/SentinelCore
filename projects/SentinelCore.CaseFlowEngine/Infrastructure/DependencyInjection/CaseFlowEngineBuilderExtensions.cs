// Solution: SentinelCore
// Project:   SentinelCore.CaseFlowEngine
// File:         CaseFlowEngineBuilderExtensions.cs
// Author: Kyle L. Crowder
// Build Num:  080801



namespace SentinelCore.Infrastructure.DependencyInjection;




/// <summary>
///     Extension methods for registering the Case Flow Engine and its internal services.
///     <para>
///         The Case Flow Engine (CFE) is the single owner of case lifecycle state.
///         This registration method wires both the public <see cref="ICaseFlowEngine" /> facade
///         and its internal persistence layer. External consumers should depend only on
///         <see cref="ICaseFlowEngine" /> — never on the internal repository.
///     </para>
/// </summary>