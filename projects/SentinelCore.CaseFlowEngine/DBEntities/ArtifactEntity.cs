using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Xml.Linq;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer.ValueGeneration.Internal;

using SentinelCore.Data.DBEntities;




namespace SentinelCore.CaseFlowEngine.DBEntities;





[Table("ArtifactEntity")]
[Index("CaseId", Name = "IX_ArtifactEntity_CaseRecordId")]
public class ArtifactEntity
{


    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public int Id { get; set; }
    public int ArtifactId { get; set; }
    public int CaseId { get; set; }

    public string ArtifactSource { get; set; } = string.Empty;
    public string ArtifactType { get; set; } = string.Empty;
    public string ArtifactDescription { get; set; } = string.Empty;
    public string ArtifactPath { get; set; } = string.Empty;
    
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }

    [InverseProperty("ArtifactEntity")] public virtual CaseEntity CaseEntity { get; set; } = null!;

}
