using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VetPax.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPetOptionalMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InsuranceProvider",
                table: "pets",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "MicrochipNumber",
                table: "pets",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PhotoUrl",
                table: "pets",
                type: "varchar(2048)",
                maxLength: 2048,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PolicyNumber",
                table: "pets",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "TutorName",
                table: "pets",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InsuranceProvider",
                table: "pets");

            migrationBuilder.DropColumn(
                name: "MicrochipNumber",
                table: "pets");

            migrationBuilder.DropColumn(
                name: "PhotoUrl",
                table: "pets");

            migrationBuilder.DropColumn(
                name: "PolicyNumber",
                table: "pets");

            migrationBuilder.DropColumn(
                name: "TutorName",
                table: "pets");
        }
    }
}
