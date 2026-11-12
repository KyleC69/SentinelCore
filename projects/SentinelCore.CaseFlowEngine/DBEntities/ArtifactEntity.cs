// Solution: SentinelCore
// Project:   SentinelCore.CaseFlowEngine
// File:         ArtifactEntity.cs
// Author: Kyle L. Crowder
// Build Num:  110813



using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;

using SentinelCore.Data.DBEntities;




namespace SentinelCore.CaseFlowEngine.DBEntities;





[Table("ArtifactEntity")]
[Index("CaseId", Name = "IX_ArtifactEntity_CaseRecordId")]
public class ArtifactEntity
{
    public string ArtifactDescription { get; set; } = string.Empty;
    public int ArtifactId { get; set; }
    public string ArtifactPath { get; set; } = string.Empty;

    public string ArtifactSource { get; set; } = string.Empty;
    public string ArtifactType { get; set; } = string.Empty;

    [ForeignKey(nameof(CaseId))]
    [InverseProperty(nameof(CaseEntity.ArtifactEntities))]
    public virtual CaseEntity CaseEntity { get; set; } = null!;

    public int CaseId { get; set; }

    public DateTime CreatedDate { get; set; }


    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public int Id { get; set; }

    public DateTime ModifiedDate { get; set; }
}