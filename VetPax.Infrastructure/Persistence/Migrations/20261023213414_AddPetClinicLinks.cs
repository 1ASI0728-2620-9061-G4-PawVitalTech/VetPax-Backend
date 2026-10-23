using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VetPax.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPetClinicLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pet_clinic_links",
                columns: table => new
                {
                    pet_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    clinic_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    linked_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pet_clinic_links", x => new { x.pet_id, x.clinic_id });
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pet_clinic_links");
        }
    }
}
