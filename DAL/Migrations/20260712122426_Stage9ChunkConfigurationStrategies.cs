using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class Stage9ChunkConfigurationStrategies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_SystemSettings_ChunkStrategy",
                table: "SystemSettings");

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "ChunkStrategy",
                value: 3);

            migrationBuilder.AddCheckConstraint(
                name: "CK_SystemSettings_ChunkStrategy",
                table: "SystemSettings",
                sql: "[ChunkStrategy] IN (0, 1, 2, 3)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_SystemSettings_ChunkStrategy",
                table: "SystemSettings");

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "ChunkStrategy",
                value: 0);

            migrationBuilder.AddCheckConstraint(
                name: "CK_SystemSettings_ChunkStrategy",
                table: "SystemSettings",
                sql: "[ChunkStrategy] IN (0, 1, 2)");
        }
    }
}
