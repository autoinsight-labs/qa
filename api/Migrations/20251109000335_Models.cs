using System;
using AutoInsight.Models;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aspnet.Migrations
{
    /// <inheritdoc />
    public partial class Models : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:employee_role", "admin,member")
                .Annotation("Npgsql:Enum:invite_status", "pending,accepted,rejected")
                .Annotation("Npgsql:Enum:vehicle_model", "mottu_sport110i,mottue,honda_pop110i,tvs_sport110i")
                .Annotation("Npgsql:Enum:vehicle_status", "scheduled,waiting,on_service,finished,cancelled");

            migrationBuilder.CreateTable(
                name: "yards",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    owner_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_yards", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "employee_invites",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    role = table.Column<EmployeeRole>(type: "employee_role", nullable: false),
                    status = table.Column<InviteStatus>(type: "invite_status", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW() AT TIME ZONE 'UTC'"),
                    accepted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    inviter_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    yard_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employee_invites", x => x.id);
                    table.ForeignKey(
                        name: "fk_employee_invites_yards_yard_id",
                        column: x => x.yard_id,
                        principalTable: "yards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "yard_capacity_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    yard_id = table.Column<Guid>(type: "uuid", nullable: false),
                    captured_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    vehicles_in_yard = table.Column<int>(type: "integer", nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_yard_capacity_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "fk_yard_capacity_snapshots_yards_yard_id",
                        column: x => x.yard_id,
                        principalTable: "yards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "yard_employees",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    image_url = table.Column<string>(type: "text", nullable: true),
                    role = table.Column<EmployeeRole>(type: "employee_role", nullable: false),
                    user_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    yard_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_yard_employees", x => x.id);
                    table.ForeignKey(
                        name: "fk_yard_employees_yards_yard_id",
                        column: x => x.yard_id,
                        principalTable: "yards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vehicles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    plate = table.Column<string>(type: "text", nullable: false),
                    model = table.Column<VehicleModel>(type: "vehicle_model", nullable: false),
                    status = table.Column<VehicleStatus>(type: "vehicle_status", nullable: false),
                    entered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    left_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    yard_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assignee_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vehicles", x => x.id);
                    table.ForeignKey(
                        name: "fk_vehicles_yard_employees_assignee_id",
                        column: x => x.assignee_id,
                        principalTable: "yard_employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_vehicles_yards_yard_id",
                        column: x => x.yard_id,
                        principalTable: "yards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "beacons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    uuid = table.Column<string>(type: "text", nullable: false),
                    major = table.Column<string>(type: "text", nullable: false),
                    minor = table.Column<string>(type: "text", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_beacons", x => x.id);
                    table.ForeignKey(
                        name: "fk_beacons_vehicles_vehicle_id",
                        column: x => x.vehicle_id,
                        principalTable: "vehicles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_beacons_major_minor",
                table: "beacons",
                columns: new[] { "major", "minor" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_beacons_uuid",
                table: "beacons",
                column: "uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_beacons_vehicle_id",
                table: "beacons",
                column: "vehicle_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_employee_invites_yard_id",
                table: "employee_invites",
                column: "yard_id");

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_assignee_id",
                table: "vehicles",
                column: "assignee_id");

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_yard_id",
                table: "vehicles",
                column: "yard_id");

            migrationBuilder.CreateIndex(
                name: "ix_yard_capacity_snapshots_yard_id",
                table: "yard_capacity_snapshots",
                column: "yard_id");

            migrationBuilder.CreateIndex(
                name: "ix_yard_employees_yard_id",
                table: "yard_employees",
                column: "yard_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "beacons");

            migrationBuilder.DropTable(
                name: "employee_invites");

            migrationBuilder.DropTable(
                name: "yard_capacity_snapshots");

            migrationBuilder.DropTable(
                name: "vehicles");

            migrationBuilder.DropTable(
                name: "yard_employees");

            migrationBuilder.DropTable(
                name: "yards");
        }
    }
}
