using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VetPax.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNutritionReminderProcessing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "attempt_count",
                table: "nutrition_reminders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "last_error",
                table: "nutrition_reminders",
                type: "varchar(250)",
                maxLength: 250,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<Guid>(
                name: "lease_id",
                table: "nutrition_reminders",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<DateTime>(
                name: "locked_until_utc",
                table: "nutrition_reminders",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "next_attempt_at_utc",
                table: "nutrition_reminders",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_nutrition_reminders_status_next_attempt_at_utc_locked_until_~",
                table: "nutrition_reminders",
                columns: new[] { "status", "next_attempt_at_utc", "locked_until_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_nutrition_reminders_status_next_attempt_at_utc_locked_until_~",
                table: "nutrition_reminders");

            migrationBuilder.DropColumn(
                name: "attempt_count",
                table: "nutrition_reminders");

            migrationBuilder.DropColumn(
                name: "last_error",
                table: "nutrition_reminders");

            migrationBuilder.DropColumn(
                name: "lease_id",
                table: "nutrition_reminders");

            migrationBuilder.DropColumn(
                name: "locked_until_utc",
                table: "nutrition_reminders");

            migrationBuilder.DropColumn(
                name: "next_attempt_at_utc",
                table: "nutrition_reminders");
        }
    }
}
