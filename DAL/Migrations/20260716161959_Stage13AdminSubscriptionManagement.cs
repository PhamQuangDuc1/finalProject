using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class Stage13AdminSubscriptionManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ScheduledDowngradePackageId",
                table: "UserSubscriptions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_ScheduledDowngradePackageId",
                table: "UserSubscriptions",
                column: "ScheduledDowngradePackageId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserSubscriptions_SubscriptionPackages_ScheduledDowngradePackageId",
                table: "UserSubscriptions",
                column: "ScheduledDowngradePackageId",
                principalTable: "SubscriptionPackages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserSubscriptions_SubscriptionPackages_ScheduledDowngradePackageId",
                table: "UserSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_UserSubscriptions_ScheduledDowngradePackageId",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "ScheduledDowngradePackageId",
                table: "UserSubscriptions");
        }
    }
}
