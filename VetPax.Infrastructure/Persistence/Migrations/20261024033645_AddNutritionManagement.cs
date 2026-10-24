using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VetPax.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNutritionManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "nutrition_feeding_plans",
                columns: table => new
                {
                    plan_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    pet_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    clinic_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    veterinarian_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    food_name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    recommendations = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    time_zone_id = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    reminders_enabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    version = table.Column<int>(type: "int", nullable: false),
                    concurrency_stamp = table.Column<long>(type: "bigint", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nutrition_feeding_plans", x => x.plan_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "nutrition_outbox",
                columns: table => new
                {
                    event_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    event_type = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    payload = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    occurred_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    published_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    attempt_count = table.Column<int>(type: "int", nullable: false),
                    next_attempt_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nutrition_outbox", x => x.event_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "nutrition_active_plans",
                columns: table => new
                {
                    pet_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    plan_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nutrition_active_plans", x => x.pet_id);
                    table.ForeignKey(
                        name: "FK_nutrition_active_plans_nutrition_feeding_plans_plan_id",
                        column: x => x.plan_id,
                        principalTable: "nutrition_feeding_plans",
                        principalColumn: "plan_id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "nutrition_feeding_schedules",
                columns: table => new
                {
                    plan_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    plan_version = table.Column<int>(type: "int", nullable: false),
                    schedule_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    local_time = table.Column<TimeOnly>(type: "time(6)", nullable: false),
                    days_of_week = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    unit = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nutrition_feeding_schedules", x => new { x.plan_id, x.plan_version, x.schedule_id });
                    table.ForeignKey(
                        name: "FK_nutrition_feeding_schedules_nutrition_feeding_plans_plan_id",
                        column: x => x.plan_id,
                        principalTable: "nutrition_feeding_plans",
                        principalColumn: "plan_id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "nutrition_plan_revisions",
                columns: table => new
                {
                    plan_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    version = table.Column<int>(type: "int", nullable: false),
                    food_name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    recommendations = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    time_zone_id = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    replaced_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nutrition_plan_revisions", x => new { x.plan_id, x.version });
                    table.ForeignKey(
                        name: "FK_nutrition_plan_revisions_nutrition_feeding_plans_plan_id",
                        column: x => x.plan_id,
                        principalTable: "nutrition_feeding_plans",
                        principalColumn: "plan_id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "nutrition_reminders",
                columns: table => new
                {
                    reminder_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    plan_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    pet_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    schedule_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    plan_version = table.Column<int>(type: "int", nullable: false),
                    local_date = table.Column<DateOnly>(type: "date", nullable: false),
                    scheduled_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    processed_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nutrition_reminders", x => x.reminder_id);
                    table.ForeignKey(
                        name: "FK_nutrition_reminders_nutrition_feeding_plans_plan_id",
                        column: x => x.plan_id,
                        principalTable: "nutrition_feeding_plans",
                        principalColumn: "plan_id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_nutrition_active_plans_plan_id",
                table: "nutrition_active_plans",
                column: "plan_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_nutrition_feeding_plans_clinic_id_veterinarian_id",
                table: "nutrition_feeding_plans",
                columns: new[] { "clinic_id", "veterinarian_id" });

            migrationBuilder.CreateIndex(
                name: "IX_nutrition_feeding_plans_pet_id_status",
                table: "nutrition_feeding_plans",
                columns: new[] { "pet_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_nutrition_outbox_published_at_utc_next_attempt_at_utc",
                table: "nutrition_outbox",
                columns: new[] { "published_at_utc", "next_attempt_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_nutrition_reminders_plan_id_plan_version_schedule_id_local_d~",
                table: "nutrition_reminders",
                columns: new[] { "plan_id", "plan_version", "schedule_id", "local_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_nutrition_reminders_status_scheduled_at_utc",
                table: "nutrition_reminders",
                columns: new[] { "status", "scheduled_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "nutrition_active_plans");

            migrationBuilder.DropTable(
                name: "nutrition_feeding_schedules");

            migrationBuilder.DropTable(
                name: "nutrition_outbox");

            migrationBuilder.DropTable(
                name: "nutrition_plan_revisions");

            migrationBuilder.DropTable(
                name: "nutrition_reminders");

            migrationBuilder.DropTable(
                name: "nutrition_feeding_plans");
        }
    }
}
