using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Diploma.Entity.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "measurements",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    received_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    packet_number = table.Column<int>(type: "integer", nullable: false),
                    device_ts = table.Column<long>(type: "bigint", nullable: false),
                    baseline_ready = table.Column<bool>(type: "boolean", nullable: false),
                    z_rms = table.Column<double>(type: "double precision", nullable: false),
                    z_crest = table.Column<double>(type: "double precision", nullable: false),
                    z_bear = table.Column<double>(type: "double precision", nullable: false),
                    z_gear = table.Column<double>(type: "double precision", nullable: false),
                    z_freq = table.Column<double>(type: "double precision", nullable: false),
                    x_rms = table.Column<double>(type: "double precision", nullable: false),
                    x_crest = table.Column<double>(type: "double precision", nullable: false),
                    x_bear = table.Column<double>(type: "double precision", nullable: false),
                    x_gear = table.Column<double>(type: "double precision", nullable: false),
                    x_freq = table.Column<double>(type: "double precision", nullable: false),
                    y_rms = table.Column<double>(type: "double precision", nullable: false),
                    y_crest = table.Column<double>(type: "double precision", nullable: false),
                    y_bear = table.Column<double>(type: "double precision", nullable: false),
                    y_gear = table.Column<double>(type: "double precision", nullable: false),
                    y_freq = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_measurements", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "thresholds",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    metric = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    value = table.Column<double>(type: "double precision", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_thresholds", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "alerts",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    triggered_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    severity = table.Column<string>(type: "text", nullable: false),
                    axis = table.Column<string>(type: "text", nullable: false),
                    metric = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<double>(type: "double precision", nullable: false),
                    threshold = table.Column<double>(type: "double precision", nullable: false),
                    measurement_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alerts", x => x.id);
                    table.ForeignKey(
                        name: "FK_alerts_measurements_measurement_id",
                        column: x => x.measurement_id,
                        principalTable: "measurements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "thresholds",
                columns: new[] { "id", "metric", "updated_at", "value" },
                values: new object[,]
                {
                    { 1, "crest", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 4.0 },
                    { 2, "bearing", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.050000000000000003 },
                    { 3, "gear", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.050000000000000003 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_alerts_measurement_id",
                table: "alerts",
                column: "measurement_id");

            migrationBuilder.CreateIndex(
                name: "ix_alerts_triggered_at",
                table: "alerts",
                column: "triggered_at");

            migrationBuilder.CreateIndex(
                name: "ix_measurements_received_at",
                table: "measurements",
                column: "received_at");

            migrationBuilder.CreateIndex(
                name: "ix_thresholds_metric",
                table: "thresholds",
                column: "metric",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alerts");

            migrationBuilder.DropTable(
                name: "thresholds");

            migrationBuilder.DropTable(
                name: "measurements");
        }
    }
}
