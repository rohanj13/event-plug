using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventPlug.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "organizations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    subscription_tier = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    billing_cycle_start = table.Column<DateOnly>(type: "date", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organizations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "teams",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    org_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_teams", x => x.id);
                    table.ForeignKey(
                        name: "fk_teams_organizations_org_id",
                        column: x => x.org_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    org_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    org_role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_users_organizations_org_id",
                        column: x => x.org_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vendors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    org_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    contact_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    contact_phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vendors", x => x.id);
                    table.ForeignKey(
                        name: "fk_vendors_organizations_org_id",
                        column: x => x.org_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    org_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    event_date = table.Column<DateOnly>(type: "date", nullable: false),
                    location = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    guest_count_estimate = table.Column<int>(type: "integer", nullable: true),
                    budget_band = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    style_notes = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_shared = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_events_organizations_org_id",
                        column: x => x.org_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_events_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "team_memberships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_team_memberships", x => x.id);
                    table.ForeignKey(
                        name: "fk_team_memberships_teams_team_id",
                        column: x => x.team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_team_memberships_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ai_structuring_drafts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    raw_input_text = table.Column<string>(type: "text", nullable: false),
                    structured_output = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    accepted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ai_structuring_drafts", x => x.id);
                    table.ForeignKey(
                        name: "fk_ai_structuring_drafts_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ai_structuring_drafts_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "budget_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    allocated_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_budget_categories", x => x.id);
                    table.ForeignKey(
                        name: "fk_budget_categories_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "event_teams",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    split_method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    split_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    is_owning_team = table.Column<bool>(type: "boolean", nullable: false),
                    approver_user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_teams", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_teams_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_teams_teams_team_id",
                        column: x => x.team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_teams_users_approver_user_id",
                        column: x => x.approver_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "guests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    rsvp_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    dietary_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    access_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_guests", x => x.id);
                    table.ForeignKey(
                        name: "fk_guests_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "run_sheet_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    title = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_run_sheet_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_run_sheet_items_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_run_sheet_items_users_owner_user_id",
                        column: x => x.owner_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vendor_event_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vendor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vendor_event_links", x => x.id);
                    table.ForeignKey(
                        name: "fk_vendor_event_links_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_vendor_event_links_vendors_vendor_id",
                        column: x => x.vendor_id,
                        principalTable: "vendors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "expenses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    budget_category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    vendor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_expenses", x => x.id);
                    table.ForeignKey(
                        name: "fk_expenses_budget_categories_budget_category_id",
                        column: x => x.budget_category_id,
                        principalTable: "budget_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_expenses_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_expenses_vendors_vendor_id",
                        column: x => x.vendor_id,
                        principalTable: "vendors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "quote_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    vendor_event_link_id = table.Column<Guid>(type: "uuid", nullable: false),
                    brief_snapshot = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    sent_to_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quote_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_quote_requests_vendor_event_links_vendor_event_link_id",
                        column: x => x.vendor_event_link_id,
                        principalTable: "vendor_event_links",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "approvals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    expense_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_by = table.Column<Guid>(type: "uuid", nullable: false),
                    approver_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    decided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_approvals", x => x.id);
                    table.ForeignKey(
                        name: "fk_approvals_expenses_expense_id",
                        column: x => x.expense_id,
                        principalTable: "expenses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_approvals_teams_team_id",
                        column: x => x.team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_approvals_users_approver_id",
                        column: x => x.approver_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_approvals_users_requested_by",
                        column: x => x.requested_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "expense_team_splits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    expense_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    portion_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_expense_team_splits", x => x.id);
                    table.ForeignKey(
                        name: "fk_expense_team_splits_expenses_expense_id",
                        column: x => x.expense_id,
                        principalTable: "expenses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_expense_team_splits_teams_team_id",
                        column: x => x.team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "approval_audit_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    approval_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_approval_audit_entries", x => x.id);
                    table.ForeignKey(
                        name: "fk_approval_audit_entries_approvals_approval_id",
                        column: x => x.approval_id,
                        principalTable: "approvals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_approval_audit_entries_users_actor_id",
                        column: x => x.actor_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ai_structuring_drafts_created_by",
                table: "ai_structuring_drafts",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_ai_structuring_drafts_event_id",
                table: "ai_structuring_drafts",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_approval_audit_entries_actor_id",
                table: "approval_audit_entries",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_approval_audit_entries_approval_id",
                table: "approval_audit_entries",
                column: "approval_id");

            migrationBuilder.CreateIndex(
                name: "ix_approvals_approver_id",
                table: "approvals",
                column: "approver_id");

            migrationBuilder.CreateIndex(
                name: "ix_approvals_expense_id",
                table: "approvals",
                column: "expense_id");

            migrationBuilder.CreateIndex(
                name: "ix_approvals_requested_by",
                table: "approvals",
                column: "requested_by");

            migrationBuilder.CreateIndex(
                name: "ix_approvals_team_id",
                table: "approvals",
                column: "team_id");

            migrationBuilder.CreateIndex(
                name: "ix_budget_categories_event_id",
                table: "budget_categories",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_teams_approver_user_id",
                table: "event_teams",
                column: "approver_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_teams_event_id_team_id",
                table: "event_teams",
                columns: new[] { "event_id", "team_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_teams_team_id",
                table: "event_teams",
                column: "team_id");

            migrationBuilder.CreateIndex(
                name: "ix_events_created_by",
                table: "events",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_events_org_id",
                table: "events",
                column: "org_id");

            migrationBuilder.CreateIndex(
                name: "ix_expense_team_splits_expense_id",
                table: "expense_team_splits",
                column: "expense_id");

            migrationBuilder.CreateIndex(
                name: "ix_expense_team_splits_team_id",
                table: "expense_team_splits",
                column: "team_id");

            migrationBuilder.CreateIndex(
                name: "ix_expenses_budget_category_id",
                table: "expenses",
                column: "budget_category_id");

            migrationBuilder.CreateIndex(
                name: "ix_expenses_created_by",
                table: "expenses",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_expenses_vendor_id",
                table: "expenses",
                column: "vendor_id");

            migrationBuilder.CreateIndex(
                name: "ix_guests_event_id",
                table: "guests",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_quote_requests_vendor_event_link_id",
                table: "quote_requests",
                column: "vendor_event_link_id");

            migrationBuilder.CreateIndex(
                name: "ix_run_sheet_items_event_id",
                table: "run_sheet_items",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_run_sheet_items_owner_user_id",
                table: "run_sheet_items",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_team_memberships_team_id",
                table: "team_memberships",
                column: "team_id");

            migrationBuilder.CreateIndex(
                name: "ix_team_memberships_user_id_team_id",
                table: "team_memberships",
                columns: new[] { "user_id", "team_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_teams_org_id",
                table: "teams",
                column: "org_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_org_id_email",
                table: "users",
                columns: new[] { "org_id", "email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vendor_event_links_event_id",
                table: "vendor_event_links",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_vendor_event_links_vendor_id",
                table: "vendor_event_links",
                column: "vendor_id");

            migrationBuilder.CreateIndex(
                name: "ix_vendors_org_id",
                table: "vendors",
                column: "org_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_structuring_drafts");

            migrationBuilder.DropTable(
                name: "approval_audit_entries");

            migrationBuilder.DropTable(
                name: "event_teams");

            migrationBuilder.DropTable(
                name: "expense_team_splits");

            migrationBuilder.DropTable(
                name: "guests");

            migrationBuilder.DropTable(
                name: "quote_requests");

            migrationBuilder.DropTable(
                name: "run_sheet_items");

            migrationBuilder.DropTable(
                name: "team_memberships");

            migrationBuilder.DropTable(
                name: "approvals");

            migrationBuilder.DropTable(
                name: "vendor_event_links");

            migrationBuilder.DropTable(
                name: "expenses");

            migrationBuilder.DropTable(
                name: "teams");

            migrationBuilder.DropTable(
                name: "budget_categories");

            migrationBuilder.DropTable(
                name: "vendors");

            migrationBuilder.DropTable(
                name: "events");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "organizations");
        }
    }
}
