using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Krizovka.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase5_MethodologiesDiagramsLegalBasis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_reports_OrganizationId_ReporterAccountId_ClientReportId",
                table: "reports");

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedAt",
                table: "reports",
                type: "character varying(48)",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LocationMeasuredAt",
                table: "reports",
                type: "character varying(48)",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedAt",
                table: "reports",
                type: "character varying(48)",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "reports",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                table: "reports",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedAt",
                table: "report_history",
                type: "character varying(48)",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedAt",
                table: "report_attachments",
                type: "character varying(48)",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedAt",
                table: "organizations",
                type: "character varying(48)",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedAt",
                table: "organizations",
                type: "character varying(48)",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedAt",
                table: "organization_areas",
                type: "character varying(48)",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedAt",
                table: "organization_areas",
                type: "character varying(48)",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedAt",
                table: "guides",
                type: "character varying(48)",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PublishedAt",
                table: "guides",
                type: "character varying(48)",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedAt",
                table: "guides",
                type: "character varying(48)",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "DiagramMermaid",
                table: "guides",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LegalBasisJson",
                table: "guides",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "QuickSummary",
                table: "guides",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedAt",
                table: "guide_templates",
                type: "character varying(48)",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "DiagramMermaid",
                table: "guide_templates",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LegalBasisJson",
                table: "guide_templates",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "QuickSummary",
                table: "guide_templates",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RecommendedOrgTypesJson",
                table: "guide_templates",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_reports_OrganizationId_ClientReportId",
                table: "reports",
                columns: new[] { "OrganizationId", "ClientReportId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_reports_OrganizationId_ClientReportId",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "DiagramMermaid",
                table: "guides");

            migrationBuilder.DropColumn(
                name: "LegalBasisJson",
                table: "guides");

            migrationBuilder.DropColumn(
                name: "QuickSummary",
                table: "guides");

            migrationBuilder.DropColumn(
                name: "DiagramMermaid",
                table: "guide_templates");

            migrationBuilder.DropColumn(
                name: "LegalBasisJson",
                table: "guide_templates");

            migrationBuilder.DropColumn(
                name: "QuickSummary",
                table: "guide_templates");

            migrationBuilder.DropColumn(
                name: "RecommendedOrgTypesJson",
                table: "guide_templates");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "reports",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(48)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "LocationMeasuredAt",
                table: "reports",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(48)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "reports",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(48)");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "report_history",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(48)");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "report_attachments",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(48)");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "organizations",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(48)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "organizations",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(48)");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "organization_areas",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(48)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "organization_areas",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(48)");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "guides",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(48)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "PublishedAt",
                table: "guides",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(48)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "guides",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(48)");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "guide_templates",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(48)");

            migrationBuilder.CreateIndex(
                name: "IX_reports_OrganizationId_ReporterAccountId_ClientReportId",
                table: "reports",
                columns: new[] { "OrganizationId", "ReporterAccountId", "ClientReportId" },
                unique: true);
        }
    }
}
