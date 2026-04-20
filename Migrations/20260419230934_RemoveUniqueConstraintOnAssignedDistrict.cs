using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CitizenAppealsPortal.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUniqueConstraintOnAssignedDistrict : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_AssignedDistrictId",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "IX_Districts_DeputyId",
                table: "Districts",
                column: "DeputyId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_AssignedDistrictId",
                table: "AspNetUsers",
                column: "AssignedDistrictId");

            migrationBuilder.AddForeignKey(
                name: "FK_Districts_AspNetUsers_DeputyId",
                table: "Districts",
                column: "DeputyId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Districts_AspNetUsers_DeputyId",
                table: "Districts");

            migrationBuilder.DropIndex(
                name: "IX_Districts_DeputyId",
                table: "Districts");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_AssignedDistrictId",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_AssignedDistrictId",
                table: "AspNetUsers",
                column: "AssignedDistrictId",
                unique: true);
        }
    }
}
