using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CitizenAppealsPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Categories",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Code",
                table: "Categories");
        }
    }
}
