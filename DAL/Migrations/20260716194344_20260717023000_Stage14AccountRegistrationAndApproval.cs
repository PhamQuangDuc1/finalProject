using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class _20260717023000_Stage14AccountRegistrationAndApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_Role",
                table: "Users");

            migrationBuilder.AddColumn<int>(
                name: "AccountStatus",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovedByAdminId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE [Users]
                SET [Email] = CONCAT([Username], '@studymate.local')
                WHERE [Email] = N'' OR [Email] IS NULL;

                UPDATE [Users]
                SET [AccountStatus] = CASE WHEN [IsActive] = 1 THEN 1 ELSE 2 END
                WHERE [Role] IN (0, 1, 2);

                UPDATE [Users]
                SET [ApprovedAt] = [CreatedAt]
                WHERE [AccountStatus] = 1 AND [ApprovedAt] IS NULL;
                """);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "AccountStatus", "ApprovedAt", "ApprovedByAdminId", "Email" },
                values: new object[] { 1, new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, "admin@studymate.local" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "AccountStatus", "ApprovedAt", "ApprovedByAdminId", "Email" },
                values: new object[] { 1, new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Utc), 1, "teacherA@studymate.local" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "AccountStatus", "ApprovedAt", "ApprovedByAdminId", "Email" },
                values: new object[] { 1, new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Utc), 1, "teacherB@studymate.local" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "AccountStatus", "ApprovedAt", "ApprovedByAdminId", "Email" },
                values: new object[] { 1, new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Utc), 1, "student@studymate.local" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_ApprovedByAdminId",
                table: "Users",
                column: "ApprovedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_AccountStatus",
                table: "Users",
                sql: "[AccountStatus] IN (0, 1, 2)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_Role",
                table: "Users",
                sql: "[Role] IN (0, 1, 2, 3)");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_ApprovedByAdminId",
                table: "Users",
                column: "ApprovedByAdminId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_ApprovedByAdminId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_ApprovedByAdminId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_AccountStatus",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_Role",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AccountStatus",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ApprovedByAdminId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Users");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_Role",
                table: "Users",
                sql: "[Role] IN (0, 1, 2)");
        }
    }
}
