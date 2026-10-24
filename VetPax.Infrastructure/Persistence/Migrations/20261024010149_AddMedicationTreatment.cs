using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VetPax.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicationTreatment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "medication_outbox",
                columns: table => new
                {
                    event_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    plan_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    event_type = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    payload = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    occurred_at_utc = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: false),
                    next_attempt_at_utc = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: false),
                    published_at_utc = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: true),
                    attempts = table.Column<int>(type: "int", nullable: false),
                    last_error = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    lease_token = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    locked_until_utc = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medication_outbox", x => x.event_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "medication_plans",
                columns: table => new
                {
                    plan_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    owner_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    pet_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    clinic_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    veterinarian_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    clinical_attention_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    medication_name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    route = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    instructions = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    time_zone_id = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    reminders_enabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    schedule_version = table.Column<int>(type: "int", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medication_plans", x => x.plan_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "medication_reminder_preferences",
                columns: table => new
                {
                    plan_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    enabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medication_reminder_preferences", x => x.plan_id);
                    table.ForeignKey(
                        name: "FK_medication_reminder_preferences_medication_plans_plan_id",
                        column: x => x.plan_id,
                        principalTable: "medication_plans",
                        principalColumn: "plan_id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "medication_schedules",
                columns: table => new
                {
                    schedule_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    plan_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    schedule_version = table.Column<int>(type: "int", nullable: false),
                    local_time = table.Column<TimeOnly>(type: "time(6)", nullable: false),
                    days_mask = table.Column<int>(type: "int", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    unit = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medication_schedules", x => x.schedule_id);
                    table.ForeignKey(
                        name: "FK_medication_schedules_medication_plans_plan_id",
                        column: x => x.plan_id,
                        principalTable: "medication_plans",
                        principalColumn: "plan_id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "medication_doses",
                columns: table => new
                {
                    dose_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    plan_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    schedule_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    schedule_version = table.Column<int>(type: "int", nullable: false),
                    local_date = table.Column<DateOnly>(type: "date", nullable: false),
                    local_time = table.Column<TimeOnly>(type: "time(6)", nullable: false),
                    scheduled_at_utc = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: false),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medication_doses", x => x.dose_id);
                    table.ForeignKey(
                        name: "FK_medication_doses_medication_plans_plan_id",
                        column: x => x.plan_id,
                        principalTable: "medication_plans",
                        principalColumn: "plan_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_medication_doses_medication_schedules_schedule_id",
                        column: x => x.schedule_id,
                        principalTable: "medication_schedules",
                        principalColumn: "schedule_id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "medication_administrations",
                columns: table => new
                {
                    administration_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    dose_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    administered_by = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    administered_at_utc = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: false),
                    recorded_at_utc = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medication_administrations", x => x.administration_id);
                    table.ForeignKey(
                        name: "FK_medication_administrations_medication_doses_dose_id",
                        column: x => x.dose_id,
                        principalTable: "medication_doses",
                        principalColumn: "dose_id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "medication_reminders",
                columns: table => new
                {
                    reminder_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    plan_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    dose_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    owner_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    due_at_utc = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: false),
                    next_attempt_at_utc = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: false),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    attempts = table.Column<int>(type: "int", nullable: false),
                    last_error = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sent_at_utc = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: true),
                    locked_until_utc = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: true),
                    lease_token = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    updated_at_utc = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medication_reminders", x => x.reminder_id);
                    table.ForeignKey(
                        name: "FK_medication_reminders_medication_doses_dose_id",
                        column: x => x.dose_id,
                        principalTable: "medication_doses",
                        principalColumn: "dose_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_medication_reminders_medication_plans_plan_id",
                        column: x => x.plan_id,
                        principalTable: "medication_plans",
                        principalColumn: "plan_id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_medication_administrations_dose_id",
                table: "medication_administrations",
                column: "dose_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_medication_doses_plan_id_schedule_version_schedule_id_local_~",
                table: "medication_doses",
                columns: new[] { "plan_id", "schedule_version", "schedule_id", "local_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_medication_doses_schedule_id",
                table: "medication_doses",
                column: "schedule_id");

            migrationBuilder.CreateIndex(
                name: "IX_medication_doses_status_scheduled_at_utc",
                table: "medication_doses",
                columns: new[] { "status", "scheduled_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_medication_outbox_published_at_utc_next_attempt_at_utc",
                table: "medication_outbox",
                columns: new[] { "published_at_utc", "next_attempt_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_medication_plans_clinic_id_veterinarian_id",
                table: "medication_plans",
                columns: new[] { "clinic_id", "veterinarian_id" });

            migrationBuilder.CreateIndex(
                name: "IX_medication_plans_pet_id",
                table: "medication_plans",
                column: "pet_id");

            migrationBuilder.CreateIndex(
                name: "IX_medication_plans_status_end_date",
                table: "medication_plans",
                columns: new[] { "status", "end_date" });

            migrationBuilder.CreateIndex(
                name: "IX_medication_reminders_dose_id",
                table: "medication_reminders",
                column: "dose_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_medication_reminders_plan_id",
                table: "medication_reminders",
                column: "plan_id");

            migrationBuilder.CreateIndex(
                name: "IX_medication_reminders_status_next_attempt_at_utc",
                table: "medication_reminders",
                columns: new[] { "status", "next_attempt_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_medication_schedules_plan_id_schedule_version",
                table: "medication_schedules",
                columns: new[] { "plan_id", "schedule_version" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "medication_administrations");

            migrationBuilder.DropTable(
                name: "medication_outbox");

            migrationBuilder.DropTable(
                name: "medication_reminder_preferences");

            migrationBuilder.DropTable(
                name: "medication_reminders");

            migrationBuilder.DropTable(
                name: "medication_doses");

            migrationBuilder.DropTable(
                name: "medication_schedules");

            migrationBuilder.DropTable(
                name: "medication_plans");
        }
    }
}
