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
                    table.UniqueConstraint("ak_teams_org_id_id", x => new { x.org_id, x.id });
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
                    auth0_subject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    org_role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.UniqueConstraint("ak_users_org_id_id", x => new { x.org_id, x.id });
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
                    table.UniqueConstraint("ak_vendors_org_id_id", x => new { x.org_id, x.id });
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
                    table.UniqueConstraint("ak_events_org_id_id", x => new { x.org_id, x.id });
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
                    org_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_team_memberships", x => x.id);
                    table.ForeignKey(
                        name: "fk_team_memberships_organizations_org_id",
                        column: x => x.org_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_team_memberships_teams_org_id_team_id",
                        columns: x => new { x.org_id, x.team_id },
                        principalTable: "teams",
                        principalColumns: new[] { "org_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_team_memberships_users_org_id_user_id",
                        columns: x => new { x.org_id, x.user_id },
                        principalTable: "users",
                        principalColumns: new[] { "org_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ai_structuring_drafts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    org_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                        name: "fk_ai_structuring_drafts_events_org_id_event_id",
                        columns: x => new { x.org_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "org_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ai_structuring_drafts_organizations_org_id",
                        column: x => x.org_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ai_structuring_drafts_users_org_id_created_by",
                        columns: x => new { x.org_id, x.created_by },
                        principalTable: "users",
                        principalColumns: new[] { "org_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "budget_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    org_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    allocated_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_budget_categories", x => x.id);
                    table.UniqueConstraint("ak_budget_categories_org_id_id", x => new { x.org_id, x.id });
                    table.ForeignKey(
                        name: "fk_budget_categories_events_org_id_event_id",
                        columns: x => new { x.org_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "org_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_budget_categories_organizations_org_id",
                        column: x => x.org_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_teams",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    org_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                        name: "fk_event_teams_events_org_id_event_id",
                        columns: x => new { x.org_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "org_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_teams_organizations_org_id",
                        column: x => x.org_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_teams_teams_org_id_team_id",
                        columns: x => new { x.org_id, x.team_id },
                        principalTable: "teams",
                        principalColumns: new[] { "org_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_teams_users_org_id_approver_user_id",
                        columns: x => new { x.org_id, x.approver_user_id },
                        principalTable: "users",
                        principalColumns: new[] { "org_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "guests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    org_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                        name: "fk_guests_events_org_id_event_id",
                        columns: x => new { x.org_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "org_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_guests_organizations_org_id",
                        column: x => x.org_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "run_sheet_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    org_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                        name: "fk_run_sheet_items_events_org_id_event_id",
                        columns: x => new { x.org_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "org_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_run_sheet_items_organizations_org_id",
                        column: x => x.org_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_run_sheet_items_users_org_id_owner_user_id",
                        columns: x => new { x.org_id, x.owner_user_id },
                        principalTable: "users",
                        principalColumns: new[] { "org_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vendor_event_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    org_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vendor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vendor_event_links", x => x.id);
                    table.UniqueConstraint("ak_vendor_event_links_org_id_id", x => new { x.org_id, x.id });
                    table.ForeignKey(
                        name: "fk_vendor_event_links_events_org_id_event_id",
                        columns: x => new { x.org_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "org_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_vendor_event_links_organizations_org_id",
                        column: x => x.org_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_vendor_event_links_vendors_org_id_vendor_id",
                        columns: x => new { x.org_id, x.vendor_id },
                        principalTable: "vendors",
                        principalColumns: new[] { "org_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "expenses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    org_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.UniqueConstraint("ak_expenses_org_id_id", x => new { x.org_id, x.id });
                    table.ForeignKey(
                        name: "fk_expenses_budget_categories_org_id_budget_category_id",
                        columns: x => new { x.org_id, x.budget_category_id },
                        principalTable: "budget_categories",
                        principalColumns: new[] { "org_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_expenses_organizations_org_id",
                        column: x => x.org_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_expenses_users_org_id_created_by",
                        columns: x => new { x.org_id, x.created_by },
                        principalTable: "users",
                        principalColumns: new[] { "org_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_expenses_vendors_org_id_vendor_id",
                        columns: x => new { x.org_id, x.vendor_id },
                        principalTable: "vendors",
                        principalColumns: new[] { "org_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "quote_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    org_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                        name: "fk_quote_requests_organizations_org_id",
                        column: x => x.org_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_quote_requests_vendor_event_links_org_id_vendor_event_link_",
                        columns: x => new { x.org_id, x.vendor_event_link_id },
                        principalTable: "vendor_event_links",
                        principalColumns: new[] { "org_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "approvals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    org_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.UniqueConstraint("ak_approvals_org_id_id", x => new { x.org_id, x.id });
                    table.ForeignKey(
                        name: "fk_approvals_expenses_org_id_expense_id",
                        columns: x => new { x.org_id, x.expense_id },
                        principalTable: "expenses",
                        principalColumns: new[] { "org_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_approvals_organizations_org_id",
                        column: x => x.org_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_approvals_teams_org_id_team_id",
                        columns: x => new { x.org_id, x.team_id },
                        principalTable: "teams",
                        principalColumns: new[] { "org_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_approvals_users_org_id_approver_id",
                        columns: x => new { x.org_id, x.approver_id },
                        principalTable: "users",
                        principalColumns: new[] { "org_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_approvals_users_org_id_requested_by",
                        columns: x => new { x.org_id, x.requested_by },
                        principalTable: "users",
                        principalColumns: new[] { "org_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "expense_team_splits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    org_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expense_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    portion_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_expense_team_splits", x => x.id);
                    table.ForeignKey(
                        name: "fk_expense_team_splits_expenses_org_id_expense_id",
                        columns: x => new { x.org_id, x.expense_id },
                        principalTable: "expenses",
                        principalColumns: new[] { "org_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_expense_team_splits_organizations_org_id",
                        column: x => x.org_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_expense_team_splits_teams_org_id_team_id",
                        columns: x => new { x.org_id, x.team_id },
                        principalTable: "teams",
                        principalColumns: new[] { "org_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "approval_audit_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    org_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                        name: "fk_approval_audit_entries_approvals_org_id_approval_id",
                        columns: x => new { x.org_id, x.approval_id },
                        principalTable: "approvals",
                        principalColumns: new[] { "org_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_approval_audit_entries_organizations_org_id",
                        column: x => x.org_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_approval_audit_entries_users_org_id_actor_id",
                        columns: x => new { x.org_id, x.actor_id },
                        principalTable: "users",
                        principalColumns: new[] { "org_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ai_structuring_drafts_org_id_created_by",
                table: "ai_structuring_drafts",
                columns: new[] { "org_id", "created_by" });

            migrationBuilder.CreateIndex(
                name: "ix_ai_structuring_drafts_org_id_event_id",
                table: "ai_structuring_drafts",
                columns: new[] { "org_id", "event_id" });

            migrationBuilder.CreateIndex(
                name: "ix_approval_audit_entries_org_id_actor_id",
                table: "approval_audit_entries",
                columns: new[] { "org_id", "actor_id" });

            migrationBuilder.CreateIndex(
                name: "ix_approval_audit_entries_org_id_approval_id",
                table: "approval_audit_entries",
                columns: new[] { "org_id", "approval_id" });

            migrationBuilder.CreateIndex(
                name: "ix_approvals_org_id_approver_id",
                table: "approvals",
                columns: new[] { "org_id", "approver_id" });

            migrationBuilder.CreateIndex(
                name: "ix_approvals_org_id_expense_id",
                table: "approvals",
                columns: new[] { "org_id", "expense_id" });

            migrationBuilder.CreateIndex(
                name: "ix_approvals_org_id_requested_by",
                table: "approvals",
                columns: new[] { "org_id", "requested_by" });

            migrationBuilder.CreateIndex(
                name: "ix_approvals_org_id_team_id",
                table: "approvals",
                columns: new[] { "org_id", "team_id" });

            migrationBuilder.CreateIndex(
                name: "ix_budget_categories_org_id_event_id",
                table: "budget_categories",
                columns: new[] { "org_id", "event_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_teams_event_id_team_id",
                table: "event_teams",
                columns: new[] { "event_id", "team_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_teams_org_id_approver_user_id",
                table: "event_teams",
                columns: new[] { "org_id", "approver_user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_teams_org_id_event_id",
                table: "event_teams",
                columns: new[] { "org_id", "event_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_teams_org_id_team_id",
                table: "event_teams",
                columns: new[] { "org_id", "team_id" });

            migrationBuilder.CreateIndex(
                name: "ix_events_created_by",
                table: "events",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_expense_team_splits_org_id_expense_id",
                table: "expense_team_splits",
                columns: new[] { "org_id", "expense_id" });

            migrationBuilder.CreateIndex(
                name: "ix_expense_team_splits_org_id_team_id",
                table: "expense_team_splits",
                columns: new[] { "org_id", "team_id" });

            migrationBuilder.CreateIndex(
                name: "ix_expenses_org_id_budget_category_id",
                table: "expenses",
                columns: new[] { "org_id", "budget_category_id" });

            migrationBuilder.CreateIndex(
                name: "ix_expenses_org_id_created_by",
                table: "expenses",
                columns: new[] { "org_id", "created_by" });

            migrationBuilder.CreateIndex(
                name: "ix_expenses_org_id_vendor_id",
                table: "expenses",
                columns: new[] { "org_id", "vendor_id" });

            migrationBuilder.CreateIndex(
                name: "ix_guests_org_id_event_id",
                table: "guests",
                columns: new[] { "org_id", "event_id" });

            migrationBuilder.CreateIndex(
                name: "ix_quote_requests_org_id_vendor_event_link_id",
                table: "quote_requests",
                columns: new[] { "org_id", "vendor_event_link_id" });

            migrationBuilder.CreateIndex(
                name: "ix_run_sheet_items_org_id_event_id",
                table: "run_sheet_items",
                columns: new[] { "org_id", "event_id" });

            migrationBuilder.CreateIndex(
                name: "ix_run_sheet_items_org_id_owner_user_id",
                table: "run_sheet_items",
                columns: new[] { "org_id", "owner_user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_team_memberships_org_id_team_id",
                table: "team_memberships",
                columns: new[] { "org_id", "team_id" });

            migrationBuilder.CreateIndex(
                name: "ix_team_memberships_org_id_user_id",
                table: "team_memberships",
                columns: new[] { "org_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_team_memberships_user_id_team_id",
                table: "team_memberships",
                columns: new[] { "user_id", "team_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_org_id_auth0_subject",
                table: "users",
                columns: new[] { "org_id", "auth0_subject" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_org_id_email",
                table: "users",
                columns: new[] { "org_id", "email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vendor_event_links_org_id_event_id",
                table: "vendor_event_links",
                columns: new[] { "org_id", "event_id" });

            migrationBuilder.CreateIndex(
                name: "ix_vendor_event_links_org_id_vendor_id",
                table: "vendor_event_links",
                columns: new[] { "org_id", "vendor_id" });
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
