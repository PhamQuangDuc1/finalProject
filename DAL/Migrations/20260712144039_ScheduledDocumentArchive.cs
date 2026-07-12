using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class ScheduledDocumentArchive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledArchiveAt",
                table: "Documents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScheduledArchiveByTeacherId",
                table: "Documents",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ScheduledArchiveByTeacherId",
                table: "Documents",
                column: "ScheduledArchiveByTeacherId");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Users_ScheduledArchiveByTeacherId",
                table: "Documents",
                column: "ScheduledArchiveByTeacherId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Users_ScheduledArchiveByTeacherId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_ScheduledArchiveByTeacherId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ScheduledArchiveAt",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ScheduledArchiveByTeacherId",
                table: "Documents");
        }
    }
}
