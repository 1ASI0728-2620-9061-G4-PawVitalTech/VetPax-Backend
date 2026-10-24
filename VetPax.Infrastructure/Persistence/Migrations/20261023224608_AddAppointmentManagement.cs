using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VetPax.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "appointment_outbox",
                columns: table => new
                {
                    event_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    appointment_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    event_type = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    payload = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    occurred_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    next_attempt_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    published_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    attempts = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    last_error = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_appointment_outbox", x => x.event_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "appointments",
                columns: table => new
                {
                    appointment_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    owner_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    pet_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    clinic_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    veterinarian_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    appointment_date = table.Column<DateOnly>(type: "date", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time(6)", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time(6)", nullable: false),
                    reason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    active_slot_time = table.Column<TimeOnly>(type: "time(6)", nullable: true, computedColumnSql: "CASE WHEN `status` IN ('Pending', 'Attended') THEN `start_time` ELSE NULL END", stored: true),
                    clinical_attention_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_appointments", x => x.appointment_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "appointment_attention_links",
                columns: table => new
                {
                    appointment_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    clinical_attention_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    linked_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_appointment_attention_links", x => x.appointment_id);
                    table.ForeignKey(
                        name: "FK_appointment_attention_links_appointments_appointment_id",
                        column: x => x.appointment_id,
                        principalTable: "appointments",
                        principalColumn: "appointment_id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "appointment_reminders",
                columns: table => new
                {
                    appointment_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    owner_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    due_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    updated_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    sent_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_appointment_reminders", x => x.appointment_id);
                    table.ForeignKey(
                        name: "FK_appointment_reminders_appointments_appointment_id",
                        column: x => x.appointment_id,
                        principalTable: "appointments",
                        principalColumn: "appointment_id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ux_appointment_attention_links_attention",
                table: "appointment_attention_links",
                column: "clinical_attention_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_appointment_outbox_pending",
                table: "appointment_outbox",
                columns: new[] { "published_at_utc", "next_attempt_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_appointment_reminders_due",
                table: "appointment_reminders",
                columns: new[] { "status", "due_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_appointments_clinic_date",
                table: "appointments",
                columns: new[] { "clinic_id", "appointment_date" });

            migrationBuilder.CreateIndex(
                name: "ix_appointments_owner",
                table: "appointments",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "ix_appointments_vet_agenda",
                table: "appointments",
                columns: new[] { "veterinarian_id", "appointment_date", "start_time" });

            migrationBuilder.CreateIndex(
                name: "ux_appointments_vet_active_slot",
                table: "appointments",
                columns: new[] { "veterinarian_id", "appointment_date", "active_slot_time" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "appointment_attention_links");

            migrationBuilder.DropTable(
                name: "appointment_outbox");

            migrationBuilder.DropTable(
                name: "appointment_reminders");

            migrationBuilder.DropTable(
                name: "appointments");
        }
    }
}
