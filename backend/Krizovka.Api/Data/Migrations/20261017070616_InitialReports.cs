using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Krizovka.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReporterAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    AccuracyMeters = table.Column<double>(type: "double precision", nullable: true),
                    LocationMeasuredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Contact_FirstName = table.Column<string>(type: "text", nullable: true),
                    Contact_LastName = table.Column<string>(type: "text", nullable: true),
                    Contact_PersonnelNumber = table.Column<string>(type: "text", nullable: true),
                    Contact_Phone = table.Column<string>(type: "text", nullable: true),
                    Contact_Email = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "report_attachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IssueId = table.Column<Guid>(type: "uuid", nullable: false),
                    StorageKey = table.Column<string>(type: "text", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_report_attachments_reports_IssueId",
                        column: x => x.IssueId,
                        principalTable: "reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "report_history",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IssueId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    PreviousValue = table.Column<string>(type: "text", nullable: true),
                    NewValue = table.Column<string>(type: "text", nullable: true),
                    Metadata = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_report_history_reports_IssueId",
                        column: x => x.IssueId,
                        principalTable: "reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_report_attachments_IssueId",
                table: "report_attachments",
                column: "IssueId");

            migrationBuilder.CreateIndex(
                name: "IX_report_history_IssueId",
                table: "report_history",
                column: "IssueId");

            migrationBuilder.CreateIndex(
                name: "IX_reports_OrganizationId_CreatedAt",
                table: "reports",
                columns: new[] { "OrganizationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_reports_OrganizationId_ReporterAccountId_ClientReportId",
                table: "reports",
                columns: new[] { "OrganizationId", "ReporterAccountId", "ClientReportId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "report_attachments");

            migrationBuilder.DropTable(
                name: "report_history");

            migrationBuilder.DropTable(
                name: "reports");
        }
    }
}
