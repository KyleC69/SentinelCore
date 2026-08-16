using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SentinelCore.Migrations
{
    /// <inheritdoc />
    public partial class update2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "SignalId",
                table: "SignalEntity",
                type: "int",
                nullable: false,
                defaultValueSql: "NEXT VALUE FOR [dbo].[Signal_ID_seq]",
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "SignalId",
                table: "SignalEntity",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValueSql: "NEXT VALUE FOR [dbo].[Signal_ID_seq]");
        }
    }
}
