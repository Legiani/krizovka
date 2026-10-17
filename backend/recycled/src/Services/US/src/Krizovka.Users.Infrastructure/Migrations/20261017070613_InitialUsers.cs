using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Krizovka.Users.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "backoffice_roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_backoffice_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_accounts",
                columns: table => new
                {
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    identity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    account_type = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    registration_system_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    first_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    last_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_accounts", x => x.account_id);
                });

            migrationBuilder.CreateTable(
                name: "backoffice_permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    module = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    scope_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    scope_value = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_backoffice_permissions", x => x.id);
                    table.ForeignKey(
                        name: "fk_backoffice_permissions_backoffice_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "backoffice_roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "backoffice_users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    identity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    first_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    last_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    is_super_admin = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_backoffice_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_backoffice_users_backoffice_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "backoffice_roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "account_claims",
                columns: table => new
                {
                    claim_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    claim_token = table.Column<string>(type: "text", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_account_claims", x => x.claim_id);
                    table.ForeignKey(
                        name: "fk_account_claims_user_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "user_accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "relationships",
                columns: table => new
                {
                    relationship_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    child_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    relation_type = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    valid_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    valid_to = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    metadata_json = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_relationships", x => x.relationship_id);
                    table.ForeignKey(
                        name: "fk_relationships_user_accounts_child_account_id",
                        column: x => x.child_account_id,
                        principalTable: "user_accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_relationships_user_accounts_parent_account_id",
                        column: x => x.parent_account_id,
                        principalTable: "user_accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "role_assignments",
                columns: table => new
                {
                    assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_code = table.Column<string>(type: "text", nullable: false),
                    scope_type = table.Column<string>(type: "text", nullable: false),
                    scope_id = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    valid_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    valid_to = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    granted_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_assignments", x => x.assignment_id);
                    table.ForeignKey(
                        name: "fk_role_assignments_user_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "user_accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_preferences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    system_id = table.Column<Guid>(type: "uuid", nullable: false),
                    favorites = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    menu_layout = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    notification_settings = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_preferences", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_preferences_user_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "user_accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_verifications",
                columns: table => new
                {
                    verification_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attribute_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    verification_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    verification_method = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    verified_by = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_verifications", x => x.verification_id);
                    table.ForeignKey(
                        name: "fk_user_verifications_user_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "user_accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_account_claims_account_id",
                table: "account_claims",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_account_claims_claim_token",
                table: "account_claims",
                column: "claim_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_backoffice_permissions_role_id",
                table: "backoffice_permissions",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_backoffice_roles_code",
                table: "backoffice_roles",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_backoffice_users_role_id",
                table: "backoffice_users",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_relationships_child_account_id",
                table: "relationships",
                column: "child_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_relationships_parent_account_id",
                table: "relationships",
                column: "parent_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_assignments_account_id",
                table: "role_assignments",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_assignments_scope_type_scope_id",
                table: "role_assignments",
                columns: new[] { "scope_type", "scope_id" });

            migrationBuilder.CreateIndex(
                name: "ix_user_accounts_identity_id",
                table: "user_accounts",
                column: "identity_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_preferences_account_id_system_id",
                table: "user_preferences",
                columns: new[] { "account_id", "system_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_user_verifications_account",
                table: "user_verifications",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "idx_user_verifications_account_attribute",
                table: "user_verifications",
                columns: new[] { "account_id", "attribute_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_user_verifications_status",
                table: "user_verifications",
                column: "verification_status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_claims");

            migrationBuilder.DropTable(
                name: "backoffice_permissions");

            migrationBuilder.DropTable(
                name: "backoffice_users");

            migrationBuilder.DropTable(
                name: "relationships");

            migrationBuilder.DropTable(
                name: "role_assignments");

            migrationBuilder.DropTable(
                name: "user_preferences");

            migrationBuilder.DropTable(
                name: "user_verifications");

            migrationBuilder.DropTable(
                name: "backoffice_roles");

            migrationBuilder.DropTable(
                name: "user_accounts");
        }
    }
}
