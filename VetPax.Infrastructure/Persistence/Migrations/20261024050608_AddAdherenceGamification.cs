using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VetPax.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdherenceGamification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "adherence_outbox",
                columns: table => new
                {
                    event_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    event_type = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    payload_json = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    occurred_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    published_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    attempt_count = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adherence_outbox", x => x.event_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "adherence_processed_events",
                columns: table => new
                {
                    event_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    owner_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    kind = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    source_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    occurred_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    processed_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adherence_processed_events", x => x.event_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "adherence_profiles",
                columns: table => new
                {
                    owner_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    evidence_count = table.Column<int>(type: "int", nullable: false),
                    score = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    level = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    updated_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    concurrency_stamp = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adherence_profiles", x => x.owner_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "adherence_recognitions",
                columns: table => new
                {
                    recognition_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    owner_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    from_level = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    to_level = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    sent_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    delivery_status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    attempt_count = table.Column<int>(type: "int", nullable: false),
                    next_attempt_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    lease_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    locked_until_utc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adherence_recognitions", x => x.recognition_id);
                    table.ForeignKey(
                        name: "FK_adherence_recognitions_adherence_profiles_owner_id",
                        column: x => x.owner_id,
                        principalTable: "adherence_profiles",
                        principalColumn: "owner_id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_adherence_outbox_published_at_utc",
                table: "adherence_outbox",
                column: "published_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_adherence_processed_events_kind_source_id",
                table: "adherence_processed_events",
                columns: new[] { "kind", "source_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_adherence_processed_events_owner_id",
                table: "adherence_processed_events",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "IX_adherence_recognitions_delivery_status_next_attempt_at_utc_l~",
                table: "adherence_recognitions",
                columns: new[] { "delivery_status", "next_attempt_at_utc", "locked_until_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_adherence_recognitions_owner_id",
                table: "adherence_recognitions",
                column: "owner_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "adherence_outbox");

            migrationBuilder.DropTable(
                name: "adherence_processed_events");

            migrationBuilder.DropTable(
                name: "adherence_recognitions");

            migrationBuilder.DropTable(
                name: "adherence_profiles");
        }
    }
}
