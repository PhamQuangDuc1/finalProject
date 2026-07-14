using System;
using DAL.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260713143730_EditableDocumentContent")]
    public partial class EditableDocumentContent : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ContentUpdatedAt",
                table: "Documents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ContentUpdatedByTeacherId",
                table: "Documents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ContentVersion",
                table: "Documents",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "EditedContent",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExtractedContent",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasManualEdits",
                table: "Documents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "DocumentVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentId = table.Column<int>(type: "int", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedByTeacherId = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangeNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentVersions_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentVersions_Users_UpdatedByTeacherId",
                        column: x => x.UpdatedByTeacherId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ContentUpdatedByTeacherId",
                table: "Documents",
                column: "ContentUpdatedByTeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentVersions_DocumentId_VersionNumber",
                table: "DocumentVersions",
                columns: new[] { "DocumentId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentVersions_UpdatedByTeacherId",
                table: "DocumentVersions",
                column: "UpdatedByTeacherId");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Users_ContentUpdatedByTeacherId",
                table: "Documents",
                column: "ContentUpdatedByTeacherId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Users_ContentUpdatedByTeacherId",
                table: "Documents");

            migrationBuilder.DropTable(
                name: "DocumentVersions");

            migrationBuilder.DropIndex(
                name: "IX_Documents_ContentUpdatedByTeacherId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ContentUpdatedAt",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ContentUpdatedByTeacherId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ContentVersion",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "EditedContent",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ExtractedContent",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "HasManualEdits",
                table: "Documents");
        }
    }
}
