using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SentinelCore.Migrations
{
    /// <inheritdoc />
    public partial class update22 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "FK_CaseEntity",
                table: "CaseEntity");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "CaseEntity",
                type: "datetime2(2)",
                precision: 2,
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2(2)",
                oldPrecision: 2);

            migrationBuilder.AlterColumn<int>(
                name: "PlanId",
                table: "CaseEntity",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "PatternMemoryId",
                table: "CaseEntity",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "CaseEntity",
                type: "datetime2(2)",
                precision: 2,
                nullable: false,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(2)",
                oldPrecision: 2);

            migrationBuilder.CreateIndex(
                name: "FK_CaseEntity",
                table: "CaseEntity",
                column: "PatternMemoryId",
                unique: true,
                filter: "[PatternMemoryId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "FK_CaseEntity",
                table: "CaseEntity");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "CaseEntity",
                type: "datetime2(2)",
                precision: 2,
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2(2)",
                oldPrecision: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PlanId",
                table: "CaseEntity",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PatternMemoryId",
                table: "CaseEntity",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "CaseEntity",
                type: "datetime2(2)",
                precision: 2,
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2(2)",
                oldPrecision: 2,
                oldDefaultValueSql: "GETDATE()");

            migrationBuilder.CreateIndex(
                name: "FK_CaseEntity",
                table: "CaseEntity",
                column: "PatternMemoryId",
                unique: true);
        }
    }
}
