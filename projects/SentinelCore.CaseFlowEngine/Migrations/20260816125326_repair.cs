using System;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SentinelCore.Migrations
{
    /// <inheritdoc />
    public partial class Repair : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CaseEntity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    InitiatingSignal = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(2)", precision: 2, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(2)", precision: 2, nullable: false),
                    PlanId = table.Column<int>(type: "int", nullable: false),
                    AdditionalSignals = table.Column<int>(type: "int", nullable: true),
                    EvidenceId = table.Column<int>(type: "int", nullable: true),
                    Remediation = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    PatternMemoryId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseEntity", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EvidenceEntity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EvidenceId = table.Column<int>(type: "int", nullable: false),
                    ContentJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Provenance = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvidenceEntity", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InvestigationPlanStepsEntity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StepId = table.Column<int>(type: "int", nullable: false, comment: "Links this step the plan it was created in"),
                    PlanId = table.Column<int>(type: "int", nullable: false),
                    Surface = table.Column<string>(type: "nvarchar(max)", nullable: false, comment: "Domain or Surface that the task applies to"),
                    Instruction = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Result = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CompletedSuccessfully = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                        .Annotation("Relational:DefaultConstraintName", "DF_InvestigationPlanStepsEntities_CompletedSuccessfully"),
                    TaskBlocked = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                        .Annotation("Relational:DefaultConstraintName", "DF_InvestigationPlanStepsEntities_TaskBlocked"),
                    IsTargetPropertyMissing = table.Column<bool>(type: "bit", nullable: false, defaultValue: false, comment: "If the target of the task is not found this bit must be flipped")
                        .Annotation("Relational:DefaultConstraintName", "DF_InvestigationPlanStepsEntities_IsTargetPropertyMissing")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvestigationPlanSteps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PatternMemoryEntity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatternId = table.Column<int>(type: "int", nullable: false),
                    CaseId = table.Column<int>(type: "int", nullable: false),
                    SignalEmbedding = table.Column<SqlVector<float>>(type: "vector(1024)", maxLength: 1024, nullable: true),
                    SummaryEmbedding = table.Column<SqlVector<float>>(type: "vector(1024)", maxLength: 1024, nullable: true),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2(2)", precision: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatternMemory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResolutionEntity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RawJsonContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CaseRecordId = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(2)", precision: 2, nullable: true),
                    Verified = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                        .Annotation("Relational:DefaultConstraintName", "DF_ResolutionSteps_Verified")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResolutionSteps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SignalEntity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SignalId = table.Column<int>(type: "int", nullable: false),
                    SignalText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Signals", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "FK_CaseEntity",
                table: "CaseEntity",
                column: "PatternMemoryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CaseEntity",
                table: "CaseEntity",
                column: "InitiatingSignal",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Evidence",
                table: "CaseEntity",
                column: "EvidenceId",
                unique: true,
                filter: "[EvidenceId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "AK_InvestigationPlanSteps_InvestigationPlanStepId",
                table: "InvestigationPlanStepsEntity",
                column: "StepId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "AK_PatternMemory_PatternMemoryId",
                table: "PatternMemoryEntity",
                column: "PatternId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatternMemory_CaseRecordId",
                table: "PatternMemoryEntity",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "AK_Signals_SignalId",
                table: "SignalEntity",
                column: "SignalId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CaseEntity");

            migrationBuilder.DropTable(
                name: "EvidenceEntity");

            migrationBuilder.DropTable(
                name: "InvestigationPlanStepsEntity");

            migrationBuilder.DropTable(
                name: "PatternMemoryEntity");

            migrationBuilder.DropTable(
                name: "ResolutionEntity");

            migrationBuilder.DropTable(
                name: "SignalEntity");
        }
    }
}
