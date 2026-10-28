using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VetPax.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdherenceOutboxDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "last_error",
                table: "adherence_outbox",
                type: "varchar(1024)",
                maxLength: 1024,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<Guid>(
                name: "lease_token",
                table: "adherence_outbox",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<DateTime>(
                name: "locked_until_utc",
                table: "adherence_outbox",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "next_attempt_at_utc",
                table: "adherence_outbox",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_adherence_outbox_published_at_utc_next_attempt_at_utc_locked~",
                table: "adherence_outbox",
                columns: new[] { "published_at_utc", "next_attempt_at_utc", "locked_until_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_adherence_outbox_published_at_utc_next_attempt_at_utc_locked~",
                table: "adherence_outbox");

            migrationBuilder.DropColumn(
                name: "last_error",
                table: "adherence_outbox");

            migrationBuilder.DropColumn(
                name: "lease_token",
                table: "adherence_outbox");

            migrationBuilder.DropColumn(
                name: "locked_until_utc",
                table: "adherence_outbox");

            migrationBuilder.DropColumn(
                name: "next_attempt_at_utc",
                table: "adherence_outbox");
        }
    }
}
