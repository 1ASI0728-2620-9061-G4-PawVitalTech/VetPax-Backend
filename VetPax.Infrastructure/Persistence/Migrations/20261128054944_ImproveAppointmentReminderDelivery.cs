using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VetPax.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImproveAppointmentReminderDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "attempts",
                table: "appointment_reminders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "last_error",
                table: "appointment_reminders",
                type: "varchar(1024)",
                maxLength: 1024,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<Guid>(
                name: "lease_token",
                table: "appointment_reminders",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<DateTime>(
                name: "locked_until_utc",
                table: "appointment_reminders",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "next_attempt_at_utc",
                table: "appointment_reminders",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_appointment_reminders_retry",
                table: "appointment_reminders",
                columns: new[] { "status", "next_attempt_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_appointment_reminders_retry",
                table: "appointment_reminders");

            migrationBuilder.DropColumn(
                name: "attempts",
                table: "appointment_reminders");

            migrationBuilder.DropColumn(
                name: "last_error",
                table: "appointment_reminders");

            migrationBuilder.DropColumn(
                name: "lease_token",
                table: "appointment_reminders");

            migrationBuilder.DropColumn(
                name: "locked_until_utc",
                table: "appointment_reminders");

            migrationBuilder.DropColumn(
                name: "next_attempt_at_utc",
                table: "appointment_reminders");
        }
    }
}
