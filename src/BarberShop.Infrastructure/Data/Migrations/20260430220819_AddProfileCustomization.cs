using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarberShop.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileCustomization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "photo_url",
                table: "barber_profiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "primary_color",
                table: "barber_profiles",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#18181b");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "photo_url",
                table: "barber_profiles");

            migrationBuilder.DropColumn(
                name: "primary_color",
                table: "barber_profiles");
        }
    }
}
