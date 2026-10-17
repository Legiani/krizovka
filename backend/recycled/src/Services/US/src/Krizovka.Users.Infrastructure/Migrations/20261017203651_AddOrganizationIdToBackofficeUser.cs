using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Krizovka.Users.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationIdToBackofficeUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "organization_id",
                table: "backoffice_users",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "organization_id",
                table: "backoffice_users");
        }
    }
}
