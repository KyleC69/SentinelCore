// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         SentinelCoreEntities.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;




namespace SentinelCoreLib.Infrastructure.Persistence.Entities;





/// <summary>
///     Entity representing a case.
/// </summary>
public sealed class CaseEntity
{

    /// <summary>
    ///     Gets or sets the case identifier.
    /// </summary>
    public required string CaseId { get; set; }

    /// <summary>
    ///     Gets or sets the case creation timestamp.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    ///     Gets or sets the database primary key.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    ///     Gets or sets the JSON-serialized case state.
    /// </summary>
    public required string StateJson { get; set; }

    /// <summary>
    ///     Gets or sets the case status.
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    ///     Gets or sets the case title.
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    ///     Gets or sets the case last-updated timestamp.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }
}





/// <summary>
///     Entity representing an evidence item.
/// </summary>
public sealed class EvidenceEntity
{

    /// <summary>
    ///     Gets or sets the associated case identifier.
    /// </summary>
    public required string CaseId { get; set; }

    /// <summary>
    ///     Gets or sets the JSON content.
    /// </summary>
    public required string ContentJson { get; set; }

    /// <summary>
    ///     Gets or sets the evidence identifier.
    /// </summary>
    public required string EvidenceId { get; set; }

    /// <summary>
    ///     Gets or sets the database primary key.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    ///     Gets or sets the provenance.
    /// </summary>
    public required string Provenance { get; set; }

    /// <summary>
    ///     Gets or sets the evidence source.
    /// </summary>
    public required string Source { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    ///     Gets or sets the evidence type.
    /// </summary>
    public required string Type { get; set; }
}





/// <summary>
///     Entity representing a pattern memory entry.
/// </summary>
public sealed class PatternMemoryEntity
{

    /// <summary>
    ///     Gets or sets the associated case identifier, if any.
    /// </summary>
    public string? CaseId { get; set; }

    /// <summary>
    ///     Gets or sets the entry category.
    /// </summary>
    public required string Category { get; set; }

    /// <summary>
    ///     Gets or sets the description.
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    ///     Gets or sets the embedding vector.
    /// </summary>
    public required float[] Embedding { get; set; }

    /// <summary>
    ///     Gets or sets the entry identifier.
    /// </summary>
    public required string EntryId { get; set; }

    /// <summary>
    ///     Gets or sets the database primary key.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    ///     Gets or sets the JSON metadata.
    /// </summary>
    public required string MetadataJson { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
}