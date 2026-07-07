// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         SentinelCoreDbContext.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using Microsoft.EntityFrameworkCore;

using SentinelCoreLib.Infrastructure.Persistence.Entities;




namespace SentinelCoreLib.Infrastructure.Persistence;





/// <summary>
///     Entity Framework Core database context for SentinelCore persistence.
/// </summary>
public sealed class SentinelCoreDbContext : DbContext
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SentinelCoreDbContext" /> class.
    /// </summary>
    public SentinelCoreDbContext(DbContextOptions<SentinelCoreDbContext> options) : base(options)
    {
    }








    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<CaseEntity>().HasIndex(c => c.CaseId).IsUnique();

        modelBuilder.Entity<EvidenceEntity>().HasIndex(e => new { e.CaseId, e.Timestamp });

        modelBuilder.Entity<PatternMemoryEntity>().HasIndex(p => p.CaseId);
    }








    /// <summary>
    ///     Gets or sets the cases DbSet.
    /// </summary>
    public DbSet<CaseEntity> Cases { get; set; } = default!;

    /// <summary>
    ///     Gets or sets the evidence DbSet.
    /// </summary>
    public DbSet<EvidenceEntity> Evidence { get; set; } = default!;

    /// <summary>
    ///     Gets or sets the pattern memory DbSet.
    /// </summary>
    public DbSet<PatternMemoryEntity> PatternMemory { get; set; } = default!;
}